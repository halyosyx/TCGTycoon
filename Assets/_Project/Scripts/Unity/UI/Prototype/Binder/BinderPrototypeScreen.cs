// PROTOTYPE: temporary Binder UI. Replace, don't extend. See Docs/UI/UI_STYLE_GUIDE.md §8.

using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Unity.Flow;
using Game.Unity.Player;
using Game.Unity.UI.Controls;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Prototype
{
    /// <summary>
    /// The binder inventory (prototype). Reads only <see cref="IBinderReadModel"/>, found through
    /// <see cref="GameBootstrap"/>; nothing outside Prototype/ references this class, so deleting the
    /// folder removes the screen cleanly. I opens and closes (Esc also closes), A/D turn a spread,
    /// Q/E switch tabs, click selects a pocket and Enter asks for its details. The tabs are built from
    /// the read model (one per card set, then Sealed), so a new set needs no change here.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class BinderPrototypeScreen : MonoBehaviour
    {
        private const int RowsPerPage = 3;
        private const int PocketsPerRow = 3;
        private const int NoSelection = -1;
        private const string SpacedTabClassName = "binder__tab--spaced";

        [SerializeField, Tooltip("Supplies the binder read model and the session.")]
        private GameBootstrap _bootstrap;

        [SerializeField, Tooltip("Gameplay input is switched off while the binder is open.")]
        private PlayerController _player;

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _tabBar;
        private Label _leftPageNumber;
        private Label _rightPageNumber;
        private VisualElement[] _leftRows;
        private VisualElement[] _rightRows;
        private KitTab[] _tabs;
        private PlayerControls _controls;
        private ObjectPool<BinderPocket> _pocketPool;
        private List<BinderPocket> _visiblePockets;
        private IBinderReadModel _binder;

        private int _tabIndex;
        private int _spreadIndex;
        private int _selectedSlot;
        private bool _isOpen;
        private bool _isStale;
        private bool _isInitialized;

        /// <summary>Raised with the item id when the player asks for a pocket's details (Enter).</summary>
        public event Action<string> CardDetailsRequested;

        public bool IsOpen => _isOpen;

        /// <summary>Index of the open tab in the read model's <see cref="IBinderReadModel.Tabs"/>.</summary>
        public int ActiveTabIndex => _tabIndex;

        public int SpreadIndex => _spreadIndex;

        public BinderEntry SelectedEntry => _selectedSlot >= 0 ? _visiblePockets[_selectedSlot].Entry : null;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();

            // Private fields survive between Play sessions when scene reload is disabled.
            _controls = new PlayerControls();
            _visiblePockets = new List<BinderPocket>(BinderPaging.SlotsPerSpread);
            _pocketPool = new ObjectPool<BinderPocket>(CreatePocket, actionOnRelease: pocket => pocket.Root.RemoveFromHierarchy(), defaultCapacity: BinderPaging.SlotsPerSpread);
            _tabIndex = 0;
            _spreadIndex = 0;
            _selectedSlot = NoSelection;
            _isOpen = false;
            _isStale = true;
            _isInitialized = false;
        }

        // Start, so GameBootstrap.Awake has already built the read model.
        private void Start()
        {
            _binder = _bootstrap == null ? null : _bootstrap.BinderReadModel;
            if (_binder == null || _player == null || !BindElements())
            {
                Debug.LogError($"{name}: {nameof(BinderPrototypeScreen)} needs a GameBootstrap with a binder read model, a player and Binder.uxml.", this);
                return;
            }

            BuildTabs();
            _binder.Changed += OnBinderChanged;
            _controls.Screens.Enable();
            _root.style.display = DisplayStyle.None;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_binder != null)
            {
                _binder.Changed -= OnBinderChanged;
            }

            // Disable before disposing, or the generated wrapper warns about a leak when it is finalized.
            _controls.Screens.Disable();
            _controls.Dispose();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            PlayerControls.ScreensActions actions = _controls.Screens;
            if (!_isOpen)
            {
                if (actions.ToggleInventory.WasPressedThisFrame())
                {
                    Open();
                }

                return;
            }

            if (actions.ToggleInventory.WasPressedThisFrame() || actions.Dismiss.WasPressedThisFrame())
            {
                Close();
            }
            else if (actions.BinderPreviousTab.WasPressedThisFrame())
            {
                ChangeTab(-1);
            }
            else if (actions.BinderNextTab.WasPressedThisFrame())
            {
                ChangeTab(1);
            }
            else if (actions.BinderPreviousPage.WasPressedThisFrame())
            {
                TurnSpread(-1);
            }
            else if (actions.BinderNextPage.WasPressedThisFrame())
            {
                TurnSpread(1);
            }
            else if (actions.Submit.WasPressedThisFrame())
            {
                RequestDetails();
            }
        }

        /// <summary>Opens the binder. Ignored while another screen (e.g. a pack reveal) has the player's input.</summary>
        public bool Open()
        {
            if (!_isInitialized || _isOpen || !_player.IsInGameplay)
            {
                return false;
            }

            _isOpen = true;
            Rebuild();
            _root.style.display = DisplayStyle.Flex;
            _player.SetGameplayInput(false);
            return true;
        }

        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            _root.style.display = DisplayStyle.None;
            _player.SetGameplayInput(true);
        }

        public void ChangeTab(int step)
        {
            int tabCount = _tabs.Length;
            ShowTab(((_tabIndex + step) % tabCount + tabCount) % tabCount);
        }

        public void ShowTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Length)
            {
                return;
            }

            _tabIndex = tabIndex;
            _spreadIndex = 0;
            _selectedSlot = NoSelection;
            Rebuild();
        }

        public void TurnSpread(int step)
        {
            int spreadCount = BinderPaging.SpreadCount(_binder.GetEntries(_tabIndex).Count);
            int target = Mathf.Clamp(_spreadIndex + step, 0, spreadCount - 1);
            if (target == _spreadIndex)
            {
                return;
            }

            _spreadIndex = target;
            _selectedSlot = NoSelection;
            Rebuild();
        }

        /// <summary>Selects a pocket on the visible spread (0 to 17); empty pockets can't be selected.</summary>
        public void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= _visiblePockets.Count || _visiblePockets[slot].Entry == null)
            {
                return;
            }

            if (_selectedSlot >= 0)
            {
                _visiblePockets[_selectedSlot].SetSelected(false);
            }

            _selectedSlot = slot;
            _visiblePockets[slot].SetSelected(true);
        }

        public void RequestDetails()
        {
            BinderEntry entry = SelectedEntry;
            if (entry == null)
            {
                return;
            }

            // The card details screen is out of scope for the prototype; the event is the hook for it.
            Debug.Log($"Binder: card details requested for {entry.ItemId} ({entry.DisplayName}).", this);
            CardDetailsRequested?.Invoke(entry.ItemId);
        }

        // Only the visible spread is rebuilt, and only while open; a closed binder rebuilds on open.
        private void OnBinderChanged()
        {
            _isStale = true;
            if (_isOpen)
            {
                _root.schedule.Execute(RebuildIfStale);
            }
        }

        private void RebuildIfStale()
        {
            if (_isStale && _isOpen)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            _isStale = false;
            IReadOnlyList<BinderEntry> entries = _binder.GetEntries(_tabIndex);
            _spreadIndex = Mathf.Clamp(_spreadIndex, 0, BinderPaging.SpreadCount(entries.Count) - 1);
            BinderSpread spread = BinderPaging.GetSpread(entries, _spreadIndex);

            foreach (BinderPocket pocket in _visiblePockets)
            {
                pocket.SetSelected(false);
                _pocketPool.Release(pocket);
            }

            _visiblePockets.Clear();
            FillPage(spread.LeftPage, _leftRows);
            FillPage(spread.RightPage, _rightRows);

            if (_selectedSlot == NoSelection || _visiblePockets[_selectedSlot].Entry == null)
            {
                _selectedSlot = NoSelection;
                SelectSlot(FirstFilledSlot());
            }
            else
            {
                _visiblePockets[_selectedSlot].SetSelected(true);
            }

            IReadOnlyList<BinderTabInfo> tabs = _binder.Tabs;
            for (int i = 0; i < _tabs.Length && i < tabs.Count; i++)
            {
                _tabs[i].Count = tabs[i].Count;
                _tabs[i].Active = i == _tabIndex;
            }

            _leftPageNumber.text = spread.LeftPageNumber.ToString(CultureInfo.InvariantCulture);
            _rightPageNumber.text = spread.RightPageNumber.ToString(CultureInfo.InvariantCulture);
        }

        private void FillPage(IReadOnlyList<BinderEntry> slots, VisualElement[] rows)
        {
            for (int slot = 0; slot < slots.Count; slot++)
            {
                BinderPocket pocket = _pocketPool.Get();
                pocket.Bind(slots[slot]);
                pocket.Root.EnableInClassList(BinderPocket.SpacedClassName, slot % PocketsPerRow > 0);
                rows[slot / PocketsPerRow].Add(pocket.Root);
                _visiblePockets.Add(pocket);
            }
        }

        private int FirstFilledSlot()
        {
            for (int slot = 0; slot < _visiblePockets.Count; slot++)
            {
                if (_visiblePockets[slot].Entry != null)
                {
                    return slot;
                }
            }

            return NoSelection;
        }

        private BinderPocket CreatePocket()
        {
            var pocket = new BinderPocket();
            pocket.Root.RegisterCallback<ClickEvent>(OnPocketClicked);
            return pocket;
        }

        private void OnPocketClicked(ClickEvent evt)
        {
            if (evt.currentTarget is VisualElement element && element.userData is BinderPocket pocket)
            {
                SelectSlot(_visiblePockets.IndexOf(pocket));
            }
        }

        private bool BindElements()
        {
            VisualElement documentRoot = _document.rootVisualElement;
            _root = documentRoot == null ? null : documentRoot.Q<VisualElement>("binder");
            if (_root == null)
            {
                return false;
            }

            _tabBar = _root.Q<VisualElement>("binder-tabs");
            _leftPageNumber = _root.Q<Label>("page-left-number");
            _rightPageNumber = _root.Q<Label>("page-right-number");
            _leftRows = Rows(_root.Q<VisualElement>("page-left"));
            _rightRows = Rows(_root.Q<VisualElement>("page-right"));

            return _tabBar != null && _leftPageNumber != null && _rightPageNumber != null && _leftRows != null && _rightRows != null;
        }

        // One tab per read-model tab. The tab list is fixed for the session; only counts change.
        private void BuildTabs()
        {
            _tabBar.Clear();
            IReadOnlyList<BinderTabInfo> tabs = _binder.Tabs;
            _tabs = new KitTab[tabs.Count];
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = new KitTab { Title = tabs[i].Title, Count = tabs[i].Count };
                tab.EnableInClassList(SpacedTabClassName, i > 0);
                int tabIndex = i;
                tab.RegisterCallback<ClickEvent>(evt => ShowTab(tabIndex));
                _tabBar.Add(tab);
                _tabs[i] = tab;
            }
        }

        private static VisualElement[] Rows(VisualElement page)
        {
            if (page == null || page.childCount < RowsPerPage)
            {
                return null;
            }

            var rows = new VisualElement[RowsPerPage];
            for (int i = 0; i < RowsPerPage; i++)
            {
                rows[i] = page[i];
            }

            return rows;
        }
    }
}
