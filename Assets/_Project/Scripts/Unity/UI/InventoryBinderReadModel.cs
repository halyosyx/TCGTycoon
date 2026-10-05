using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Store;
using Game.Unity.UI.Controls;
using static System.FormattableString;

namespace Game.Unity.UI
{
    /// <summary>
    /// <see cref="IBinderReadModel"/> over the player's <see cref="InventoryService"/>. One tab per card
    /// set it is given, in that order, then Sealed. Every owned single of a set, Common included, goes
    /// on that set's tab, rarest first; cards of sets without a tab are left out. Owned sealed products
    /// go on Sealed, named by the product lookup it is given. Rebuilds lazily after
    /// the inventory raises Changed, so reading is cheap and nothing is rebuilt while the binder is
    /// closed. Holds no game rules. Dispose it to stop listening to the inventory.
    /// </summary>
    public sealed class InventoryBinderReadModel : IBinderReadModel, IDisposable
    {
        /// <summary>The Sealed tab's <see cref="BinderTabInfo.Id"/>.</summary>
        public const string SealedTabId = "Sealed";

        private const string SealedTitle = "Sealed";

        private static readonly Comparison<BinderEntry> s_byTierThenId = CompareEntries;

        private readonly InventoryService _inventory;
        private readonly CardPool _cards;
        private readonly List<BinderSet> _sets = new List<BinderSet>();
        private readonly Dictionary<string, int> _tabIndexBySetId = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Product> _productsById = new Dictionary<string, Product>(StringComparer.Ordinal);
        private readonly List<BinderEntry>[] _entriesByTab;
        private readonly BinderTabInfo[] _tabs;
        private readonly int _sealedTabIndex;
        private bool _isStale;
        private bool _isDisposed;

        /// <param name="cards">Looks up each owned card's set and name; holds the cards of every binder set.</param>
        /// <param name="sets">The card sets that get a tab, in tab order. Empty entries and repeated ids are skipped.</param>
        /// <param name="products">Names sealed products on the Sealed tab; an unknown product shows its id.</param>
        public InventoryBinderReadModel(InventoryService inventory, CardPool cards, IEnumerable<BinderSet> sets, IEnumerable<Product> products = null)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            if (sets == null) throw new ArgumentNullException(nameof(sets));

            if (products != null)
            {
                foreach (Product product in products)
                {
                    if (product != null && !_productsById.ContainsKey(product.Id))
                    {
                        _productsById.Add(product.Id, product);
                    }
                }
            }

            foreach (BinderSet set in sets)
            {
                if (set != null && !_tabIndexBySetId.ContainsKey(set.SetId))
                {
                    _tabIndexBySetId.Add(set.SetId, _sets.Count);
                    _sets.Add(set);
                }
            }

            _sealedTabIndex = _sets.Count;
            _entriesByTab = new List<BinderEntry>[_sets.Count + 1];
            for (int i = 0; i < _entriesByTab.Length; i++)
            {
                _entriesByTab[i] = new List<BinderEntry>();
            }

            _tabs = new BinderTabInfo[_entriesByTab.Length];
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

        public IReadOnlyList<BinderEntry> GetEntries(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _entriesByTab.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(tabIndex), tabIndex, $"The binder has {_entriesByTab.Length} tabs.");
            }

            RebuildIfStale();
            return _entriesByTab[tabIndex];
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

            // Stacks don't record their set, so the card lookup supplies it (and the name).
            // Only what is stored in the binder: held cards, cased cards and set-down packs are elsewhere.
            foreach (InventoryStack stack in _inventory.Stacks)
            {
                if (stack.Count > 0
                    && stack.Location == ItemLocation.Binder
                    && _cards.TryGetCard(stack.CardId, out Card card)
                    && _tabIndexBySetId.TryGetValue(card.SetId, out int tabIndex))
                {
                    _entriesByTab[tabIndex].Add(new BinderEntry(stack.CardId, card.DisplayName, TierDisplay.FromRarity(stack.Tier), stack.Count));
                }
            }

            foreach (SealedStack stack in _inventory.SealedStacks)
            {
                if (stack.Count > 0 && stack.Location == ItemLocation.Binder)
                {
                    _entriesByTab[_sealedTabIndex].Add(CreateSealedEntry(stack));
                }
            }

            for (int i = 0; i < _entriesByTab.Length; i++)
            {
                _entriesByTab[i].Sort(s_byTierThenId);
                _tabs[i] = i == _sealedTabIndex
                    ? new BinderTabInfo(BinderTabKind.Sealed, SealedTabId, SealedTitle, _entriesByTab[i].Count)
                    : new BinderTabInfo(BinderTabKind.CardSet, _sets[i].SetId, _sets[i].Title, _entriesByTab[i].Count);
            }

            _isStale = false;
        }

        // Sealed entries have no tier. Name: the product type; detail: the set's tab title, plus the
        // pack count for multi-pack products ("Set A · 36 packs").
        private BinderEntry CreateSealedEntry(SealedStack stack)
        {
            if (!_productsById.TryGetValue(stack.ProductId, out Product product))
            {
                return new BinderEntry(stack.ProductId, stack.ProductId, null, stack.Count);
            }

            string setTitle = _tabIndexBySetId.TryGetValue(product.SetId, out int tabIndex) ? _sets[tabIndex].Title : product.SetId;
            string detail = product.PackCount > 1 ? Invariant($"{setTitle} · {product.PackCount} packs") : setTitle;
            return new BinderEntry(stack.ProductId, product.TypeName, null, stack.Count, detail);
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
