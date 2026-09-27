using System;
using System.Collections.Generic;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Base price in cents for each rarity tier: the single source generated cards take their value
    /// from. Retune a tier here and regenerate. The market (F4) can build on these later.
    /// </summary>
    [CreateAssetMenu(menuName = "TCG/Tier Price Table", fileName = "TierPrices")]
    public sealed class TierPriceTableDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("One entry per rarity tier.")]
        private List<TierPrice> _prices = new List<TierPrice>();

        public bool HasPrice(RarityTier tier) => TryFind(tier, out _);

        /// <exception cref="InvalidOperationException">The table has no entry for the tier.</exception>
        public long PriceCentsOf(RarityTier tier)
        {
            if (!TryFind(tier, out long priceCents))
            {
                throw new InvalidOperationException($"Tier price table '{name}' has no price for {tier}.");
            }

            return priceCents;
        }

        /// <summary>Restores the F1a starting values (cents): 5, 15, 60, 250, 800, 2500, 9000.</summary>
        public void ResetToDefaults()
        {
            _prices = new List<TierPrice>
            {
                new TierPrice(RarityTier.Common, 5),
                new TierPrice(RarityTier.Uncommon, 15),
                new TierPrice(RarityTier.Rare, 60),
                new TierPrice(RarityTier.Holographic, 250),
                new TierPrice(RarityTier.FullArt, 800),
                new TierPrice(RarityTier.AlternateIllustration, 2500),
                new TierPrice(RarityTier.SpecialIllustration, 9000),
            };
        }

        private void Reset() => ResetToDefaults();

        private bool TryFind(RarityTier tier, out long priceCents)
        {
            foreach (TierPrice price in _prices)
            {
                if (price.Tier == tier)
                {
                    priceCents = price.PriceCents;
                    return true;
                }
            }

            priceCents = 0;
            return false;
        }
    }
}
