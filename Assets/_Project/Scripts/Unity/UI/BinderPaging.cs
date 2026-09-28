using System;
using System.Collections.Generic;

namespace Game.Unity.UI
{
    /// <summary>
    /// Splits a tab's entries into binder spreads: two facing pages of 3 × 3 pockets. A binder is
    /// always shown as whole spreads, so the page count is even and at least two (an empty tab is one
    /// spread of empty pockets). Pure: no state, no Unity types.
    /// </summary>
    public static class BinderPaging
    {
        public const int SlotsPerPage = 9;
        public const int PagesPerSpread = 2;
        public const int SlotsPerSpread = SlotsPerPage * PagesPerSpread;

        /// <summary>Spreads needed for <paramref name="entryCount"/> entries; at least one.</summary>
        public static int SpreadCount(int entryCount)
        {
            if (entryCount <= 0)
            {
                return 1;
            }

            return (entryCount + SlotsPerSpread - 1) / SlotsPerSpread;
        }

        /// <summary>Pages needed for <paramref name="entryCount"/> entries: always whole spreads.</summary>
        public static int PageCount(int entryCount) => SpreadCount(entryCount) * PagesPerSpread;

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="spreadIndex"/> is not a spread of these entries.</exception>
        public static BinderSpread GetSpread(IReadOnlyList<BinderEntry> entries, int spreadIndex)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            int spreadCount = SpreadCount(entries.Count);
            if (spreadIndex < 0 || spreadIndex >= spreadCount)
            {
                throw new ArgumentOutOfRangeException(nameof(spreadIndex), spreadIndex, $"There are spreads 0 to {spreadCount - 1}.");
            }

            int firstEntry = spreadIndex * SlotsPerSpread;
            return new BinderSpread(
                Page(entries, firstEntry),
                Page(entries, firstEntry + SlotsPerPage),
                spreadIndex,
                spreadCount);
        }

        private static BinderEntry[] Page(IReadOnlyList<BinderEntry> entries, int firstEntry)
        {
            var slots = new BinderEntry[SlotsPerPage];
            for (int slot = 0; slot < SlotsPerPage; slot++)
            {
                int entryIndex = firstEntry + slot;
                slots[slot] = entryIndex < entries.Count ? entries[entryIndex] : null;
            }

            return slots;
        }
    }
}
