using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Session;
using Game.Unity.Cards;
using Game.Unity.Definitions;
using Game.Unity.Player;
using Game.Unity.UI.Hud;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Pack opening, owned end to end by this screen; the pack in the hand only hands itself over
    /// (<see cref="BeginOpen"/>) and shows the poses it is given.
    /// <list type="number">
    /// <item>Zoom (uncommitted): the pack travels from the hand to a centre anchor, turns to show its
    /// back and scales up to nearly fill the screen; the world dims. Esc backs out with nothing committed.</item>
    /// <item>Rip: the click commits the cards once, before anything animates, then the back seam tears
    /// top to bottom and the flaps open like a book, showing the real first card face up inside.</item>
    /// <item>Lift: that card is handed to the UI at exactly its place on screen and zooms to the stack,
    /// the rest of the pile building under it, while the dimmed world darkens into the reveal backdrop.</item>
    /// <item>Reveal: the cards are a large face-up stack, swiped away along a downward curve; a rare card
    /// flashes once as it lands (Special Full Art Holo also sparkles), locally and without blocking input.</item>
    /// <item>Rows: 4 + 3, where any card can be lifted into a large showcase.</item>
    /// </list>
    /// Space skips to the rows (during the zoom it counts as the rip click). Storing at any point after
    /// the commit loses nothing. Owns input, animation and layout only.
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
        /// <summary>Serialized name of the card template field, for the card data generator's re-link step.</summary>
        public const string CardTemplateField = nameof(_cardTemplate);

        private const int PrewarmedCardViews = 7;
        private const int MaxPooledCardViews = 40;
        private const int PrimaryPointerButton = 0;
        private const int SecondaryPointerButton = 1;

        // Used only if the panel hasn't been laid out yet; matches the panel settings' reference height.
        private const float FallbackPanelHeight = 1080f;

        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("UI Toolkit card template (Data/Generated/UI/CardTemplate.uxml).")]
        private VisualTreeAsset _cardTemplate;

        [SerializeField]
        private RevealPacing _pacing = new RevealPacing();

        [SerializeField]
        private RevealLayout _layout = new RevealLayout();

        [SerializeField, Tooltip("Reaction per rarity tier when its card is revealed. Halo and sparkle colour come from the Rarity Palette.")]
        private TierTell[] _tierTells = TierTell.CreateDefaults();

        [SerializeField]
        private SparkleSettings _sparkles = new SparkleSettings();

        [Header("Opening the pack")]
        [SerializeField]
        private PackTearPacing _tearPacing = new PackTearPacing();

        [SerializeField]
        private PackTearSounds _tearSounds = new PackTearSounds();

        [SerializeField, Tooltip("Plays the rip's sounds. Optional: without it the rip is silent.")]
        private AudioSource _tearAudio;

        [SerializeField, Tooltip("Dark quad on the Held layer behind the zoom anchor (under HeldItemsCamera): dims the world, not the pack. Optional.")]
        private Renderer _worldDim;

        [SerializeField] private string _ripPromptKey = "LMB";
        [SerializeField] private string _ripPromptVerb = "Rip";
        [SerializeField] private string _ripPromptObject = "the back seam";

        [SerializeField, Tooltip("HUD key hints while the pack is zoomed, as Key:Label pairs separated by semicolons.")]
        private string _zoomHints = "LMB:Rip;Space:Skip to row;Esc:Put back";

        [SerializeField, Tooltip("HUD key hints while the pack rips open.")]
        private string _ripHints = "Space:Skip to row;Esc:Store";

        [SerializeField]
        private string _revealHint = "Click or swipe for the next card   ·   Space to skip";

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
        private ObjectPool<SparkleBurst> _sparklePool;
        private List<SparkleBurst> _activeBursts;
        private MaterialPropertyBlock _dimBlock;

        private GameSession _session;
        private RarityPaletteDefinition _palette;
        private PlayerController _player;
        private HudPresenter _hud;
        private PackTearProgress _tearProgress;
        private ITearablePack _openingPack;
        private bool _isInitialized;
        private bool _isPromptShown;
        private bool _isRowPending;
        private float _rowCountdown;
        private float _stackScale;
        private float _dimAlpha;
        private bool _isLifting;
        private bool _isCrossfading;
        private float _liftElapsed;
        private float _liftDimStart;
        private Color _backdropColour;
        private bool _isPointerDown;
        private int _pointerId;
        private float _pointerStartX;
        private float _pointerDeltaX;
        private CardView _hoveredView;
        private CardView _returningView;
        private float _backdropLevel;

        public PackRevealState State => _reveal == null ? PackRevealState.Idle : _reveal.State;

        /// <summary>The pack is at the anchor and waiting for the rip click.</summary>
        public bool IsSettled => _reveal != null && _reveal.State == PackRevealState.Zooming && _tearProgress.IsSettled;

        /// <summary>Cards on screen in slot order, for tests and debugging.</summary>
        public IReadOnlyList<CardView> CardViews => _cardViews;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_cardTemplate == null)
            {
                Debug.LogError($"{name}: Card Template is not assigned on {nameof(PackOpeningScreen)}.", this);
            }

            ReportStaleTierTells();

            // Private fields survive between Play sessions when scene reload is disabled, so every
            // runtime object is created here rather than in field initialisers.
            _controls = new PlayerControls();
            _reveal = new PackRevealStateMachine();
            _tearProgress = new PackTearProgress(_tearPacing);
            _tearProgress.CueReached += OnTearCue;
            _openingPack = null;
            _hud = null;
            _dimBlock = new MaterialPropertyBlock();
            _cardViews = new List<CardView>(PrewarmedCardViews * 2);
            _cardViewPool = new ObjectPool<CardView>(
                CreateCardView,
                actionOnRelease: ReleaseCardView,
                defaultCapacity: PrewarmedCardViews,
                maxSize: MaxPooledCardViews);
            _activeBursts = new List<SparkleBurst>(_sparkles.PooledBursts);
            _sparklePool = new ObjectPool<SparkleBurst>(
                () => new SparkleBurst(_sparkles.MaxSparklesPerBurst),
                actionOnRelease: burst => burst.Stop(),
                defaultCapacity: _sparkles.PooledBursts,
                maxSize: _sparkles.PooledBursts);
            _isInitialized = false;
            _isPromptShown = false;
            _isRowPending = false;
            _isLifting = false;
            _isCrossfading = false;
            _dimAlpha = 0f;
            _backdropColour = Color.clear;
            _isPointerDown = false;
            _hoveredView = null;
            _returningView = null;
            _backdropLevel = 0f;
            SetDim(0f);
        }

        /// <summary>Wires the screen to the session and the scene. Called once by <c>GameBootstrap</c>.</summary>
        /// <param name="hud">Shows the opening's prompt and key hints; may be null.</param>
        public void Initialize(GameSession session, RarityPaletteDefinition palette, PlayerController player, HudPresenter hud)
        {
            _session = session;
            _palette = palette;
            _player = player;
            _hud = hud;
            if (_session == null || _palette == null || _player == null || _player.Hands == null || _cardTemplate == null)
            {
                Debug.LogError($"{name}: {nameof(PackOpeningScreen)} is missing its session, palette, player (with hands) or card template.", this);
                return;
            }

            if (!BindElements())
            {
                return;
            }

            Prewarm();
            _controls.Screens.Enable();
            SetVisible(false);
            _isInitialized = true;
        }

        private void OnDestroy()
        {
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

            _tearProgress.CueReached -= OnTearCue;

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
            if (_reveal.State == PackRevealState.Zooming || _reveal.State == PackRevealState.Ripping)
            {
                UpdateOpening(deltaSeconds);
                return;
            }

            float hoverFactor = _layout.HoverScale / _layout.RowScale;
            for (int i = 0; i < _cardViews.Count; i++)
            {
                CardView view = _cardViews[i];
                bool isHovered = _reveal.State == PackRevealState.Row && view == _hoveredView;
                view.SetHoverTarget(isHovered ? hoverFactor : 1f, _pacing.HoverSeconds);
                view.Tick(deltaSeconds);
            }

            TickSparkles(deltaSeconds);
            UpdateShowcaseLayers(deltaSeconds);
            if (_isLifting)
            {
                UpdateLift(deltaSeconds);
            }

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

        // --- Opening: zoom (uncommitted), then the rip ---

        /// <summary>
        /// Starts opening the pack in the player's hand (Use, LMB): the zoom. Commits nothing; the pack
        /// stays in the hand and in Core until the rip click. Returns false (and does nothing) when no such
        /// pack is held, a pack is already open or another screen has the input. Public so tests and debug
        /// tools can drive the same path.
        /// </summary>
        public bool BeginOpen(ITearablePack pack)
        {
            if (pack == null || !CanOpen() || _session.Inventory.CountOfSealed(pack.ProductId, ItemLocation.Held) == 0)
            {
                return false;
            }

            _reveal.BeginZoom();
            _openingPack = pack;
            _tearProgress.Reset();
            _isPromptShown = false;

            // The cursor stays captured: nothing needs pointing until the reveal.
            _player.SetGameplayInput(false, keepsCursorLocked: true);
            float anchorScale = _tearPacing.AnchorScale(PackShape.Default.Height, _player.Hands.HeldFieldOfView);
            pack.BeginZoom(_player.Hands.View, new Vector3(0f, 0f, _tearPacing.ZoomDistance), _tearPacing.AnchorRotation, anchorScale);
            ApplyOpeningPose();
            if (_hud != null)
            {
                _hud.SetContextHints(_zoomHints);
            }

            return true;
        }

        /// <summary>
        /// The rip click: commits the cards once (Core takes the held pack and adds its cards to the binder
        /// at the price paid) before anything of the rip animates, then plays it. Only once the pack has
        /// settled at the anchor; any other time (and every click after the first) it does nothing.
        /// </summary>
        public bool RipOpen()
        {
            return IsSettled && TryRip();
        }

        /// <summary>Backs out of the zoom (Esc): the pack goes back to the hand; nothing was committed.</summary>
        public void BackOut()
        {
            if (_reveal.State != PackRevealState.Zooming || _tearProgress.IsZoomingOut)
            {
                return;
            }

            _tearProgress.ZoomOut();
            ClearPrompt();
            if (_hud != null)
            {
                _hud.ClearContextHints();
            }
        }

        private bool CanOpen() => _isInitialized && _reveal.State == PackRevealState.Idle && _player.IsInGameplay;

        private void UpdateOpening(float deltaSeconds)
        {
            PlayerControls.ScreensActions actions = _controls.Screens;
            if (_reveal.State == PackRevealState.Zooming && !_tearProgress.IsZoomingOut)
            {
                if (actions.Dismiss.WasPressedThisFrame())
                {
                    BackOut();
                }
                else if (actions.QuickOpen.WasPressedThisFrame())
                {
                    // Space during the zoom counts as the rip click, then skips to the rows.
                    if (TryRip())
                    {
                        SkipToRow();
                        return;
                    }
                }
                else if (_tearProgress.IsSettled && actions.RipPack.WasPressedThisFrame())
                {
                    TryRip();
                }
            }
            else if (_reveal.State == PackRevealState.Ripping)
            {
                if (actions.Dismiss.WasPressedThisFrame())
                {
                    Store();
                    return;
                }

                if (actions.QuickOpen.WasPressedThisFrame())
                {
                    SkipToRow();
                    return;
                }
            }

            _tearProgress.Tick(deltaSeconds);
            ApplyOpeningPose();

            if (_reveal.State == PackRevealState.Zooming)
            {
                if (_tearProgress.IsBackInHand)
                {
                    FinishBackOut();
                }
                else if (_tearProgress.IsSettled && !_isPromptShown)
                {
                    _isPromptShown = true;
                    if (_hud != null)
                    {
                        _hud.SetContextPrompt(_ripPromptKey, _ripPromptVerb, _ripPromptObject);
                    }
                }
            }
            else if (_tearProgress.IsComplete)
            {
                FinishRip();
            }
        }

        private bool TryRip()
        {
            string productId = _openingPack.ProductId;

            // From this line on the cards are owned; everything after it is presentation.
            if (!_reveal.TryBeginRip(() => _session.OpenSealedPack(productId, ItemLocation.Held)))
            {
                return false;
            }

            // The real first card sits face up inside, ready for the moment the back opens.
            IReadOnlyList<Card> cards = _reveal.Pack.Cards;
            if (cards.Count > 0)
            {
                _openingPack.ShowFirstCard(cards[0]);
            }

            // Core no longer holds the pack, so the hand lets go of it; the screen keeps the wrapper.
            _player.Hands.Clear();
            _tearProgress.StartRip();
            ClearPrompt();
            if (_hud != null)
            {
                _hud.SetContextHints(_ripHints);
            }

            return true;
        }

        private void FinishBackOut()
        {
            _openingPack.ReturnToHand();
            _openingPack = null;
            _reveal.CancelZoom();
            SetDim(0f);
            ClearPrompt();
            if (_hud != null)
            {
                _hud.ClearContextHints();
            }

            _player.SetGameplayInput(true);
        }

        // The back is open and the first card shows inside: hand the cards to the UI where they are.
        private void FinishRip()
        {
            _reveal.FinishRip();
            ReleaseOpeningControls();
            if (_reveal.State == PackRevealState.Revealing && TryGetFirstCardOnPanel(out Vector2 centre, out float height))
            {
                BeginLift(centre, height);
                return;
            }

            DiscardWrapper();
            ShowStack(_reveal.Pack);
        }

        private void SkipToRow()
        {
            _reveal.ShowRow();
            EndOpening();
            ShowStack(_reveal.Pack);
        }

        // The wrapper goes, the dimmer and hints go, and the cursor is freed for the reveal.
        private void EndOpening()
        {
            DiscardWrapper();
            ReleaseOpeningControls();
        }

        private void ReleaseOpeningControls()
        {
            ClearPrompt();
            if (_hud != null)
            {
                _hud.ClearContextHints();
            }

            _player.SetGameplayInput(false);
        }

        private void DiscardWrapper()
        {
            if (_openingPack != null)
            {
                _openingPack.Discard();
                _openingPack = null;
            }

            SetDim(0f);
        }

        // --- Lift: the cards leave the opened pack for the stack, and the screen darkens ---

        // Where the first card inside the pack is on the panel: its centre and height in panel pixels.
        private bool TryGetFirstCardOnPanel(out Vector2 centre, out float height)
        {
            centre = default;
            height = 0f;
            Camera camera = _player.Hands.HeldCamera;
            if (_openingPack == null || camera == null || _root.panel == null || !_openingPack.TryGetFirstCardEdges(out Vector3 top, out Vector3 bottom))
            {
                return false;
            }

            Vector3 topScreen = camera.WorldToScreenPoint(top);
            Vector3 bottomScreen = camera.WorldToScreenPoint(bottom);
            if (topScreen.z <= 0f || bottomScreen.z <= 0f)
            {
                return false;
            }

            // Screen space has its origin at the bottom left; panels at the top left.
            Vector2 topPanel = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(topScreen.x, Screen.height - topScreen.y));
            Vector2 bottomPanel = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(bottomScreen.x, Screen.height - bottomScreen.y));
            centre = (topPanel + bottomPanel) * 0.5f;
            height = Vector2.Distance(topPanel, bottomPanel);
            return height > 0f;
        }

        // The UI stack starts exactly over the first card in the pack and fades in there (the two card
        // faces cross-fade, so the hand-over can't be seen), then zooms to its place; the other cards start
        // under it and fan out into the pile on the way.
        private void BeginLift(Vector2 cardCentre, float cardHeight)
        {
            SetVisible(true);
            Vector2 panel = _root.panel.visualTree.layout.size;
            float panelWidth = PanelSize(panel.x, FallbackPanelHeight * 16f / 9f);
            float panelHeight = PanelSize(panel.y, FallbackPanelHeight);
            _stackScale = _layout.StackScale(panelHeight);
            var rest = new Vector2(panelWidth * 0.5f, panelHeight * RevealLayout.CardRestCentreShare);
            Vector2 start = cardCentre - rest;
            float startScale = cardHeight / CardView.Height;

            IReadOnlyList<Card> cards = _reveal.Pack.Cards;
            for (int slotIndex = 0; slotIndex < cards.Count; slotIndex++)
            {
                CardView view = _cardViewPool.Get();
                view.Bind(cards[slotIndex], _palette, start, startScale, isCovered: slotIndex > 0);
                view.PlaceAt(start, startScale, 0f);
                view.MoveTo(start, startScale, 1f, _pacing.LiftCrossfadeSeconds, isHiddenWhenMoved: false);
                _cardViews.Add(view);
            }

            // Later children draw on top, so the first slot is added last.
            for (int slotIndex = _cardViews.Count - 1; slotIndex >= 0; slotIndex--)
            {
                _cardLayer.Add(_cardViews[slotIndex].Root);
            }

            if (_backdropColour.a <= 0f)
            {
                _backdropColour = _root.resolvedStyle.backgroundColor;
            }

            _isLifting = true;
            _isCrossfading = true;
            _liftElapsed = 0f;
            _liftDimStart = _dimAlpha;
            ApplyLiftDarkness(0f);
            UpdateHint();
        }

        private void UpdateLift(float deltaSeconds)
        {
            _liftElapsed += deltaSeconds;
            if (_isCrossfading)
            {
                if (_liftElapsed >= _pacing.LiftCrossfadeSeconds)
                {
                    StartLiftZoom();
                }

                return;
            }

            float progress = _liftElapsed / _pacing.LiftSeconds;
            ApplyLiftDarkness(progress);
            if (progress >= 1f && (_cardViews.Count == 0 || !_cardViews[0].IsAnimating))
            {
                EndLift(isRevealingTopCard: true);
            }
        }

        // The reveal's cards now cover the ones in the pack completely: the pack lets them go, and the
        // cards zoom to the stack.
        private void StartLiftZoom()
        {
            _isCrossfading = false;
            _liftElapsed = 0f;
            if (_openingPack != null)
            {
                _openingPack.HideCards();
            }

            for (int slotIndex = 0; slotIndex < _cardViews.Count; slotIndex++)
            {
                _cardViews[slotIndex].MoveTo(StackPosition(slotIndex), _stackScale, 1f, _pacing.LiftSeconds, isHiddenWhenMoved: false);
            }
        }

        // The world dimmer fades out as the reveal backdrop fades in, so the screen darkens smoothly; the
        // hint and Store button fade in with it.
        private void ApplyLiftDarkness(float progress)
        {
            RevealBackdrop.Blend(_liftDimStart, _backdropColour.a, progress, out float dim, out float backdrop);
            SetDim(dim);
            Color colour = _backdropColour;
            colour.a = backdrop;
            _root.style.backgroundColor = colour;
            float footer = Mathf.Clamp01(progress);
            _hint.style.opacity = footer;
            _storeButton.style.opacity = footer;
        }

        // The lift is over (landed, skipped or stored): the wrapper goes and the backdrop is its own again.
        private void EndLift(bool isRevealingTopCard)
        {
            if (!_isLifting)
            {
                return;
            }

            _isLifting = false;
            _isCrossfading = false;
            DiscardWrapper();
            _root.style.backgroundColor = StyleKeyword.Null;
            _hint.style.opacity = StyleKeyword.Null;
            _storeButton.style.opacity = StyleKeyword.Null;
            if (isRevealingTopCard)
            {
                // The first card is revealed as it lands: its tier reaction plays now.
                RevealTopCard();
            }
        }

        // A click during the lift lands the cards at once rather than swiping the first one away.
        private void FinishLift()
        {
            for (int slotIndex = 0; slotIndex < _cardViews.Count; slotIndex++)
            {
                _cardViews[slotIndex].PlaceAt(StackPosition(slotIndex), _stackScale, 1f);
            }

            EndLift(isRevealingTopCard: true);
        }

        private void ApplyOpeningPose()
        {
            PackTearPose pose = PackTearMotion.Evaluate(_tearProgress.Zoom, _tearProgress.Seam, _tearProgress.Open, _tearProgress.Dim, _tearPacing);
            if (_openingPack != null)
            {
                _openingPack.ApplyPose(pose);
            }

            SetDim(pose.DimAlpha);
        }

        private void ClearPrompt()
        {
            if (_isPromptShown && _hud != null)
            {
                _hud.ClearContextPrompt();
            }

            _isPromptShown = false;
        }

        private void SetDim(float alpha)
        {
            _dimAlpha = alpha;
            if (_worldDim == null)
            {
                return;
            }

            bool isVisible = alpha > 0.001f;
            if (_worldDim.enabled != isVisible)
            {
                _worldDim.enabled = isVisible;
            }

            if (isVisible)
            {
                _dimBlock.SetColor(s_baseColorId, new Color(0f, 0f, 0f, alpha));
                _worldDim.SetPropertyBlock(_dimBlock);
            }
        }

        private void OnTearCue(PackTearCue cue)
        {
            AudioClip clip = _tearSounds.ClipFor(cue);
            if (clip != null && _tearAudio != null)
            {
                _tearAudio.PlayOneShot(clip, _tearSounds.Volume);
            }
        }

        // --- Reveal ---

        private void ShowStack(OpenedPack pack)
        {
            SetVisible(true);
            _stackScale = _layout.StackScale(PanelSize(_root.layout.height, FallbackPanelHeight));
            BuildStack(pack);
            UpdateHint();
            if (_reveal.State == PackRevealState.Row)
            {
                ShowRow(isAnimated: false);
                return;
            }

            // The first card arrives face up: it is revealed the moment the stack appears.
            RevealTopCard();
        }

        /// <summary>Swipes the top card away and reveals the next, as a click on the stack does.</summary>
        public void RevealNext() => Advance(1f);

        /// <summary>Skips straight to the rows, as Space does: mid-rip too, and during the zoom it rips first.</summary>
        public void QuickOpen()
        {
            switch (_reveal.State)
            {
                case PackRevealState.Zooming:
                    if (!_tearProgress.IsZoomingOut && TryRip())
                    {
                        SkipToRow();
                    }

                    break;
                case PackRevealState.Ripping:
                    SkipToRow();
                    break;
                case PackRevealState.Revealing:
                    EndLift(isRevealingTopCard: false);
                    ShowRow(isAnimated: false);
                    break;
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

        /// <summary>
        /// Closes the screen, as the Store button, Escape or a click outside the row does. Mid-rip it ends
        /// quietly: the cards are already in the binder. During the zoom (nothing committed) it puts the
        /// pack straight back in the hand.
        /// </summary>
        public void Store()
        {
            switch (_reveal.State)
            {
                case PackRevealState.Idle:
                    return;
                case PackRevealState.Zooming:
                    FinishBackOut();
                    return;
                case PackRevealState.Ripping:
                    EndOpening();
                    break;
            }

            EndLift(isRevealingTopCard: false);
            _reveal.Store();
            _isRowPending = false;
            _hoveredView = null;
            _returningView = null;
            _backdropLevel = 0f;
            SetBackdropInteractive(false);
            _showcaseBackdrop.style.opacity = 0f;
            _showcaseBackdrop.style.display = DisplayStyle.None;
            CancelPointer();
            StopSparkles();
            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViewPool.Release(_cardViews[i]);
            }

            _cardViews.Clear();
            SetVisible(false);
            _player.SetGameplayInput(true);
        }

        private void BuildStack(OpenedPack pack)
        {
            IReadOnlyList<Card> cards = pack.Cards;
            for (int slotIndex = 0; slotIndex < cards.Count; slotIndex++)
            {
                CardView view = _cardViewPool.Get();
                // Waiting cards are plain backs, so the pile's edges never give a tier away.
                view.Bind(cards[slotIndex], _palette, StackPosition(slotIndex), _stackScale, isCovered: true);
                _cardViews.Add(view);
            }

            // Later children draw on top, so the first slot is added last.
            for (int slotIndex = _cardViews.Count - 1; slotIndex >= 0; slotIndex--)
            {
                _cardLayer.Add(_cardViews[slotIndex].Root);
            }
        }

        // One click or swipe: the top card curves away and the next one, already face up, is revealed.
        private void Advance(float direction)
        {
            if (_reveal.State != PackRevealState.Revealing)
            {
                return;
            }

            if (_isLifting)
            {
                FinishLift();
                return;
            }

            if (!_reveal.HasUnrevealedCards)
            {
                ShowRow(isAnimated: true);
                return;
            }

            int topIndex = _reveal.RevealedCount - 1;
            if (topIndex >= 0)
            {
                _cardViews[topIndex].SwipeAway(direction, _layout.SwipeDistance, _layout.SwipeDrop, _layout.SwipeCurve, _layout.SwipeTiltDegrees, _pacing.SwipeSeconds);
            }

            RevealTopCard();
        }

        private void RevealTopCard()
        {
            int slotIndex = _reveal.RevealNext();
            Card card = _reveal.Pack.Cards[slotIndex];
            TierTell tell = TierTell.Find(_tierTells, card.Tier);
            CardView view = _cardViews[slotIndex];
            view.Uncover();
            view.PlayTell(tell);
            if (TierTell.KindOf(tell) == TierTellKind.FlashAndSparkle)
            {
                PlaySparkles(view, tell.SparkleCount, _palette.ColorOf(card.Tier));
            }

            if (!_reveal.HasUnrevealedCards)
            {
                _isRowPending = true;
                _rowCountdown = _pacing.LastCardHoldSeconds;
            }
        }

        // Pooled: at most the configured number of bursts exist; a new one beyond that reuses the oldest.
        private void PlaySparkles(CardView view, int count, Color colour)
        {
            if (_activeBursts.Count >= _sparkles.PooledBursts)
            {
                _sparklePool.Release(_activeBursts[0]);
                _activeBursts.RemoveAt(0);
            }

            SparkleBurst burst = _sparklePool.Get();
            burst.Play(view.Root, new Vector2(CardView.Width, CardView.Height), count, colour, _sparkles);
            _activeBursts.Add(burst);
        }

        private void TickSparkles(float deltaSeconds)
        {
            for (int i = _activeBursts.Count - 1; i >= 0; i--)
            {
                if (!_activeBursts[i].Tick(deltaSeconds))
                {
                    _sparklePool.Release(_activeBursts[i]);
                    _activeBursts.RemoveAt(i);
                }
            }
        }

        private void StopSparkles()
        {
            for (int i = 0; i < _activeBursts.Count; i++)
            {
                _sparklePool.Release(_activeBursts[i]);
            }

            _activeBursts.Clear();
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
                view.Uncover();
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

            // Taking hold of the top card shows the real card underneath, like lifting a real one.
            SetNextCardPeeked(true);
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
                // A short drag that isn't a swipe puts the card back, and the one underneath is covered again.
                dragged.SetDragOffset(0f);
                SetNextCardPeeked(false);
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
                    SetNextCardPeeked(false);
                }
            }
        }

        // The card directly under the top one: face up while the top card is held, a plain back otherwise.
        // Its rarity reaction still waits until it becomes the top card.
        private void SetNextCardPeeked(bool isPeeked)
        {
            if (_reveal.State != PackRevealState.Revealing || _isLifting || _reveal.RevealedCount < 1 || !_reveal.HasUnrevealedCards)
            {
                return;
            }

            CardView next = _cardViews[_reveal.RevealedCount];
            if (isPeeked)
            {
                next.Uncover();
            }
            else
            {
                next.Cover();
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

        // The face-up card on top of the stack is the one dragged and swiped.
        private CardView DragTarget()
        {
            if (_reveal.State != PackRevealState.Revealing)
            {
                return null;
            }

            int topIndex = _reveal.RevealedCount - 1;
            return topIndex >= 0 ? _cardViews[topIndex] : null;
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

        // A tell saved under the seven-tier ladder never matches a card, so that tier would silently lose its reaction.
        private void ReportStaleTierTells()
        {
            if (_tierTells == null)
            {
                return;
            }

            foreach (TierTell tell in _tierTells)
            {
                if (tell != null && !RarityTiers.IsDefined(tell.Tier))
                {
                    Debug.LogError($"{name}: a Tier Tell on {nameof(PackOpeningScreen)} uses {RarityTiers.Describe(tell.Tier)}; reset the Tier Tells to the four tiers.", this);
                }
            }
        }

        private Vector2 StackPosition(int slotIndex) => _layout.StackStep * (slotIndex * _stackScale);

        private Vector2 RowPosition(int slotIndex, int cardCount)
        {
            return _layout.RowPosition(slotIndex, cardCount, new Vector2(CardView.Width, CardView.Height));
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

            var bursts = new SparkleBurst[_sparkles.PooledBursts];
            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i] = _sparklePool.Get();
            }

            for (int i = 0; i < bursts.Length; i++)
            {
                _sparklePool.Release(bursts[i]);
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
