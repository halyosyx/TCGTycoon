using System;
using System.Collections.Generic;

namespace Game.Core.Inventory
{
    /// <summary>Everything the player owns, as plain serialisable data (ready for a future save).</summary>
    [Serializable]
    public sealed class InventoryState
    {
        public List<InventoryStack> Stacks = new List<InventoryStack>();

        public List<SealedStack> SealedStacks = new List<SealedStack>();
    }
}
