using System;
using System.Collections.Generic;

namespace Game.Core.Content
{
    /// <summary>One card position in a pack: the tiers it can roll and their weights. Any slot may hold any tiers.</summary>
    public sealed class PackSlot
    {
        public PackSlot(IEnumerable<TierWeight> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            Entries = new List<TierWeight>(entries).AsReadOnly();
        }

        public IReadOnlyList<TierWeight> Entries { get; }
    }
}
