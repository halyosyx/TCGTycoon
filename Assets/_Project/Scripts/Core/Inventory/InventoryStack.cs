using System;
using Game.Core.Content;

namespace Game.Core.Inventory
{
    /// <summary>
    /// Copies of one card at one tier, and what they cost in total. Plain serialisable data with no
    /// behaviour; change it only through <see cref="InventoryService"/>.
    /// </summary>
    [Serializable]
    public sealed class InventoryStack
    {
        public string CardId;
        public RarityTier Tier;
        public int Count;
        public long CostBasisCents;
    }
}
