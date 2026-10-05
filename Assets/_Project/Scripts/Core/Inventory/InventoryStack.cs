using System;
using Game.Core.Content;

namespace Game.Core.Inventory
{
    /// <summary>
    /// Copies of one card at one tier in one location, and what they cost in total. Plain serialisable
    /// data; change it only through <see cref="InventoryService"/>.
    /// </summary>
    [Serializable]
    public sealed class InventoryStack
    {
        public string CardId;
        public RarityTier Tier;
        public ItemLocation Location;
        public int Count;
        public long CostBasisCents;

        /// <summary>Derived, never stored: a card is for sale exactly while it is in the display case.</summary>
        public bool IsForSale => Location == ItemLocation.DisplayCase;
    }
}
