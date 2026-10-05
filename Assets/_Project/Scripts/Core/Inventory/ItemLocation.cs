namespace Game.Core.Inventory
{
    /// <summary>
    /// Where an owned item is. Every copy of every owned item is in exactly one location, and only
    /// <see cref="InventoryService.Move"/> changes it. Values are stable: they end up in save files.
    /// </summary>
    public enum ItemLocation
    {
        /// <summary>Stored at home: the binder for singles, the shelf (table stacks) for sealed.</summary>
        Binder = 0,

        /// <summary>In the player's hand: one sealed pack, or up to ten single cards, never both.</summary>
        Held = 1,

        /// <summary>The booth's display case: single cards only, and everything in it is for sale.</summary>
        DisplayCase = 2,

        /// <summary>Set down loose in the room by the player (sealed packs today).</summary>
        Placed = 3,

        // Reserved, not declared until their features exist: BulkBox = 4, Booth = 5.
    }
}
