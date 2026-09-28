using System;
using System.Collections.Generic;

namespace Game.Core.Content
{
    /// <summary>
    /// A pack product: its price and an ordered list of slots. Slot count and slot contents come
    /// entirely from data; nothing in code assumes a number of slots or which tiers they hold.
    /// </summary>
    public sealed class PackConfig
    {
        public PackConfig(string id, string displayName, long priceCents, IEnumerable<PackSlot> slots)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            Id = id ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
            PriceCents = priceCents;
            Slots = new List<PackSlot>(slots).AsReadOnly();
        }

        public string Id { get; }

        public string DisplayName { get; }

        /// <summary>Market price in cents. Fixed until market prices exist (F4).</summary>
        public long PriceCents { get; }

        /// <summary>Slots in reveal order.</summary>
        public IReadOnlyList<PackSlot> Slots { get; }
    }
}
