using System;

namespace Game.Unity.UI
{
    /// <summary>
    /// One pocket's worth of binder data: a distinct owned item and how many copies are owned.
    /// Immutable, so a view can hold on to it safely.
    /// </summary>
    public sealed class BinderEntry
    {
        public BinderEntry(string itemId, string displayName, int? tier, int copies)
        {
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("An entry needs an item id.", nameof(itemId));

            ItemId = itemId;
            DisplayName = string.IsNullOrEmpty(displayName) ? itemId : displayName;
            Tier = tier;
            Copies = copies;
        }

        /// <summary>Card id for singles; product id for sealed items.</summary>
        public string ItemId { get; }

        public string DisplayName { get; }

        /// <summary>UI tier number 1 (Common) to 7 (Special Illustration); null for sealed products.</summary>
        public int? Tier { get; }

        public int Copies { get; }
    }
}
