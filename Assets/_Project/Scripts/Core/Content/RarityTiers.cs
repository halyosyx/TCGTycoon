using System;
using System.Collections.Generic;

namespace Game.Core.Content
{
    /// <summary>Helpers for iterating and checking <see cref="RarityTier"/> values.</summary>
    public static class RarityTiers
    {
        /// <summary>Every tier, from most common to rarest.</summary>
        public static IReadOnlyList<RarityTier> All { get; } =
            Array.AsReadOnly((RarityTier[])Enum.GetValues(typeof(RarityTier)));

        /// <summary>Number of tiers.</summary>
        public static int Count => All.Count;

        /// <summary>True when the value is one of the defined tiers (data can hold stale or out-of-range values).</summary>
        public static bool IsDefined(RarityTier tier) => (int)tier >= 0 && (int)tier < Count;
    }
}
