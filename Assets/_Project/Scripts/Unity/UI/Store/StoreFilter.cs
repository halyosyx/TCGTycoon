using System;
using System.Collections.Generic;

namespace Game.Unity.UI.Store
{
    /// <summary>
    /// Which product cards the store grid shows, and in what order (STORE_UI_REQUIREMENTS STR-23).
    /// Pure, so the tab, search, Type and Sort rules are tested without UI. Hidden listings never show.
    /// </summary>
    public static class StoreFilter
    {
        private static readonly Comparison<StoreFilterItem> s_byPrice = CompareByPrice;
        private static readonly Comparison<StoreFilterItem> s_byName = CompareByName;

        /// <summary>Clears <paramref name="results"/> and fills it with the visible items in display order.</summary>
        public static void Apply(IReadOnlyList<StoreFilterItem> items, StoreFilterCriteria criteria, List<StoreFilterItem> results)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();
            string search = criteria.Search == null ? string.Empty : criteria.Search.Trim();
            foreach (StoreFilterItem item in items)
            {
                if (item == null
                    || item.IsHidden
                    || (!string.IsNullOrEmpty(criteria.SetId) && !string.Equals(item.SetId, criteria.SetId, StringComparison.Ordinal))
                    || (criteria.Type.HasValue && item.Type != criteria.Type.Value)
                    || (search.Length > 0 && !Contains(item.Name, search) && !Contains(item.SetName, search)))
                {
                    continue;
                }

                results.Add(item);
            }

            results.Sort(criteria.Sort == StoreSort.Name ? s_byName : s_byPrice);
        }

        private static bool Contains(string text, string search) => text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        private static int CompareByPrice(StoreFilterItem left, StoreFilterItem right)
        {
            int byPrice = left.UnitPriceCents.CompareTo(right.UnitPriceCents);
            return byPrice != 0 ? byPrice : left.Order.CompareTo(right.Order);
        }

        private static int CompareByName(StoreFilterItem left, StoreFilterItem right)
        {
            int byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return byName != 0 ? byName : left.Order.CompareTo(right.Order);
        }
    }
}
