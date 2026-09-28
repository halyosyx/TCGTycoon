using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.UI.Controls;

namespace Game.Unity.UI
{
    /// <summary>
    /// <see cref="IBinderReadModel"/> over the player's <see cref="InventoryService"/>. Sorts owned
    /// singles into tabs: Rare and above go to their set's tab, Commons and Uncommons of either set to
    /// Bulk (GDD: they are bulk). Rebuilds lazily after the inventory raises Changed, so reading is
    /// cheap and nothing is rebuilt while the binder is closed. Holds no game rules of its own; the
    /// bulk rule is <see cref="RarityTiers.IsBulk"/>. Dispose it to stop listening to the inventory.
    /// </summary>
    public sealed class InventoryBinderReadModel : IBinderReadModel, IDisposable
    {
        private static readonly BinderTab[] s_tabOrder = { BinderTab.SetA, BinderTab.SetB, BinderTab.Sealed, BinderTab.Bulk };
        private static readonly string[] s_tabTitles = { "Set A", "Set B", "Sealed", "Bulk" };
        private static readonly Comparison<BinderEntry> s_byTierThenId = CompareEntries;

        private readonly InventoryService _inventory;
        private readonly CardPool _pool;
        private readonly string _setAId;
        private readonly string _setBId;
        private readonly List<BinderEntry>[] _entriesByTab;
        private readonly BinderTabInfo[] _tabs;
        private bool _isStale;
        private bool _isDisposed;

        /// <param name="setAId">Card set id shown on the Set A tab (e.g. "SetA").</param>
        /// <param name="setBId">Card set id shown on the Set B tab; its tab stays empty while no such cards exist.</param>
        public InventoryBinderReadModel(InventoryService inventory, CardPool pool, string setAId, string setBId)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _setAId = setAId ?? string.Empty;
            _setBId = setBId ?? string.Empty;

            _entriesByTab = new List<BinderEntry>[s_tabOrder.Length];
            for (int i = 0; i < _entriesByTab.Length; i++)
            {
                _entriesByTab[i] = new List<BinderEntry>();
            }

            _tabs = new BinderTabInfo[s_tabOrder.Length];
            _isStale = true;
            _inventory.Changed += OnInventoryChanged;
        }

        public event Action Changed;

        public IReadOnlyList<BinderTabInfo> Tabs
        {
            get
            {
                RebuildIfStale();
                return _tabs;
            }
        }

        public IReadOnlyList<BinderEntry> GetEntries(BinderTab tab)
        {
            RebuildIfStale();
            return _entriesByTab[(int)tab];
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _inventory.Changed -= OnInventoryChanged;
        }

        private void OnInventoryChanged()
        {
            _isStale = true;
            Changed?.Invoke();
        }

        private void RebuildIfStale()
        {
            if (!_isStale)
            {
                return;
            }

            foreach (List<BinderEntry> entries in _entriesByTab)
            {
                entries.Clear();
            }

            foreach (InventoryStack stack in _inventory.Stacks)
            {
                if (stack.Count <= 0 || !TryGetTab(stack, out BinderTab tab, out string name))
                {
                    continue;
                }

                _entriesByTab[(int)tab].Add(new BinderEntry(stack.CardId, name, TierDisplay.FromRarity(stack.Tier), stack.Count));
            }

            // TODO(sealed products): the inventory holds only singles today. When owned packs, bundles and
            // boxes exist, add them to the Sealed tab here with a null tier.
            for (int i = 0; i < s_tabOrder.Length; i++)
            {
                _entriesByTab[i].Sort(s_byTierThenId);
                _tabs[i] = new BinderTabInfo(s_tabOrder[i], s_tabTitles[i], _entriesByTab[i].Count);
            }

            _isStale = false;
        }

        // Stacks don't record their set, so the card pool supplies it (and the name).
        private bool TryGetTab(InventoryStack stack, out BinderTab tab, out string name)
        {
            bool isKnown = _pool.TryGetCard(stack.CardId, out Card card);
            name = isKnown ? card.DisplayName : stack.CardId;
            string setId = isKnown ? card.SetId : string.Empty;
            bool isFromBinderSet = string.Equals(setId, _setAId, StringComparison.Ordinal) || string.Equals(setId, _setBId, StringComparison.Ordinal);

            if (RarityTiers.IsBulk(stack.Tier) && isFromBinderSet)
            {
                tab = BinderTab.Bulk;
                return true;
            }

            if (string.Equals(setId, _setAId, StringComparison.Ordinal))
            {
                tab = BinderTab.SetA;
                return true;
            }

            if (string.Equals(setId, _setBId, StringComparison.Ordinal))
            {
                tab = BinderTab.SetB;
                return true;
            }

            tab = default;
            return false;
        }

        private static int CompareEntries(BinderEntry left, BinderEntry right)
        {
            int leftTier = left.Tier ?? 0;
            int rightTier = right.Tier ?? 0;
            int byTier = rightTier.CompareTo(leftTier);
            return byTier != 0 ? byTier : string.CompareOrdinal(left.ItemId, right.ItemId);
        }
    }
}
