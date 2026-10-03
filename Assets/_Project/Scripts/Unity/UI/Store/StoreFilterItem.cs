using Game.Core.Store;

namespace Game.Unity.UI.Store
{
    /// <summary>What <see cref="StoreFilter"/> needs to know about one listing. Immutable.</summary>
    public sealed class StoreFilterItem
    {
        public StoreFilterItem(string id, string setId, string name, string setName, ProductType type, long unitPriceCents, int order, bool isHidden)
        {
            Id = id;
            SetId = setId ?? string.Empty;
            Name = name ?? string.Empty;
            SetName = setName ?? string.Empty;
            Type = type;
            UnitPriceCents = unitPriceCents;
            Order = order;
            IsHidden = isHidden;
        }

        public string Id { get; }

        public string SetId { get; }

        public string Name { get; }

        public string SetName { get; }

        public ProductType Type { get; }

        public long UnitPriceCents { get; }

        /// <summary>Position in the store config's listing list: the tie-breaker for every sort.</summary>
        public int Order { get; }

        public bool IsHidden { get; }
    }
}
