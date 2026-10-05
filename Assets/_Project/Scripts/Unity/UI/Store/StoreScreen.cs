using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Common;
using Game.Core.Economy;
using Game.Core.Session;
using Game.Core.Store;
using Game.Unity.Definitions;
using Game.Unity.Player;
using Game.Unity.UI.Controls;
using Game.Unity.UI.Hud;
using Game.Unity.UI.Shell;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Store
{
    /// <summary>
    /// The supplier website on the home computer (STORE_UI_REQUIREMENTS §4), bound to the given markup:
    /// ComputerShell.uxml with StoreSite.uxml in its content slot. Fills the empty containers, toggles
    /// the markup's state classes, and drives the keyboard modes (Browsing → CartOpen → OrderPlaced).
    /// No rules here: every change goes through <c>StoreService</c>, and the screen redraws from its
    /// <c>CartChanged</c> / <c>StockChanged</c> and <c>EconomyService.BalanceChanged</c> events.
    /// Opening hides the HUD, frees the cursor, stops the player and enables the Computer action map;
    /// closing undoes all four.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StoreScreen : MonoBehaviour
    {
        public const string OverClass = "store--over";
        public const string EmptyClass = "store--cart-empty";
        public const string PlacedClass = "store--order-placed";
        public const string BumpClass = "store-cart-button--bump";
        public const string TabClass = "store-nav__tab";
        public const string ActiveTabClass = "store-nav__tab--active";

        private const string TabFontClass = "font-display-extrabold";
        private const string PlacedLineClasses = "store-cart-line__name font-body-bold";
        private const int GridColumns = 2;
        private const int NoFocus = -1;

        [Header("Markup")]
        [SerializeField, Tooltip("UI/Shell/ComputerShell.uxml")]
        private VisualTreeAsset _shellTemplate;

        [SerializeField, Tooltip("UI/Store/StoreSite.uxml")]
        private VisualTreeAsset _siteTemplate;

        [SerializeField, Tooltip("UI/Store/StoreProductCard.uxml")]
        private VisualTreeAsset _cardTemplate;

        [SerializeField, Tooltip("UI/Store/StoreCartLine.uxml")]
        private VisualTreeAsset _cartLineTemplate;

        [Header("Tuning")]
        [SerializeField, Min(1), Tooltip("Amount stepper cap for listings with unlimited stock.")]
        private int _maxAmount = 999;

        [SerializeField, Min(0), Tooltip("Milliseconds the cart button stays bumped after Add to cart.")]
        private int _bumpMilliseconds = 120;

        private UIDocument _document;
        private PlayerControls _controls;
        private GameSession _session;
        private StoreConfigDefinition _config;
        private PlayerController _player;
        private HudPresenter _hud;

        private Dictionary<string, StoreListingInfo> _infos;
        private List<StoreFilterItem> _filterItems;
        private List<StoreFilterItem> _visible;
        private StoreFilterCriteria _criteria;
        private Dictionary<string, int> _amounts;
        private List<Button> _tabs;
        private List<string> _tabSetIds;
        private List<ProductType?> _typeChoices;

        private ComputerShellView _shell;
        private VisualElement _site;
        private CartPanelView _cart;
        private Label _balanceLabel;
        private Label _balanceValue;
        private Button _cartButton;
        private Label _cartButtonCount;
        private Label _cartButtonTotal;
        private VisualElement _tabBar;
        private TextField _search;
        private DropdownField _typeFilter;
        private DropdownField _sortFilter;
        private ScrollView _productScroll;
        private VisualElement _grid;
        private VisualElement _cartView;
        private Label _cartCount;
        private ScrollView _cartLines;
        private Label _rowBalanceValue;
        private Label _rowTotalValue;
        private Label _rowAfterValue;
        private Button _placeOrder;
        private Label _placeOrderTotal;
        private VisualElement _placedLines;

        private ObjectPool<StoreProductCardView> _cardPool;
        private List<StoreProductCardView> _cards;
        private ObjectPool<StoreCartLineView> _linePool;
        private List<StoreCartLineView> _lines;
        private StoreScreenMode _mode;
        private int _cardFocus;
        private int _lineFocus;
        private bool _isInitialized;

        public StoreScreenMode Mode => _mode;

        public bool IsOpen => _mode != StoreScreenMode.Closed;

        private StoreService Store => _session.Store;

        private EconomyService Economy => _session.Economy;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();

            // Private fields survive between Play sessions when scene reload is disabled.
            _controls = new PlayerControls();
            _session = null;
            _config = null;
            _player = null;
            _hud = null;
            _visible = new List<StoreFilterItem>();
            _criteria = new StoreFilterCriteria();
            _amounts = new Dictionary<string, int>(StringComparer.Ordinal);
            _tabs = new List<Button>();
            _tabSetIds = new List<string>();
            _typeChoices = new List<ProductType?>();
            _cards = new List<StoreProductCardView>();
            _lines = new List<StoreCartLineView>();
            _cardPool = new ObjectPool<StoreProductCardView>(CreateCard, actionOnRelease: card => card.Root.RemoveFromHierarchy(), defaultCapacity: 8);
            _linePool = new ObjectPool<StoreCartLineView>(CreateLine, actionOnRelease: line => line.Root.RemoveFromHierarchy(), defaultCapacity: 4);
            _mode = StoreScreenMode.Closed;
            _cardFocus = NoFocus;
            _lineFocus = NoFocus;
            _isInitialized = false;

            if (_shellTemplate == null || _siteTemplate == null || _cardTemplate == null || _cartLineTemplate == null)
            {
                Debug.LogError($"{name}: {nameof(StoreScreen)} needs the shell, site, product card and cart line templates.", this);
            }
        }

        /// <summary>Wires the screen to the session and the scene. Called once by <c>GameBootstrap</c>.</summary>
        /// <param name="hud">Hidden while the computer is open; may be null.</param>
        public void Initialize(GameSession session, StoreConfigDefinition config, PlayerController player, HudPresenter hud)
        {
            if (session == null || config == null || player == null
                || _shellTemplate == null || _siteTemplate == null || _cardTemplate == null || _cartLineTemplate == null)
            {
                Debug.LogError($"{name}: {nameof(StoreScreen)} is missing its session, store config, player or a template.", this);
                return;
            }

            _session = session;
            _config = config;
            _player = player;
            _hud = hud;
            _infos = StoreListingInfo.FromConfig(config);
            _filterItems = CreateFilterItems();

            if (!Build())
            {
                Debug.LogError($"{name}: the store markup is missing an element the screen binds to.", this);
                return;
            }

            Store.CartChanged += OnCartChanged;
            Store.StockChanged += OnStockChanged;
            Economy.BalanceChanged += OnBalanceChanged;
            _controls.Screens.Enable();
            _document.rootVisualElement.style.display = DisplayStyle.None;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                Store.CartChanged -= OnCartChanged;
                Store.StockChanged -= OnStockChanged;
                Economy.BalanceChanged -= OnBalanceChanged;
            }

            // Disable before disposing, or the generated wrapper warns about a leak when it is finalized.
            _controls.Computer.Disable();
            _controls.Screens.Disable();
            _controls.Dispose();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_mode == StoreScreenMode.Closed)
            {
                if (_controls.Screens.OpenComputer.WasPressedThisFrame())
                {
                    Open();
                }

                return;
            }

            PlayerControls.ComputerActions keys = _controls.Computer;
            if (IsTyping())
            {
                // Typed letters belong to the search field; Esc only leaves it.
                if (keys.Leave.WasPressedThisFrame())
                {
                    _search.Blur();
                }

                return;
            }

            switch (_mode)
            {
                case StoreScreenMode.Browsing:
                    if (keys.Leave.WasPressedThisFrame()) Close();
                    else if (keys.ToggleCart.WasPressedThisFrame()) OpenCart();
                    else if (keys.MoveLeft.WasPressedThisFrame()) MoveCardFocus(-1, 0);
                    else if (keys.MoveRight.WasPressedThisFrame()) MoveCardFocus(1, 0);
                    else if (keys.MoveUp.WasPressedThisFrame()) MoveCardFocus(0, -1);
                    else if (keys.MoveDown.WasPressedThisFrame()) MoveCardFocus(0, 1);
                    else if (keys.Decrease.WasPressedThisFrame()) StepFocusedCard(-1);
                    else if (keys.Increase.WasPressedThisFrame()) StepFocusedCard(1);
                    else if (keys.Submit.WasPressedThisFrame()) AddFocusedCard();
                    break;
                case StoreScreenMode.CartOpen:
                    if (keys.ToggleCart.WasPressedThisFrame() || keys.Leave.WasPressedThisFrame()) _cart.Close();
                    else if (keys.MoveUp.WasPressedThisFrame()) MoveLineFocus(-1);
                    else if (keys.MoveDown.WasPressedThisFrame()) MoveLineFocus(1);
                    else if (keys.Decrease.WasPressedThisFrame()) StepFocusedLine(-1);
                    else if (keys.Increase.WasPressedThisFrame()) StepFocusedLine(1);
                    else if (keys.RemoveLine.WasPressedThisFrame()) RemoveFocusedLine();
                    else if (keys.Submit.WasPressedThisFrame()) PlaceOrder();
                    break;
                case StoreScreenMode.OrderPlaced:
                    if (keys.Submit.WasPressedThisFrame() || keys.Leave.WasPressedThisFrame()) Done();
                    break;
            }
        }

        /// <summary>
        /// Opens the computer on the store. Ignored while another screen has the player's input, and
        /// while the player is holding something (put it down first).
        /// </summary>
        public bool Open()
        {
            if (!_isInitialized || _mode != StoreScreenMode.Closed || !_player.IsInGameplay
                || (_player.Hands != null && !_player.Hands.IsEmpty))
            {
                return false;
            }

            _mode = StoreScreenMode.Browsing;
            _player.SetGameplayInput(false);
            if (_hud != null) _hud.SetVisible(false);
            _controls.Computer.Enable();
            _document.rootVisualElement.style.display = DisplayStyle.Flex;

            RefreshClock();
            RefreshHeader();
            RebuildGrid();
            RebuildCart();
            _cardFocus = FirstAddableCard();
            ApplyFocus();
            RefreshHints();
            return true;
        }

        /// <summary>Leaves the computer from any mode, without losing the cart.</summary>
        public void Close()
        {
            if (_mode == StoreScreenMode.Closed)
            {
                return;
            }

            _mode = StoreScreenMode.Closed;
            _amounts.Clear();   // every card's amount is back to 0 next time (the cart itself is kept)
            _cart.Reset();
            _site.RemoveFromClassList(PlacedClass);
            _cartView.SetEnabled(true);
            _search.Blur();
            _controls.Computer.Disable();
            _document.rootVisualElement.style.display = DisplayStyle.None;
            if (_hud != null) _hud.SetVisible(true);
            _player.SetGameplayInput(true);
        }

        public void OpenCart()
        {
            if (_mode != StoreScreenMode.Browsing)
            {
                return;
            }

            _mode = StoreScreenMode.CartOpen;
            _cart.Open();
            _lineFocus = _lines.Count > 0 ? 0 : NoFocus;
            ApplyFocus();
            RefreshHints();
        }

        public void PlaceOrder()
        {
            if (_mode != StoreScreenMode.CartOpen || Store.CanPlaceOrder() != OrderCheck.Ok)
            {
                return;
            }

            OrderResult result = Store.PlaceOrder();
            if (!result.IsSuccess)
            {
                return;
            }

            // The packs are in the inventory now; PackStackSpawner puts their stacks on the home table.
            _placedLines.Clear();
            foreach (CartLine line in result.Lines)
            {
                StoreListingInfo info = _infos[line.ListingId];
                var label = new Label(string.Format(CultureInfo.InvariantCulture, _config.Strings.PlacedLineFormat, line.Quantity, info.SetName, info.Name));
                foreach (string className in PlacedLineClasses.Split(' '))
                {
                    label.AddToClassList(className);
                }

                _placedLines.Add(label);
            }

            _mode = StoreScreenMode.OrderPlaced;
            _cartView.SetEnabled(false);
            _site.AddToClassList(PlacedClass);
            _amounts.Clear();   // a new order starts from 0 on every card
            RebuildGrid();
            RefreshHints();
        }

        /// <summary>Done on the Order placed view: back to browsing, closing the cart.</summary>
        public void Done()
        {
            if (_mode != StoreScreenMode.OrderPlaced)
            {
                return;
            }

            _cart.Close();
        }

        // --- Building ---

        private bool Build()
        {
            VisualElement root = _document.rootVisualElement;
            root.Clear();

            TemplateContainer shellTree = _shellTemplate.Instantiate();
            shellTree.style.position = Position.Absolute;
            shellTree.style.left = 0;
            shellTree.style.top = 0;
            shellTree.style.right = 0;
            shellTree.style.bottom = 0;
            root.Add(shellTree);
            _shell = new ComputerShellView(shellTree);

            TemplateContainer siteTree = _siteTemplate.Instantiate();
            siteTree.style.flexGrow = 1;
            _shell.ContentSlot.Add(siteTree);
            _site = siteTree.Q<VisualElement>("store-site");
            if (_site == null)
            {
                return false;
            }

            _balanceLabel = _site.Q<Label>("balance-label");
            _balanceValue = _site.Q<Label>("balance-value");
            _cartButton = _site.Q<Button>("cart-button");
            _cartButtonCount = _site.Q<Label>("cart-button-count");
            _cartButtonTotal = _site.Q<Label>("cart-button-total");
            _tabBar = _site.Q<VisualElement>("set-tabs");
            _search = _site.Q<TextField>("search-field");
            _typeFilter = _site.Q<DropdownField>("type-filter");
            _sortFilter = _site.Q<DropdownField>("sort-filter");
            _productScroll = _site.Q<ScrollView>("product-scroll");
            _grid = _site.Q<VisualElement>("product-grid");
            _cartView = _site.Q<VisualElement>("cart-view");
            _cartCount = _site.Q<Label>("cart-count");
            _cartLines = _site.Q<ScrollView>("cart-lines");
            _rowBalanceValue = _site.Q<Label>("row-balance-value");
            _rowTotalValue = _site.Q<Label>("row-total-value");
            _rowAfterValue = _site.Q<Label>("row-after-value");
            _placeOrder = _site.Q<Button>("place-order");
            _placeOrderTotal = _site.Q<Label>("place-order-total");
            _placedLines = _site.Q<VisualElement>("placed-lines");
            Button done = _site.Q<Button>("order-done");
            if (_balanceLabel == null || _balanceValue == null || _cartButton == null || _cartButtonCount == null || _cartButtonTotal == null
                || _tabBar == null || _search == null || _typeFilter == null || _sortFilter == null || _productScroll == null || _grid == null
                || _cartView == null || _cartCount == null || _cartLines == null || _rowBalanceValue == null || _rowTotalValue == null
                || _rowAfterValue == null || _placeOrder == null || _placeOrderTotal == null || _placedLines == null || done == null)
            {
                return false;
            }

            _cart = new CartPanelView(_site);
            _cart.Closed += OnCartClosed;
            _cartButton.clicked += OnCartButtonClicked;
            _placeOrder.clicked += PlaceOrder;
            done.clicked += Done;

            ApplyStrings();
            BuildTabs();
            BuildFilters();
            return true;
        }

        // Every visible string comes from the store config (STR-01); the UXML text is sample text.
        private void ApplyStrings()
        {
            StoreStrings strings = _config.Strings;
            _shell.SetPage(_config.BrowserTabTitle, _config.AddressText);
            SetText("site-title", _config.SiteTitle);
            SetText("site-tagline", _config.SiteTagline);
            _balanceLabel.text = strings.YourBalance;
            SetText("cart-button-label", strings.Cart);
            SetText("type-label", strings.Type);
            SetText("sort-label", strings.Sort);
            SetText("cart-title", strings.YourCart);
            SetText("cart-count-word", strings.Packs);
            SetText("cart-empty", strings.CartEmpty);
            SetText("row-balance-label", strings.Balance);
            SetText("row-total-label", strings.Total);
            SetText("row-after-label", strings.BalanceAfter);
            SetText("place-order-label", strings.PlaceOrder);
            SetText("order-placed-title", strings.OrderPlaced);
            SetText("order-placed-note", strings.OrderPlacedNote);
            SetText("order-done-label", strings.Done);
            _search.textEdition.placeholder = strings.SearchProducts;
        }

        private void SetText(string elementName, string text)
        {
            Label label = _site.Q<Label>(elementName);
            if (label != null)
            {
                label.text = text;
            }
        }

        // "All sets", then one tab per card set in listing order, labelled with its short name (STR-05).
        private void BuildTabs()
        {
            _tabBar.Clear();
            _tabs.Clear();
            _tabSetIds.Clear();
            AddTab(_config.Strings.AllSets, null);
            foreach (StoreFilterItem item in _filterItems)
            {
                if (!item.IsHidden && !string.IsNullOrEmpty(item.SetId) && !_tabSetIds.Contains(item.SetId))
                {
                    AddTab(_infos[item.Id].SetShortName, item.SetId);
                }
            }

            RefreshTabs();
        }

        private void AddTab(string title, string setId)
        {
            var tab = new Button { text = title };
            tab.AddToClassList(TabClass);
            tab.AddToClassList(TabFontClass);
            tab.clicked += () => SelectTab(setId);
            _tabBar.Add(tab);
            _tabs.Add(tab);
            _tabSetIds.Add(setId);
        }

        private void SelectTab(string setId)
        {
            _criteria.SetId = setId;
            RefreshTabs();
            RebuildGrid();
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].EnableInClassList(ActiveTabClass, string.Equals(_tabSetIds[i], _criteria.SetId, StringComparison.Ordinal));
            }
        }

        // Type: "Any" then each product type the store shows (Hidden listings don't count), named by its
        // product. Sort: Price, Name.
        private void BuildFilters()
        {
            StoreStrings strings = _config.Strings;
            var typeNames = new List<string> { strings.Any };
            _typeChoices.Clear();
            _typeChoices.Add(null);
            foreach (ProductType type in (ProductType[])Enum.GetValues(typeof(ProductType)))
            {
                foreach (StoreFilterItem item in _filterItems)
                {
                    if (!item.IsHidden && item.Type == type)
                    {
                        typeNames.Add(_infos[item.Id].TypeName);
                        _typeChoices.Add(type);
                        break;
                    }
                }
            }

            _typeFilter.choices = typeNames;
            _typeFilter.index = 0;
            _typeFilter.RegisterValueChangedCallback(_ =>
            {
                _criteria.Type = _typeFilter.index >= 0 && _typeFilter.index < _typeChoices.Count ? _typeChoices[_typeFilter.index] : null;
                RebuildGrid();
            });

            _sortFilter.choices = new List<string> { strings.SortPrice, strings.SortName };
            _sortFilter.index = (int)StoreSort.Price;
            _sortFilter.RegisterValueChangedCallback(_ =>
            {
                _criteria.Sort = _sortFilter.index == (int)StoreSort.Name ? StoreSort.Name : StoreSort.Price;
                RebuildGrid();
            });

            _search.RegisterValueChangedCallback(evt =>
            {
                _criteria.Search = evt.newValue;
                RebuildGrid();
            });
        }

        private List<StoreFilterItem> CreateFilterItems()
        {
            var items = new List<StoreFilterItem>();
            foreach (StoreListingState listing in Store.Listings)
            {
                if (_infos.TryGetValue(listing.Id, out StoreListingInfo info))
                {
                    items.Add(new StoreFilterItem(listing.Id, info.SetId, info.Name, info.SetName, info.Type, listing.UnitPriceCents, info.Order,
                        listing.Availability == ListingAvailability.Hidden));
                }
            }

            return items;
        }

        private StoreProductCardView CreateCard()
        {
            var card = new StoreProductCardView(_cardTemplate);
            card.AddRequested += AddToCart;
            card.AmountChanged += (view, amount) => _amounts[view.ListingId] = amount;
            card.Pressed += OnCardPressed;
            return card;
        }

        private StoreCartLineView CreateLine()
        {
            var line = new StoreCartLineView(_cartLineTemplate);
            line.QuantityChanged += (view, quantity) => Store.SetQuantity(view.ListingId, quantity);
            line.RemoveRequested += view => Store.Remove(view.ListingId);
            line.Pressed += OnLinePressed;
            return line;
        }

        // --- Redrawing ---

        private void OnCartChanged()
        {
            RefreshHeader();
            RebuildCart();
        }

        private void OnStockChanged()
        {
            RebuildGrid();
            RebuildCart();
            RefreshHeader();
        }

        private void OnBalanceChanged(BalanceChange change) => RefreshHeader();

        // Balance, cart button and cart totals, and the over-balance and empty states (colour only:
        // the stylesheets do the red treatment; no sentence is ever shown, STR-24).
        private void RefreshHeader()
        {
            long balance = Economy.BalanceCents;
            long subtotal = Store.SubtotalCents;
            string count = Store.ItemCount.ToString(CultureInfo.InvariantCulture);
            bool canOrder = Store.CanPlaceOrder() == OrderCheck.Ok;

            _balanceValue.text = Money.FormatDisplay(balance);
            _cartButtonCount.text = count;
            _cartButtonTotal.text = Money.FormatDisplay(subtotal);
            _cartCount.text = count;
            _rowBalanceValue.text = Money.FormatDisplay(balance);
            _rowTotalValue.text = Money.FormatDisplay(subtotal);
            _rowAfterValue.text = Money.FormatDisplay(balance - subtotal);
            _placeOrderTotal.text = canOrder ? Money.FormatDisplay(subtotal) : string.Empty;
            _site.EnableInClassList(OverClass, subtotal > balance);
            _site.EnableInClassList(EmptyClass, Store.Lines.Count == 0);
            _placeOrder.SetEnabled(canOrder);
        }

        private void RefreshClock()
        {
            // TODO(F2 day cycle): read the day and time from the GameStateMachine, like the HUD will.
            int day = _hud != null ? _hud.Day : 1;
            string time = DayClock.FormatTime(DayClock.PrepNightHour, DayClock.PrepNightMinute, out string meridiem);
            _shell.SetClock(time + " " + meridiem, string.Format(CultureInfo.InvariantCulture, "Day {0} · {1}", day, DayClock.PrepNightName));
        }

        private void RebuildGrid()
        {
            if (_mode == StoreScreenMode.Closed)
            {
                return;
            }

            StoreFilter.Apply(_filterItems, _criteria, _visible);
            foreach (StoreProductCardView card in _cards)
            {
                _cardPool.Release(card);
            }

            _cards.Clear();
            foreach (StoreFilterItem item in _visible)
            {
                if (!Store.TryGetListing(item.Id, out StoreListingState listing))
                {
                    continue;
                }

                StoreProductCardView card = _cardPool.Get();
                card.Bind(_infos[item.Id], listing, _config.Strings, _maxAmount, _amounts.TryGetValue(item.Id, out int amount) ? amount : 0);
                _grid.Add(card.Root);
                _cards.Add(card);
            }

            _cardFocus = _cards.Count == 0 ? NoFocus : Mathf.Clamp(_cardFocus, 0, _cards.Count - 1);
            ApplyFocus();
        }

        // Lines re-bind in place when the cart keeps the same listings in the same order, so a click
        // on a stepper never rebuilds the line under the pointer.
        private void RebuildCart()
        {
            if (_mode == StoreScreenMode.Closed)
            {
                return;
            }

            IReadOnlyList<CartLine> lines = Store.Lines;
            bool isSameLines = lines.Count == _lines.Count;
            for (int i = 0; isSameLines && i < lines.Count; i++)
            {
                isSameLines = string.Equals(lines[i].ListingId, _lines[i].ListingId, StringComparison.Ordinal);
            }

            if (!isSameLines)
            {
                foreach (StoreCartLineView view in _lines)
                {
                    _linePool.Release(view);
                }

                _lines.Clear();
                for (int i = 0; i < lines.Count; i++)
                {
                    StoreCartLineView view = _linePool.Get();
                    _cartLines.Add(view.Root);
                    _lines.Add(view);
                }
            }

            for (int i = 0; i < lines.Count; i++)
            {
                CartLine line = lines[i];
                Store.TryGetListing(line.ListingId, out StoreListingState listing);
                int max = listing == null || listing.IsUnlimited ? _maxAmount : listing.StockRemaining;
                _lines[i].Bind(_infos[line.ListingId], line, max, _config.Strings);
            }

            _lineFocus = _lines.Count == 0 ? NoFocus : Mathf.Clamp(_lineFocus, 0, _lines.Count - 1);
            ApplyFocus();
        }

        private void RefreshHints()
        {
            StoreStrings strings = _config.Strings;
            PlayerControls.ComputerActions keys = _controls.Computer;
            string enter = KeyText(keys.Submit);
            string hints;
            switch (_mode)
            {
                case StoreScreenMode.CartOpen:
                    hints = $"icon:arrow-up|icon:arrow-down:{strings.HintLine};icon:minus|icon:plus:{strings.HintQuantity};" +
                            $"{KeyText(keys.RemoveLine)}:{strings.HintRemove};{enter}:{strings.PlaceOrder};{KeyText(keys.ToggleCart)}:{strings.HintCloseCart}";
                    break;
                case StoreScreenMode.OrderPlaced:
                    hints = $"{enter}:{strings.Done}";
                    break;
                default:
                    hints = $"icon:arrow-left|icon:arrow-right|icon:arrow-up|icon:arrow-down:{strings.HintMove};icon:minus|icon:plus:{strings.Amount};" +
                            $"{enter}:{strings.AddToCart};{KeyText(keys.ToggleCart)}:{strings.Cart};{KeyText(keys.Leave)}:{strings.HintLeave}";
                    break;
            }

            _shell.SetHints(hints);
        }

        // The keycap shows the action's first binding, so the hint follows the input asset.
        private static string KeyText(InputAction action) => action.GetBindingDisplayString(0);

        // --- Cart and focus ---

        private void OnCartButtonClicked()
        {
            if (_mode == StoreScreenMode.Browsing)
            {
                OpenCart();
            }
            else
            {
                _cart.Close();
            }
        }

        // Every way the cart closes (C, Esc, the scrim, the cart button, Done) ends here.
        private void OnCartClosed()
        {
            if (_mode != StoreScreenMode.CartOpen && _mode != StoreScreenMode.OrderPlaced)
            {
                return;
            }

            _mode = StoreScreenMode.Browsing;
            _site.RemoveFromClassList(PlacedClass);
            _cartView.SetEnabled(true);
            ApplyFocus();
            RefreshHints();
        }

        private void AddToCart(StoreProductCardView card)
        {
            if (_mode != StoreScreenMode.Browsing || !card.CanAdd)
            {
                return;
            }

            if (card.Amount <= 0)
            {
                return;
            }

            int before = Store.ItemCount;
            Store.Add(card.ListingId, card.Amount);
            if (Store.ItemCount != before)
            {
                _cartButton.AddToClassList(BumpClass);
                _cartButton.schedule.Execute(() => _cartButton.RemoveFromClassList(BumpClass)).StartingIn(_bumpMilliseconds);
            }
        }

        private void OnCardPressed(StoreProductCardView card)
        {
            if (_mode == StoreScreenMode.Browsing)
            {
                _cardFocus = _cards.IndexOf(card);
                ApplyFocus();
            }
        }

        private void OnLinePressed(StoreCartLineView line)
        {
            if (_mode == StoreScreenMode.CartOpen)
            {
                _lineFocus = _lines.IndexOf(line);
                ApplyFocus();
            }
        }

        // Row-major, two per row; moving past an edge stays put.
        private void MoveCardFocus(int columnStep, int rowStep)
        {
            if (_cards.Count == 0)
            {
                return;
            }

            int current = Mathf.Max(_cardFocus, 0);
            int column = current % GridColumns;
            int next = current;
            if (columnStep != 0 && column + columnStep >= 0 && column + columnStep < GridColumns)
            {
                next = current + columnStep;
            }
            else if (rowStep != 0)
            {
                next = current + rowStep * GridColumns;
            }

            if (next >= 0 && next < _cards.Count)
            {
                _cardFocus = next;
                ApplyFocus();
                _productScroll.ScrollTo(_cards[_cardFocus].Root);
            }
        }

        private void MoveLineFocus(int step)
        {
            int next = _lineFocus + step;
            if (next >= 0 && next < _lines.Count)
            {
                _lineFocus = next;
                ApplyFocus();
                _cartLines.ScrollTo(_lines[_lineFocus].Root);
            }
        }

        private void StepFocusedCard(int delta)
        {
            if (_cardFocus >= 0 && _cardFocus < _cards.Count && _cards[_cardFocus].CanAdd)
            {
                _cards[_cardFocus].StepAmount(delta);
            }
        }

        private void AddFocusedCard()
        {
            if (_cardFocus >= 0 && _cardFocus < _cards.Count)
            {
                AddToCart(_cards[_cardFocus]);
            }
        }

        private void StepFocusedLine(int delta)
        {
            if (_lineFocus >= 0 && _lineFocus < _lines.Count)
            {
                _lines[_lineFocus].StepQuantity(delta);
            }
        }

        private void RemoveFocusedLine()
        {
            if (_lineFocus >= 0 && _lineFocus < _lines.Count)
            {
                Store.Remove(_lines[_lineFocus].ListingId);
            }
        }

        // The focus ring shows on a card while browsing and on a cart line while the cart is open; the
        // card focus is kept, so closing the cart returns to the last focused card.
        private void ApplyFocus()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].SetFocused(_mode == StoreScreenMode.Browsing && i == _cardFocus);
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                _lines[i].SetFocused(_mode == StoreScreenMode.CartOpen && i == _lineFocus);
            }
        }

        private int FirstAddableCard()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].CanAdd)
                {
                    return i;
                }
            }

            return _cards.Count > 0 ? 0 : NoFocus;
        }

        private bool IsTyping()
        {
            Focusable focused = _search.focusController == null ? null : _search.focusController.focusedElement;
            return focused is VisualElement element && (element == _search || _search.Contains(element));
        }
    }
}
