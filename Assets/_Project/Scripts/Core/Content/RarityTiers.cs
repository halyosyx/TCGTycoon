using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Core.Content
{
    /// <summary>Helpers for iterating, indexing and checking <see cref="RarityTier"/> values.</summary>
    public static class RarityTiers
    {
        private static readonly RarityTier[] s_all =
        {
            RarityTier.Common, RarityTier.Uncommon, RarityTier.HoloFullArt, RarityTier.SpecialFullArtHolo,
        };

        // The seven-tier ladder's values that no longer exist (GDD v1.7). Kept only to name them in errors.
        private static readonly Dictionary<int, string> s_removedNames = new Dictionary<int, string>
        {
            { 2, "Rare" },
            { 3, "Holographic" },
            { 4, "FullArt" },
            { 5, "AlternateIllustration" },
            { 6, "SpecialIllustration" },
        };

        /// <summary>Every tier, from most common to rarest.</summary>
        public static IReadOnlyList<RarityTier> All { get; } = Array.AsReadOnly(s_all);

        /// <summary>Number of tiers.</summary>
        public static int Count => s_all.Length;

        /// <summary>True when the value is one of the defined tiers (data can hold stale or out-of-range values).</summary>
        public static bool IsDefined(RarityTier tier) => Array.IndexOf(s_all, tier) >= 0;

        /// <summary>True when the value belonged to the retired seven-tier ladder.</summary>
        public static bool IsRemoved(RarityTier tier) => s_removedNames.ContainsKey((int)tier);

        /// <summary>
        /// The tier's position from most common (0) to rarest (<see cref="Count"/> - 1), for indexing
        /// tallies and tables. The enum's values are not contiguous, so never cast instead.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The tier is not defined.</exception>
        public static int IndexOf(RarityTier tier)
        {
            int index = Array.IndexOf(s_all, tier);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), (int)tier, Describe(tier));
            }

            return index;
        }

        /// <summary>
        /// The next rarer tier, or the rarest when <paramref name="tier"/> is already the rarest. An
        /// undefined value gives <see cref="RarityTier.Common"/>, so callers never get a retired value
        /// (adding 1 to a tier's value would: Uncommon + 1 is the retired Rare).
        /// </summary>
        public static RarityTier NextRarer(RarityTier tier)
        {
            int index = Array.IndexOf(s_all, tier);
            return index < 0 ? s_all[0] : s_all[Math.Min(index + 1, s_all.Length - 1)];
        }

        /// <summary>
        /// The tier's name, or what is wrong with it: "HoloFullArt", "removed tier FullArt (4)" or
        /// "unknown tier value 12". Used in validation messages so stale data names itself.
        /// </summary>
        public static string Describe(RarityTier tier)
        {
            if (IsDefined(tier))
            {
                return tier.ToString();
            }

            int value = (int)tier;
            string number = value.ToString(CultureInfo.InvariantCulture);
            return s_removedNames.TryGetValue(value, out string removedName)
                ? $"removed tier {removedName} ({number})"
                : $"unknown tier value {number}";
        }

        /// <summary>Parses a tier name as written in manifests ("HoloFullArt"); removed and unknown names fail.</summary>
        public static bool TryParse(string name, out RarityTier tier)
        {
            foreach (RarityTier candidate in s_all)
            {
                if (string.Equals(candidate.ToString(), name, StringComparison.Ordinal))
                {
                    tier = candidate;
                    return true;
                }
            }

            tier = default;
            return false;
        }

        /// <summary>True for a name of the retired seven-tier ladder ("FullArt"), so manifests can say why it failed.</summary>
        public static bool IsRemovedName(string name)
        {
            foreach (string removedName in s_removedNames.Values)
            {
                if (string.Equals(removedName, name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// GDD: Commons and Uncommons are bulk. They go to the bulk box (sold at a fixed price per card)
        /// rather than the display binder and display case.
        /// </summary>
        public static bool IsBulk(RarityTier tier) => tier == RarityTier.Common || tier == RarityTier.Uncommon;
    }
}
