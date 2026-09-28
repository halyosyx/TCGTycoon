using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Session;
using Game.Unity.Definitions;
using Game.Unity.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Collection
{
    /// <summary>
    /// Inventory screen: a plain list of owned stacks (tier, card, count), highest tier first. Reads
    /// the session's inventory each time it opens; it never changes it.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class InventoryScreen : MonoBehaviour
    {
        private const string RootName = "inventory";
        private const string ListName = "inventory-list";
        private const string SummaryName = "inventory-summary";
        private const string EmptyName = "inventory-empty";
        private const string CloseButtonName = "inventory-close";
        private const string RowClassName = "inventory-row";
        private const string TierCellClassName = "inventory-row__tier";
        private const string NameCellClassName = "inventory-row__name";
        private const string CountCellClassName = "inventory-row__count";
        private const int InitialRowCapacity = 64;

        // Highest tier first, then alphabetical, so the hits sit at the top.
        private static readonly Comparison<InventoryRow> s_byTierThenName = CompareRows;

        [SerializeField, Min(16f), Tooltip("Height of one list row in panel pixels.")]
        private float _rowHeight = 40f;

        private UIDocument _document;
        private VisualElement _root;
        private ListView _list;
        private Label _summary;
        private Label _empty;
        private Button _closeButton;
        private PlayerControls _controls;
        private List<InventoryRow> _rows;

        private GameSession _session;
        private RarityPaletteDefinition _palette;
        private PlayerController _player;
        private bool _isInitialized;

        public bool IsOpen { get; private set; }

        /// <summary>The rows shown the last time the screen opened, for tests and debugging.</summary>
        public IReadOnlyList<InventoryRow> Rows => _rows;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();

            // Private fields survive between Play sessions when scene reload is disabled.
            _controls = new PlayerControls();
            _rows = new List<InventoryRow>(InitialRowCapacity);
            _isInitialized = false;
            IsOpen = false;
        }

        /// <summary>Wires the screen to the session and the player. Called once by <c>GameBootstrap</c>.</summary>
        public void Initialize(GameSession session, RarityPaletteDefinition palette, PlayerController player)
        {
            _session = session;
            _palette = palette;
            _player = player;
            if (_session == null || _palette == null || _player == null)
            {
                Debug.LogError($"{name}: {nameof(InventoryScreen)} is missing its session, palette or player.", this);
                return;
            }

            if (!BindElements())
            {
                return;
            }

            _controls.Screens.Enable();
            SetVisible(false);
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked -= Close;
            }

            _controls.Dispose();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            PlayerControls.ScreensActions actions = _controls.Screens;
            if (actions.ToggleInventory.WasPressedThisFrame())
            {
                if (IsOpen) Close();
                else Open();
            }
            else if (IsOpen && actions.Dismiss.WasPressedThisFrame())
            {
                Close();
            }
        }

        /// <summary>Shows the inventory. Ignored while another screen has the player's input.</summary>
        public void Open()
        {
            if (!_isInitialized || IsOpen || !_player.IsInGameplay)
            {
                return;
            }

            RefreshRows();
            IsOpen = true;
            SetVisible(true);
            _player.SetGameplayInput(false);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            SetVisible(false);
            _player.SetGameplayInput(true);
        }

        private void RefreshRows()
        {
            _rows.Clear();
            int cardCount = 0;
            foreach (InventoryStack stack in _session.Inventory.Stacks)
            {
                string cardName = _session.Pool.TryGetCard(stack.CardId, out Card card) ? card.DisplayName : stack.CardId;
                _rows.Add(new InventoryRow(cardName, stack.Tier, stack.Count));
                cardCount += stack.Count;
            }

            _rows.Sort(s_byTierThenName);
            _summary.text = string.Format(CultureInfo.InvariantCulture, "{0} cards in {1} stacks", cardCount, _rows.Count);
            _empty.style.display = _rows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _list.RefreshItems();
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList(RowClassName);
            var cells = new RowCells(AddCell(row, TierCellClassName), AddCell(row, NameCellClassName), AddCell(row, CountCellClassName));
            row.userData = cells;
            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            var cells = (RowCells)element.userData;
            InventoryRow row = _rows[index];
            cells.Tier.text = _palette.DisplayNameOf(row.Tier);
            cells.Tier.style.color = _palette.ColorOf(row.Tier);
            cells.Name.text = row.CardName;
            cells.Count.text = row.Count.ToString(CultureInfo.InvariantCulture);
        }

        private bool BindElements()
        {
            VisualElement documentRoot = _document.rootVisualElement;
            _root = documentRoot == null ? null : documentRoot.Q<VisualElement>(RootName);
            _list = _root == null ? null : _root.Q<ListView>(ListName);
            _summary = _root == null ? null : _root.Q<Label>(SummaryName);
            _empty = _root == null ? null : _root.Q<Label>(EmptyName);
            _closeButton = _root == null ? null : _root.Q<Button>(CloseButtonName);
            if (_root == null || _list == null || _summary == null || _empty == null || _closeButton == null)
            {
                Debug.LogError($"{name}: the UI document is missing an element of the inventory screen ('{RootName}', '{ListName}', '{SummaryName}', '{EmptyName}' or '{CloseButtonName}').", this);
                return false;
            }

            _list.fixedItemHeight = _rowHeight;
            _list.selectionType = SelectionType.None;
            _list.makeItem = MakeRow;
            _list.bindItem = BindRow;
            _list.itemsSource = _rows;
            _closeButton.focusable = false;
            _closeButton.clicked += Close;
            return true;
        }

        private void SetVisible(bool isVisible)
        {
            _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static Label AddCell(VisualElement row, string className)
        {
            var cell = new Label();
            cell.AddToClassList(className);
            row.Add(cell);
            return cell;
        }

        private static int CompareRows(InventoryRow left, InventoryRow right)
        {
            int byTier = right.Tier.CompareTo(left.Tier);
            return byTier != 0 ? byTier : string.Compare(left.CardName, right.CardName, StringComparison.Ordinal);
        }

        /// <summary>The labels of one recycled list row, kept on the row so binding needs no queries.</summary>
        private sealed class RowCells
        {
            public RowCells(Label tier, Label name, Label count)
            {
                Tier = tier;
                Name = name;
                Count = count;
            }

            public Label Tier { get; }

            public Label Name { get; }

            public Label Count { get; }
        }
    }
}
