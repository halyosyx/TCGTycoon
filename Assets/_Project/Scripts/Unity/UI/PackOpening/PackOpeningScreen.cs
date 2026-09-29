using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Core.Session;
using Game.Unity.Definitions;
using Game.Unity.Player;
using Game.Unity.Props;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Pack opening screen: asks Core to open a pack when the pack prop is picked up, then presents
    /// the result as a face-down stack revealed one card at a time and finally a row, where any card
    /// can be lifted into a large showcase for inspection. Owns input, animation and layout only. The
    /// cards are in the inventory before anything is shown, so closing the screen at any point loses
    /// nothing.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PackOpeningScreen : MonoBehaviour
    {
        private const string RootName = "pack-opening";
        private const string CardLayerName = "card-layer";
        private const string StoreButtonName = "store-button";
        private const string HintName = "hint";
        private const string ShowcaseBackdropName = "showcase-backdrop";
        private const string ShowcaseLayerName = "showcase-layer";
        private const int PrewarmedCardViews = 5;
        private const int MaxPooledCardViews = 40;
        private const int PrimaryPointerButton = 0;
        private const int SecondaryPointerButton = 1;

        // Used only if the panel hasn't been laid out yet; matches the panel settings' reference height.
        private const float FallbackPanelHeight = 1080f;

        [SerializeField, Tooltip("UI Toolkit card template (Data/Generated/UI/CardTemplate.uxml).")]
        private VisualTreeAsset _cardTemplate;

        [SerializeField]
        private RevealPacing _pacing = new RevealPacing();

        [SerializeField]
        private RevealLayout _layout = new RevealLayout();

        [SerializeField, Tooltip("Glow strength per rarity tier. The glow colour comes from the Rarity Palette.")]
        private TierTell[] _tierTells = TierTell.CreateDefaults();

        [SerializeField]
        private string _revealHint = "Click or drag to reveal   ·   Space to skip";

        [SerializeField]
        private string _rowHint = "Click a card to inspect it   ·   Click outside the cards or press Store";

        [SerializeField]
        private string _showcaseHint = "Right click, click outside the card or press Esc to put it back";

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _cardLayer;
        private Button _storeButton;
        private Label _hint;
        private VisualElement _showcaseBackdrop;
        private VisualElement _showcaseLayer;
        private PlayerControls _controls;
        private PackRevealStateMachine _reveal;
        private ObjectPool<CardView> _cardViewPool;
        private List<CardView> _cardViews;

        private GameSession _session;
        private RarityPaletteDefinition _palette;
        private PlayerController _player;
        private PackProp _packProp;
        private bool _isInitialized;

        private bool _isRowPending;
        private float _rowCountdown;
        private bool _isPointerDown;
        private int _pointerId;
        private float _pointerStartX;
        private float _pointerDeltaX;
        private CardView _hoveredView;
        private CardView _returningView;
        private float _backdropLevel;

        public PackRevealState State => _reveal == null ? PackRevealState.Idle : _reveal.State;

        /// <summary>Cards on screen in slot order, for tests and debugging.</summary>
        public IReadOnlyList<CardView> CardViews => _cardViews;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_cardTemplate == null)
            {
                Debug.LogError($"{name}: Card Template is not assigned on {nameof(PackOpeningScreen)}.", this);
            }

            // Private fields survive between Play sessions when scene reload is disabled, so every
            // runtime object is created here rather than in field initialisers.
            _controls = new PlayerControls();
            _reveal = new PackRevealStateMachine();
            _cardViews = new List<CardView>(PrewarmedCardViews * 2);
            _cardViewPool = new ObjectPool<CardView>(
                CreateCardView,
                actionOnRelease: ReleaseCardView,
                defaultCapacity: PrewarmedCardViews,
                maxSize: MaxPooledCardViews);
            _isInitialized = false;
            _isRowPending = false;
            _isPointerDown = false;
            _hoveredView = null;
            _returningView = null;
            _backdropLevel = 0f;
        }

        /// <summary>Wires the screen to the session and the scene. Called once by <c>GameBootstrap</c>.</summary>
        public void Initialize(GameSession session, RarityPaletteDefinition palette, PlayerController player, PackProp packProp)
        {
            _session = session;
            _palette = palette;
            _player = player;
            _packProp = packProp;
            if (_session == null || _palette == null || _player == null || _packProp == null || _cardTemplate == null)
            {
                Debug.LogError($"{name}: {nameof(PackOpeningScreen)} is missing its session, palette, player, pack prop or card template.", this);
                return;
            }

            if (!BindElements())
            {
                return;
            }

            Prewarm();
            _packProp.PickedUp += OnPackPickedUp;
            _controls.Screens.Enable();
            SetVisible(false);
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_packProp != null)
            {
                _packProp.PickedUp -= OnPackPickedUp;
            }

            if (_root != null)
            {
                _root.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                _root.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            }

            if (_storeButton != null)
            {
                _storeButton.clicked -= Store;
            }

            // Disable before disposing, or the generated wrapper warns about a leak when it is finalized.
            _controls.Screens.Disable();
            _controls.Dispose();
        }

        private void Update()
        {
            if (!_isInitialized || _reveal.State == PackRevealState.Idle)
            {
                return;
            }

            float deltaSeconds = Time.unscaledDeltaTime;
            float hoverFactor = _layout.HoverScale / _layout.RowScale;
            for (int i = 0; i < _cardViews.Count; i++)
            {
                CardView view = _cardViews[i];
                bool isHovered = _reveal.State == PackRevealState.Row && view == _hoveredView;
                view.SetHoverTarget(isHovered ? hoverFactor : 1f, _pacing.HoverSeconds);
                view.Tick(deltaSeconds);
            }

            UpdateShowcaseLayers(deltaSeconds);

            PlayerControls.ScreensActions actions = _controls.Screens;
            if (actions.Dismiss.WasPressedThisFrame())
            {
                // Escape backs out one level: from the showcase to the row, otherwise it stores.
                if (_reveal.State == PackRevealState.Showcase) ReturnShowcasedCard();
                else Store();
                return;
            }

            if (_reveal.State == PackRevealState.Revealing && actions.QuickOpen.WasPressedThisFrame())
            {
                QuickOpen();
                return;
            }

            if (_isRowPending && !IsAnyCardAnimating())
            {
                _rowCountdown -= deltaSeconds;
                if (_rowCountdown <= 0f)
                {
                    ShowRow(isAnimated: true);
                }
            }
        }

        /// <summary>
        /// Opens a pack: Core rolls it and adds the cards to the inventory first, then the screen shows
        /// them. Public so tests and debug tools can drive the same path as a click on the prop.
        /// </summary>
        public void OpenPack()
        {
            if (!_isInitialized || _reveal.State != PackRevealState.Idle || !_player.IsInGameplay)
            {
                return;
            }

            // From this line on the cards are owned; everything below is presentation.
            OpenedPack pack = _session.OpenPack();

            _packProp.Hide();
            _player.SetGameplayInput(false);
            _reveal.Begin(pack);
            SetVisible(true);
            BuildStack(pack);
            UpdateHint();
            if (_reveal.State == PackRevealState.Row)
            {
                ShowRow(isAnimated: false);
            }
        }

        /// <summary>Reveals the next card, as a click on the stack does.</summary>
        public void RevealNext() => Advance(1f);

        /// <summary>Skips straight to the row, as the quick-open key does.</summary>
        public void QuickOpen()
        {
            if (_reveal.State == PackRevealState.Revealing)
            {
                ShowRow(isAnimated: false);
            }
        }

        /// <summary>Lifts a row card into the showcase, as a click on it does.</summary>
        public void ShowcaseCard(int slotIndex)
        {
            if (_reveal.State != PackRevealState.Row || slotIndex < 0 || slotIndex >= _cardViews.Count)
            {
                return;
            }

            SettleReturningCard();
            _reveal.Showcase(slotIndex);

            CardView view = _cardViews[slotIndex];
            view.Finish();
            _showcaseLayer.Add(view.Root);

            // Both layers cover the panel from its origin, so the card's rest rectangle is in panel
            // coordinates in either one.
            Rect rest = view.LayoutRect;
            float panelWidth = PanelSize(_root.layout.width, rest.center.x * 2f);
            float panelHeight = PanelSize(_root.layout.height, FallbackPanelHeight);
            float restHeight = rest.height > 0f ? rest.height : CardView.Height;
            var target = new Vector2(panelWidth * 0.5f - rest.center.x, panelHeight * _layout.ShowcaseCenterShare - rest.center.y);
            float scale = panelHeight * _layout.ShowcaseHeightShare / restHeight;
            view.MoveTo(target, scale, 1f, _pacing.ShowcaseSeconds, isHiddenWhenMoved: false);
            SetBackdropInteractive(true);
            UpdateHint();
        }

        /// <summary>Puts the showcased card back in the row, as a right click, Escape or a click outside it does.</summary>
        public void ReturnShowcasedCard()
        {
            if (_reveal.State != PackRevealState.Showcase)
            {
                return;
            }

            int slotIndex = _reveal.ShowcasedSlot;
            _reveal.ReturnToRow();

            // It stays above the row until it lands, then rejoins the row layer (UpdateShowcaseLayers).
            CardView view = _cardViews[slotIndex];
            view.MoveTo(RowPosition(slotIndex, _cardViews.Count), _layout.RowScale, 1f, _pacing.ShowcaseSeconds, isHiddenWhenMoved: false);
            _returningView = view;
            SetBackdropInteractive(false);
            UpdateHint();
        }

        /// <summary>Closes the screen, as the Store button, Escape or a click outside the row does.</summary>
        public void Store()
        {
            if (_reveal.State == PackRevealState.Idle)
            {
                return;
            }

            _reveal.Store();
            _isRowPending = false;
            _hoveredView = null;
            _returningView = null;
            _backdropLevel = 0f;
            SetBackdropInteractive(false);
            _showcaseBackdrop.style.opacity = 0f;
            _showcaseBackdrop.style.display = DisplayStyle.None;
            CancelPointer();
            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViewPool.Release(_cardViews[i]);
            }

            _cardViews.Clear();
            SetVisible(false);
            _packProp.Show();
            _player.SetGameplayInput(true);
        }

        private void OnPackPickedUp(PackProp prop) => OpenPack();

        private void BuildStack(OpenedPack pack)
        {
            IReadOnlyList<Card> cards = pack.Cards;
            for (int slotIndex = 0; slotIndex < cards.Count; slotIndex++)
            {
                CardView view = _cardViewPool.Get();
                Card card = cards[slotIndex];
                view.Bind(card, _palette, TellFor(card.Tier), StackPosition(slotIndex));
                _cardViews.Add(view);
            }

            // Later children draw on top, so the first slot is added last.
            for (int slotIndex = _cardViews.Count - 1; slotIndex >= 0; slotIndex--)
            {
                _cardLayer.Add(_cardViews[slotIndex].Root);
            }
        }

        // One click or swipe: the face-up card leaves, the next one reveals.
        private void Advance(float direction)
        {
            if (_reveal.State != PackRevealState.Revealing)
            {
                return;
            }

            int topIndex = _reveal.RevealedCount - 1;
            if (topIndex >= 0 && _cardViews[topIndex].IsAnimating)
            {
                // A click during a reveal completes it instead of skipping the card.
                _cardViews[topIndex].Finish();
                return;
            }

            if (!_reveal.HasUnrevealedCards)
            {
                ShowRow(isAnimated: true);
                return;
            }

            if (topIndex >= 0)
            {
                CardView leaving = _cardViews[topIndex];
                var offscreen = new Vector2(Mathf.Sign(direction) * _layout.SlideDistance, leaving.Position.y);
                leaving.MoveTo(offscreen, 1f, 0f, _pacing.SlideOutSeconds, isHiddenWhenMoved: true);
            }

            int slotIndex = _reveal.RevealNext();
            CardView revealing = _cardViews[slotIndex];
            if (_pacing.IsSlowSlot(slotIndex, _reveal.CardCount))
            {
                revealing.RevealSlow(_pacing.SlowTellSeconds, _pacing.SlowFlipSeconds);
            }
            else
            {
                revealing.RevealFast(_pacing.FastFlipSeconds);
            }

            if (!_reveal.HasUnrevealedCards)
            {
                _isRowPending = true;
                _rowCountdown = _pacing.LastCardHoldSeconds;
            }
        }

        private void ShowRow(bool isAnimated)
        {
            if (_reveal.State == PackRevealState.Revealing)
            {
                _reveal.ShowRow();
            }

            _isRowPending = false;
            CancelPointer();
            for (int slotIndex = 0; slotIndex < _cardViews.Count; slotIndex++)
            {
                CardView view = _cardViews[slotIndex];
                view.Finish();
                view.ShowFaceUp();
                Vector2 target = RowPosition(slotIndex, _cardViews.Count);
                if (isAnimated)
                {
                    view.MoveTo(target, _layout.RowScale, 1f, _pacing.RowLayoutSeconds, isHiddenWhenMoved: false);
                }
                else
                {
                    view.PlaceAt(target, _layout.RowScale, 1f);
                }
            }

            UpdateHint();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (IsWithin(evt.target as VisualElement, _storeButton))
            {
                return;
            }

            if (_reveal.State == PackRevealState.Showcase)
            {
                // Right click anywhere, or a left click anywhere but the card, puts it back.
                CardView showcased = _cardViews[_reveal.ShowcasedSlot];
                if (evt.button == SecondaryPointerButton || !IsWithin(evt.target as VisualElement, showcased.Root))
                {
                    ReturnShowcasedCard();
                }

                return;
            }

            if (evt.button != PrimaryPointerButton)
            {
                return;
            }

            if (_reveal.State == PackRevealState.Row)
            {
                // A click on a card showcases it. Only the empty screen counts as "outside": the
                // layers the cards sit on ignore picking, so the root is the target there.
                int slotIndex = SlotOf(evt.target as VisualElement);
                if (slotIndex >= 0)
                {
                    ShowcaseCard(slotIndex);
                }
                else if (evt.target == _root)
                {
                    Store();
                }

                return;
            }

            if (_reveal.State != PackRevealState.Revealing)
            {
                return;
            }

            _isPointerDown = true;
            _pointerId = evt.pointerId;
            _pointerStartX = evt.position.x;
            _pointerDeltaX = 0f;
            _root.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isPointerDown || evt.pointerId != _pointerId)
            {
                return;
            }

            _pointerDeltaX = evt.position.x - _pointerStartX;
            CardView dragged = DragTarget();
            if (dragged != null)
            {
                dragged.SetDragOffset(_pointerDeltaX);
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isPointerDown || evt.pointerId != _pointerId)
            {
                return;
            }

            float distance = Mathf.Abs(_pointerDeltaX);
            float direction = _pointerDeltaX < 0f ? -1f : 1f;
            CardView dragged = DragTarget();
            CancelPointer();

            if (distance <= _layout.ClickSlop || distance >= _layout.DragThreshold)
            {
                Advance(direction);
            }
            else if (dragged != null)
            {
                // A short drag that isn't a swipe puts the card back.
                dragged.SetDragOffset(0f);
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_isPointerDown)
            {
                CardView dragged = DragTarget();
                CancelPointer();
                if (dragged != null)
                {
                    dragged.SetDragOffset(0f);
                }
            }
        }

        private void CancelPointer()
        {
            if (_isPointerDown && _root != null && _root.HasPointerCapture(_pointerId))
            {
                _root.ReleasePointer(_pointerId);
            }

            _isPointerDown = false;
            _pointerDeltaX = 0f;
        }

        // The face-up card is swiped away; before the first reveal, the face-down top card moves.
        private CardView DragTarget()
        {
            if (_reveal.State != PackRevealState.Revealing)
            {
                return null;
            }

            int topIndex = _reveal.RevealedCount - 1;
            if (topIndex >= 0)
            {
                return _cardViews[topIndex];
            }

            return _reveal.HasUnrevealedCards ? _cardViews[_reveal.RevealedCount] : null;
        }

        private bool IsAnyCardAnimating()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i].IsAnimating)
                {
                    return true;
                }
            }

            return false;
        }

        private TierTell TellFor(RarityTier tier)
        {
            if (_tierTells != null)
            {
                foreach (TierTell tell in _tierTells)
                {
                    if (tell != null && tell.Tier == tier)
                    {
                        return tell;
                    }
                }
            }

            return null;
        }

        private Vector2 StackPosition(int slotIndex) => _layout.StackStep * slotIndex;

        private Vector2 RowPosition(int slotIndex, int cardCount)
        {
            float step = CardView.Width * _layout.RowScale + _layout.RowGap;
            return new Vector2((slotIndex - (cardCount - 1) * 0.5f) * step, 0f);
        }

        private void UpdateHint()
        {
            switch (_reveal.State)
            {
                case PackRevealState.Row:
                    _hint.text = _rowHint;
                    break;
                case PackRevealState.Showcase:
                    _hint.text = _showcaseHint;
                    break;
                default:
                    _hint.text = _revealHint;
                    break;
            }
        }

        // Fades the dimming backdrop, and moves a card that has landed back in the row to the row layer.
        private void UpdateShowcaseLayers(float deltaSeconds)
        {
            float target = _reveal.State == PackRevealState.Showcase ? 1f : 0f;
            if (!Mathf.Approximately(_backdropLevel, target))
            {
                _backdropLevel = Mathf.MoveTowards(_backdropLevel, target, deltaSeconds / _pacing.ShowcaseSeconds);
                _showcaseBackdrop.style.opacity = _backdropLevel;
                _showcaseBackdrop.style.display = _backdropLevel > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_returningView != null && !_returningView.IsAnimating)
            {
                SettleReturningCard();
            }
        }

        private void SettleReturningCard()
        {
            if (_returningView == null)
            {
                return;
            }

            _returningView.Finish();
            _cardLayer.Add(_returningView.Root);
            _returningView = null;
        }

        // Picking is on only while a card is showcased, so the fading backdrop never blocks the row.
        private void SetBackdropInteractive(bool isInteractive)
        {
            _showcaseBackdrop.pickingMode = isInteractive ? PickingMode.Position : PickingMode.Ignore;
        }

        private int SlotOf(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.userData is CardView view)
                {
                    return _cardViews.IndexOf(view);
                }
            }

            return -1;
        }

        private void OnCardPointerEnter(PointerEnterEvent evt)
        {
            var element = evt.currentTarget as VisualElement;
            _hoveredView = element == null ? null : element.userData as CardView;
        }

        private void OnCardPointerLeave(PointerLeaveEvent evt)
        {
            var element = evt.currentTarget as VisualElement;
            if (element != null && element.userData == _hoveredView)
            {
                _hoveredView = null;
            }
        }

        private static float PanelSize(float measured, float fallback)
        {
            return float.IsNaN(measured) || measured <= 0f ? fallback : measured;
        }

        private bool BindElements()
        {
            VisualElement documentRoot = _document.rootVisualElement;
            _root = documentRoot == null ? null : documentRoot.Q<VisualElement>(RootName);
            _cardLayer = _root == null ? null : _root.Q<VisualElement>(CardLayerName);
            _storeButton = _root == null ? null : _root.Q<Button>(StoreButtonName);
            _hint = _root == null ? null : _root.Q<Label>(HintName);
            _showcaseBackdrop = _root == null ? null : _root.Q<VisualElement>(ShowcaseBackdropName);
            _showcaseLayer = _root == null ? null : _root.Q<VisualElement>(ShowcaseLayerName);
            if (_root == null || _cardLayer == null || _storeButton == null || _hint == null || _showcaseBackdrop == null || _showcaseLayer == null)
            {
                Debug.LogError($"{name}: the UI document is missing '{RootName}', '{CardLayerName}', '{StoreButtonName}', '{HintName}', '{ShowcaseBackdropName}' or '{ShowcaseLayerName}'.", this);
                return false;
            }

            // Space quick-opens; a focused button would also treat Space as a press.
            _storeButton.focusable = false;
            _storeButton.clicked += Store;
            _root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _root.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            return true;
        }

        private void Prewarm()
        {
            var warm = new CardView[PrewarmedCardViews];
            for (int i = 0; i < warm.Length; i++)
            {
                warm[i] = _cardViewPool.Get();
            }

            for (int i = 0; i < warm.Length; i++)
            {
                _cardViewPool.Release(warm[i]);
            }
        }

        private void SetVisible(bool isVisible)
        {
            _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private CardView CreateCardView()
        {
            var view = new CardView(_cardTemplate);

            // The view rides on its element so pointer handlers can find it from any child.
            view.Root.userData = view;
            view.Root.RegisterCallback<PointerEnterEvent>(OnCardPointerEnter);
            view.Root.RegisterCallback<PointerLeaveEvent>(OnCardPointerLeave);
            return view;
        }

        private static void ReleaseCardView(CardView view) => view.Root.RemoveFromHierarchy();

        private static bool IsWithin(VisualElement element, VisualElement ancestor)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current == ancestor)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
