using System;
using Game.Core.Content;

namespace Game.Core.Packs
{
    /// <summary>
    /// Accumulates opened packs into observed statistics: tier counts per slot, packs holding at least
    /// one card of each tier or better, and pull value. The editor tool, the debug console and the
    /// tests all use it, so observed numbers are computed one way everywhere.
    /// </summary>
    public sealed class PackTally
    {
        private readonly long[,] _tierCountsBySlot;
        private readonly long[] _packsWithAtLeastTier;
        private double _sumOfSquaredPackValues;

        public PackTally(int slotCount)
        {
            if (slotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "A pack needs at least one slot.");
            }

            SlotCount = slotCount;
            _tierCountsBySlot = new long[slotCount, RarityTiers.Count];
            _packsWithAtLeastTier = new long[RarityTiers.Count];
        }

        public int SlotCount { get; }

        public long PackCount { get; private set; }

        public long TotalValueCents { get; private set; }

        /// <summary>Observed Rip EV: mean pull value per pack, in cents.</summary>
        public double AverageValueCents => PackCount == 0 ? 0d : (double)TotalValueCents / PackCount;

        /// <summary>Standard error of <see cref="AverageValueCents"/>: how far it's likely to sit from the true expected value.</summary>
        public double StandardErrorCents
        {
            get
            {
                if (PackCount < 2)
                {
                    return 0d;
                }

                double mean = AverageValueCents;
                double sampleVariance = (_sumOfSquaredPackValues - PackCount * mean * mean) / (PackCount - 1);
                return Math.Sqrt(Math.Max(sampleVariance, 0d) / PackCount);
            }
        }

        public void Add(OpenedPack pack)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            if (pack.Cards.Count != SlotCount)
            {
                throw new ArgumentException($"Pack has {pack.Cards.Count} cards but the tally expects {SlotCount}.", nameof(pack));
            }

            RarityTier bestTier = RarityTier.Common;
            long packValueCents = 0;
            for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                Card card = pack.Cards[slotIndex];
                _tierCountsBySlot[slotIndex, RarityTiers.IndexOf(card.Tier)]++;
                packValueCents += card.ValueCents;
                if (card.Tier > bestTier)
                {
                    bestTier = card.Tier;
                }
            }

            // A pack whose best card is tier B holds "at least one of tier X or better" for every X ≤ B.
            for (int tierIndex = 0; tierIndex <= RarityTiers.IndexOf(bestTier); tierIndex++)
            {
                _packsWithAtLeastTier[tierIndex]++;
            }

            PackCount++;
            TotalValueCents += packValueCents;
            _sumOfSquaredPackValues += (double)packValueCents * packValueCents;
        }

        public long TierCount(int slotIndex, RarityTier tier) => _tierCountsBySlot[slotIndex, RarityTiers.IndexOf(tier)];

        /// <summary>Share of packs whose slot rolled <paramref name="tier"/>.</summary>
        public double ObservedTierRate(int slotIndex, RarityTier tier)
        {
            return PackCount == 0 ? 0d : (double)TierCount(slotIndex, tier) / PackCount;
        }

        /// <summary>Share of packs holding at least one card of <paramref name="minimumTier"/> or rarer.</summary>
        public double ObservedChanceOfAtLeastOne(RarityTier minimumTier)
        {
            return PackCount == 0 ? 0d : (double)_packsWithAtLeastTier[RarityTiers.IndexOf(minimumTier)] / PackCount;
        }
    }
}
