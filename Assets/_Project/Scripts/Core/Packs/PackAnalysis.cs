using System;
using Game.Core.Content;

namespace Game.Core.Packs
{
    /// <summary>
    /// Exact odds and expected value computed from a pack's weights, with no rolling. Assumes a
    /// configuration without validation errors; non-positive weights count as "can't roll".
    /// </summary>
    public static class PackAnalysis
    {
        /// <summary>Chance, from 0 to 1, that the slot rolls exactly <paramref name="tier"/>.</summary>
        public static double TierProbability(PackSlot slot, RarityTier tier)
        {
            return ShareOfWeight(slot, entryTier => entryTier == tier);
        }

        /// <summary>Chance, from 0 to 1, that the slot rolls <paramref name="minimumTier"/> or a rarer tier.</summary>
        public static double AtLeastTierProbability(PackSlot slot, RarityTier minimumTier)
        {
            return ShareOfWeight(slot, entryTier => entryTier >= minimumTier);
        }

        /// <summary>
        /// Chance a pack holds at least one card of <paramref name="minimumTier"/> or rarer. Slots roll
        /// independently, so it's 1 − Π(1 − p) over the slots.
        /// </summary>
        public static double ChanceOfAtLeastOne(PackConfig config, RarityTier minimumTier)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            double chanceOfNone = 1d;
            foreach (PackSlot slot in config.Slots)
            {
                chanceOfNone *= 1d - AtLeastTierProbability(slot, minimumTier);
            }

            return 1d - chanceOfNone;
        }

        /// <summary>
        /// Expected total value of one pack's cards in cents (the Rip EV). A statistic, not money, so a
        /// <see cref="double"/>.
        /// </summary>
        public static double ExpectedValueCents(PackConfig config, CardPool pool)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (pool == null) throw new ArgumentNullException(nameof(pool));

            double totalCents = 0d;
            foreach (PackSlot slot in config.Slots)
            {
                foreach (RarityTier tier in RarityTiers.All)
                {
                    totalCents += TierProbability(slot, tier) * pool.AverageValueCents(tier);
                }
            }

            return totalCents;
        }

        private static double ShareOfWeight(PackSlot slot, Func<RarityTier, bool> matches)
        {
            if (slot == null) throw new ArgumentNullException(nameof(slot));

            long totalWeight = 0;
            long matchingWeight = 0;
            foreach (TierWeight entry in slot.Entries)
            {
                if (entry.Weight <= 0)
                {
                    continue;
                }

                totalWeight += entry.Weight;
                if (matches(entry.Tier))
                {
                    matchingWeight += entry.Weight;
                }
            }

            return totalWeight == 0 ? 0d : (double)matchingWeight / totalWeight;
        }
    }
}
