using System;

namespace Game.Core.Inventory
{
    /// <summary>
    /// Unopened units of one sealed product, and what they cost in total. Plain serialisable data with
    /// no behaviour; change it only through <see cref="InventoryService"/>.
    /// </summary>
    [Serializable]
    public sealed class SealedStack
    {
        public string ProductId;
        public int Count;
        public long CostBasisCents;
    }
}
