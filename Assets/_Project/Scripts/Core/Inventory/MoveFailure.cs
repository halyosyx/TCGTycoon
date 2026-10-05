namespace Game.Core.Inventory
{
    /// <summary>Why <see cref="InventoryService.Move"/> refused. The item always stays where it was.</summary>
    public enum MoveFailure
    {
        None = 0,

        /// <summary>The source location holds fewer copies than asked for (or none at all).</summary>
        NotOwned = 1,

        /// <summary>The destination is full.</summary>
        CapacityFull = 2,

        /// <summary>The item can't go there: sealed product can't enter the display case.</summary>
        NotAllowedThere = 3,

        /// <summary>The hand holds one kind at a time: one sealed pack, or single cards.</summary>
        HeldMixed = 4,

        /// <summary>A count below one, or the same location as source and destination.</summary>
        InvalidMove = 5,
    }
}
