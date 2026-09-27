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
    /// the result as a face-down stack revealed one card at a time and finally a row. Owns input,
    /// animation and layout only. The cards are in the inventory before anything is shown, so closing
    /// the screen at any point loses nothing.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PackOpeningScreen : MonoBehaviour
    {
        private const string RootName = "pack-opening";
        private const string CardLayerName = "card-layer";
        private const string StoreButtonName = "store-button";
        private const string HintName = "hint";
        private const int PrewarmedCardViews = 5;
        private const int MaxPooledCardViews = 40;
        private const int PrimaryPointerButton = 0;

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
        private string _rowHint = "Click outside the cards or press Store";

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _cardLayer;
        private Button _storeButton;
        private Label _hint;
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

            _controls.Dispose();
        }

        private void Update()
        {
            if (!_isInitialized || _reveal.State == PackRevealState.Idle)
            {
                return;
            }

            float deltaSeconds = Time.unscaledDeltaTime;
            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViews[i].Tick(deltaSeconds);
            }

            PlayerControls.ScreensActions actions = _controls.Screens;
            if (actions.Dismiss.WasPressedThisFrame())
            {
                Store();
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

        /// <summary>Closes the screen, as the Store button, Escape or a click outside the row does.</summary>
        public void Store()
        {
            if (_reveal.State == PackRevealState.Idle)
            {
                return;
            }

            _reveal.Store();
            _isRowPending = false;
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
            if (evt.button != PrimaryPointerButton || IsWithin(evt.target as VisualElement, _storeButton))
            {
                return;
            }

            if (_reveal.State == PackRevealState.Row)
            {
                // Only the empty backdrop counts as "outside": the cards and the layer they sit on
                // pass the event up with themselves as the target.
                if (evt.target == _root)
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
            _hint.text = _reveal.State == PackRevealState.Row ? _rowHint : _revealHint;
        }

        private bool BindElements()
        {
            VisualElement documentRoot = _document.rootVisualElement;
            _root = documentRoot == null ? null : documentRoot.Q<VisualElement>(RootName);
            _cardLayer = _root == null ? null : _root.Q<VisualElement>(CardLayerName);
            _storeButton = _root == null ? null : _root.Q<Button>(StoreButtonName);
            _hint = _root == null ? null : _root.Q<Label>(HintName);
            if (_root == null || _cardLayer == null || _storeButton == null || _hint == null)
            {
                Debug.LogError($"{name}: the UI document is missing '{RootName}', '{CardLayerName}', '{StoreButtonName}' or '{HintName}'.", this);
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

        private CardView CreateCardView() => new CardView(_cardTemplate);

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
