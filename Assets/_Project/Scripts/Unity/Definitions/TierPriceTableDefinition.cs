using System;
using System.Collections.Generic;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Base price in cents and volatility for each rarity tier, generated from
    /// <c>Data/Manifests/TierPrices.csv</c> by TCG > Generate Card Data (edit the CSV, not this asset).
    /// Prices are the in-print set's; a set's price scale multiplies them. The market (F4) builds on these.
    /// </summary>
    [CreateAssetMenu(menuName = "TCG/Tier Price Table", fileName = "TierPrices")]
    public sealed class TierPriceTableDefinition : ScriptableObject
    {
        public const string PricesField = nameof(_prices);

        [SerializeField, Tooltip("One entry per rarity tier. Generated from TierPrices.csv.")]
        private List<TierPrice> _prices = new List<TierPrice>();

        public IReadOnlyList<TierPrice> Prices => _prices;

        public bool HasPrice(RarityTier tier) => TryFind(tier, out _);

        /// <exception cref="InvalidOperationException">The table has no entry for the tier.</exception>
        public long PriceCentsOf(RarityTier tier)
        {
            if (!TryFind(tier, out TierPrice price))
            {
                throw new InvalidOperationException($"Tier price table '{name}' has no price for {RarityTiers.Describe(tier)}.");
            }

            return price.PriceCents;
        }

        /// <exception cref="InvalidOperationException">The table has no entry for the tier.</exception>
        public VolatilityTier VolatilityOf(RarityTier tier)
        {
            if (!TryFind(tier, out TierPrice price))
            {
                throw new InvalidOperationException($"Tier price table '{name}' has no entry for {RarityTiers.Describe(tier)}.");
            }

            return price.Volatility;
        }

        /// <summary>Entries holding a removed or unknown tier, and defined tiers with no entry, each naming this asset.</summary>
        public List<string> FindTierProblems()
        {
            var problems = new List<string>();
            foreach (TierPrice price in _prices)
            {
                if (!RarityTiers.IsDefined(price.Tier))
                {
                    problems.Add($"Tier price table '{name}' has a price for {RarityTiers.Describe(price.Tier)}.");
                }
            }

            foreach (RarityTier tier in RarityTiers.All)
            {
                if (!HasPrice(tier))
                {
                    problems.Add($"Tier price table '{name}' has no price for {tier}.");
                }
            }

            return problems;
        }

        /// <summary>Restores the GDD v1.7 starting values (cents): 5, 15, 240, 7000. TierPrices.csv is the source; this only seeds a new asset.</summary>
        public void ResetToDefaults()
        {
            _prices = new List<TierPrice>
            {
                new TierPrice(RarityTier.Common, 5, VolatilityTier.Low),
                new TierPrice(RarityTier.Uncommon, 15, VolatilityTier.Low),
                new TierPrice(RarityTier.HoloFullArt, 240, VolatilityTier.Medium),
                new TierPrice(RarityTier.SpecialFullArtHolo, 7000, VolatilityTier.High),
            };
        }

        private void Reset() => ResetToDefaults();

        private bool TryFind(RarityTier tier, out TierPrice found)
        {
            foreach (TierPrice price in _prices)
            {
                if (price.Tier == tier)
                {
                    found = price;
                    return true;
                }
            }

            found = default;
            return false;
        }
    }
}
