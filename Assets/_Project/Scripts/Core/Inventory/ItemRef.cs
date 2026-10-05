using System;
using Game.Core.Content;

namespace Game.Core.Inventory
{
    /// <summary>
    /// Names an owned item for <see cref="InventoryService.Move"/>: a single card (card id and tier) or
    /// a sealed product (product id). Copies of the same item are interchangeable.
    /// </summary>
    public readonly struct ItemRef : IEquatable<ItemRef>
    {
        private ItemRef(ItemKind kind, string id, RarityTier tier)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Tier = tier;
        }

        public ItemKind Kind { get; }

        /// <summary>Card id for singles, product id for sealed.</summary>
        public string Id { get; }

        /// <summary>The card's tier; meaningless for sealed products.</summary>
        public RarityTier Tier { get; }

        public static ItemRef Card(string cardId, RarityTier tier) => new ItemRef(ItemKind.Card, cardId, tier);

        public static ItemRef Sealed(string productId) => new ItemRef(ItemKind.Sealed, productId, default);

        public bool Equals(ItemRef other)
        {
            return Kind == other.Kind
                && string.Equals(Id, other.Id, StringComparison.Ordinal)
                && (Kind == ItemKind.Sealed || Tier == other.Tier);
        }

        public override bool Equals(object obj) => obj is ItemRef other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Id ?? string.Empty);
                return Kind == ItemKind.Card ? (hash * 397) ^ (int)Tier : hash;
            }
        }

        public override string ToString() => Kind == ItemKind.Card ? $"{Id} ({Tier})" : $"sealed {Id}";
    }
}
