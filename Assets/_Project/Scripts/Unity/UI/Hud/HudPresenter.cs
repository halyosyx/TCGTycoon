using Game.Core.Session;
using Game.Unity.Interaction;
using Game.Unity.Player;
using Game.Unity.UI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Hud
{
    /// <summary>
    /// Fills the HUD (Hud.uxml) and keeps it current by listening to events, never by polling.
    /// Today the only live sources are the player (gameplay on or off) and the interactor (what is
    /// aimed at); day, clock and cash have no Core source yet and show placeholders until their
    /// milestones (see the TODOs). Holds no game rules: values are pushed in and shown.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudPresenter : MonoBehaviour, IHudSink
    {
        [SerializeField, Tooltip("Key shown in the interaction prompt. Interacting is a left click (GDD: click to interact).")]
        private string _interactKey = "LMB";

        [SerializeField, Tooltip("Bottom-right key hints as Key:Label pairs separated by semicolons.")]
        private string _keyHints = "I:Inventory";

        // TODO(F2 Booth setup + selling): the day cycle (Prep Night -> Show Day) and the show clock
        // come from the GameStateMachine; until then the HUD shows the only day that exists, the
        // first Prep Night at home.
        [SerializeField, Min(1), Tooltip("Placeholder day number until the day cycle exists (F2).")]
        private int _placeholderDay = 1;

        // TODO(F2 Booth setup + selling): EconomyService raises cash changes (sales, purchases). Until
        // then the readout shows the GDD's starting cash ($500) and only debug commands change it.
        [SerializeField, Tooltip("Placeholder cash in cents until EconomyService exists (F2). GDD starting cash: 50000.")]
        private long _placeholderCashCents = 50_000;

        private UIDocument _document;
        private DayClock _dayClock;
        private CashReadout _cash;
        private ToastStack _toasts;
        private Crosshair _crosshair;
        private InteractionPrompt _prompt;
        private KeyHints _hints;
        private PlayerController _player;
        private PlayerInteractor _interactor;
        private bool _isInitialized;

        public int Day => _isInitialized ? _dayClock.Day : _placeholderDay;

        public long CashCents => _isInitialized ? _cash.AmountCents : _placeholderCashCents;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();

            // Private fields survive between Play sessions when scene reload is disabled.
            _isInitialized = false;
            _player = null;
            _interactor = null;
        }

        /// <summary>Binds the HUD to the player. Called once by <c>GameBootstrap</c>.</summary>
        public void Initialize(PlayerController player, PlayerInteractor interactor)
        {
            if (player == null || !BindElements())
            {
                Debug.LogError($"{name}: {nameof(HudPresenter)} needs a player and a HUD document with every HUD control.", this);
                return;
            }

            _player = player;
            _interactor = interactor;
            _dayClock.SetDay(_placeholderDay, DayKind.PrepNight);
            _cash.SetAmount(_placeholderCashCents);
            _hints.Hints = _keyHints;

            _player.GameplayInputChanged += OnGameplayInputChanged;
            if (_interactor != null)
            {
                _interactor.HoveredChanged += OnHoveredChanged;
                OnHoveredChanged(_interactor.Hovered);
            }
            else
            {
                Debug.LogWarning($"{name}: the player has no {nameof(PlayerInteractor)}, so the HUD shows no interaction prompt.", this);
            }

            OnGameplayInputChanged(_player.IsInGameplay);
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.GameplayInputChanged -= OnGameplayInputChanged;
            }

            if (_interactor != null)
            {
                _interactor.HoveredChanged -= OnHoveredChanged;
            }
        }

        public void ShowDay(int day, DayKind kind)
        {
            if (_isInitialized) _dayClock.SetDay(day, kind);
        }

        public void SetTime(int hour, int minute)
        {
            if (_isInitialized) _dayClock.SetTime(hour, minute);
        }

        public void SetClosingSoon(bool isClosingSoon)
        {
            if (_isInitialized) _dayClock.SetClosingSoon(isClosingSoon);
        }

        public void SetCash(long cents)
        {
            if (_isInitialized) _cash.SetAmount(cents);
        }

        public void ShowCashEvent(string label, long deltaCents)
        {
            if (_isInitialized) _cash.ShowEvent(label, deltaCents);
        }

        public void ShowToast(ToastKind kind, string title, string detail, long? amountCents)
        {
            if (_isInitialized) _toasts.Show(kind, title, detail, amountCents);
        }

        private void OnHoveredChanged(IInteractable target)
        {
            bool isUsable = target != null;
            _crosshair.Usable = isUsable;
            if (isUsable)
            {
                _prompt.Show(_interactKey, target.PromptVerb, target.PromptObject);
            }
            else
            {
                _prompt.Hide();
            }
        }

        // The crosshair and prompt belong to walking around; a screen that frees the cursor hides them.
        // The rest of the HUD stays visible underneath full-screen UI.
        private void OnGameplayInputChanged(bool isInGameplay)
        {
            _crosshair.style.display = isInGameplay ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isInGameplay)
            {
                _prompt.Hide();
            }
        }

        private bool BindElements()
        {
            VisualElement root = _document.rootVisualElement;
            if (root == null)
            {
                return false;
            }

            _dayClock = root.Q<DayClock>();
            _cash = root.Q<CashReadout>();
            _toasts = root.Q<ToastStack>();
            _crosshair = root.Q<Crosshair>();
            _prompt = root.Q<InteractionPrompt>();
            _hints = root.Q<KeyHints>();

            // The HUD never takes the pointer: clicks must reach screens and the world behind it.
            root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);

            return _dayClock != null && _cash != null && _toasts != null && _crosshair != null && _prompt != null && _hints != null;
        }
    }
}
