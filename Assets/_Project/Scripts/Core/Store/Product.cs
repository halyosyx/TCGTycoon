using System;
using Game.Core.Content;

namespace Game.Core.Store
{
    /// <summary>
    /// A sealed product the supplier sells: shared, immutable definition data (instances in the
    /// inventory hold only its id). The market price is the one authored number; the store price
    /// derives from it with <see cref="SupplierPercent"/>.
    /// </summary>
    public sealed class Product
    {
        /// <param name="pack">What one pack of this product contains. Required for booster packs.</param>
        /// <param name="pool">The cards the pack draws from. Required with <paramref name="pack"/>.</param>
        public Product(string id, string typeName, string setId, ProductType type, int packCount, long marketPriceCents, int supplierPercent, PackConfig pack, CardPool pool)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A product needs an id.", nameof(id));
            if (packCount < 1) throw new ArgumentOutOfRangeException(nameof(packCount), packCount, "A product holds at least one pack.");
            if (marketPriceCents < 0) throw new ArgumentOutOfRangeException(nameof(marketPriceCents), marketPriceCents, "A market price can't be negative.");
            if (supplierPercent < 0) throw new ArgumentOutOfRangeException(nameof(supplierPercent), supplierPercent, "A supplier percent can't be negative.");
            if ((pack == null) != (pool == null)) throw new ArgumentException("Give both a pack and its card pool, or neither.", nameof(pool));
            if (type == ProductType.BoosterPack && pack == null) throw new ArgumentException($"Booster pack '{id}' needs a pack configuration.", nameof(pack));

            Id = id;
            TypeName = string.IsNullOrEmpty(typeName) ? type.ToString() : typeName;
            SetId = setId ?? string.Empty;
            Type = type;
            PackCount = packCount;
            MarketPriceCents = marketPriceCents;
            SupplierPercent = supplierPercent;
            Pack = pack;
            Pool = pool;
        }

        public string Id { get; }

        /// <summary>The product type's display name, e.g. "Booster Pack".</summary>
        public string TypeName { get; }

        public string SetId { get; }

        public ProductType Type { get; }

        /// <summary>Packs inside one unit: 1 for a booster pack.</summary>
        public int PackCount { get; }

        /// <summary>Market price in cents. Fixed until market prices exist (F4).</summary>
        public long MarketPriceCents { get; }

        /// <summary>Share of the market price the supplier charges, in percent (100 = market price).</summary>
        public int SupplierPercent { get; }

        /// <summary>Contents of one pack; null for products that can't be opened yet.</summary>
        public PackConfig Pack { get; }

        public CardPool Pool { get; }
    }
}
