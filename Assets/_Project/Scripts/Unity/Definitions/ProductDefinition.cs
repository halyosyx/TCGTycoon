using Game.Core.Store;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Authoring asset for a sealed product the supplier sells. Converted into a Core
    /// <see cref="Product"/> at load. The market price is the one authored number: a booster pack reads
    /// it (and its card set) from its pack configuration, so Rip EV and the store can't disagree;
    /// bundles and boxes author <c>basePriceCents</c>. The store price is market × supplier percent.
    /// </summary>
    [CreateAssetMenu(menuName = "TCG/Store/Product", fileName = "NewProduct")]
    public sealed class ProductDefinition : ScriptableObject
    {
        public const string IdField = nameof(_id);
        public const string TypeDisplayNameField = nameof(_typeDisplayName);
        public const string TypeField = nameof(_type);
        public const string CardSetField = nameof(_cardSet);
        public const string PackConfigField = nameof(_packConfig);
        public const string PackCountField = nameof(_packCount);
        public const string BasePriceField = nameof(_basePriceCents);
        public const string SupplierPercentField = nameof(_supplierPercent);

        [SerializeField, Tooltip("Stable id, also the store listing's id and the ledger's item id (e.g. SetA_Pack).")]
        private string _id;

        [SerializeField, Tooltip("The product type's display name (\"Booster Pack\", \"Bundle\", \"Booster Box\").")]
        private string _typeDisplayName;

        [SerializeField]
        private ProductType _type;

        [SerializeField, Tooltip("Bundles and boxes: the set they belong to. Booster packs use their pack configuration's set.")]
        private CardSetDefinition _cardSet;

        [SerializeField, Tooltip("Booster packs: what the pack contains and its market price.")]
        private PackConfigDefinition _packConfig;

        [SerializeField, Min(1), Tooltip("Packs inside one unit: 1 for a booster pack, 6 for a bundle, 36 for a box.")]
        private int _packCount = 1;

        [SerializeField, Min(0), Tooltip("Bundles and boxes: market price in cents. Booster packs read the pack configuration's price.")]
        private long _basePriceCents;

        [SerializeField, Min(0), Tooltip("What the supplier charges, as a percentage of the market price (100 = market price).")]
        private int _supplierPercent = 100;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        public string TypeDisplayName => string.IsNullOrEmpty(_typeDisplayName) ? _type.ToString() : _typeDisplayName;

        public ProductType Type => _type;

        public bool IsPack => _type == ProductType.BoosterPack;

        public CardSetDefinition CardSet => IsPack ? (_packConfig == null ? null : _packConfig.CardSet) : _cardSet;

        public PackConfigDefinition PackConfig => _packConfig;

        public int PackCount => IsPack ? 1 : _packCount;

        public long MarketPriceCents => IsPack ? (_packConfig == null ? 0 : _packConfig.PriceCents) : _basePriceCents;

        public int SupplierPercent => _supplierPercent;

        /// <exception cref="System.InvalidOperationException">A booster pack has no pack configuration or card set.</exception>
        public Product ToProduct()
        {
            CardSetDefinition set = CardSet;
            if (IsPack && (_packConfig == null || set == null))
            {
                throw new System.InvalidOperationException($"Product '{name}' is a booster pack but has no pack configuration with a card set.");
            }

            return new Product(
                Id,
                TypeDisplayName,
                set == null ? string.Empty : set.Id,
                _type,
                PackCount,
                MarketPriceCents,
                _supplierPercent,
                IsPack ? _packConfig.ToPackConfig() : null,
                IsPack ? set.ToCardPool() : null);
        }
    }
}
