using System;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>The base price in cents of cards of one rarity tier, and how volatile that tier's market price is.</summary>
    [Serializable]
    public struct TierPrice
    {
        public const string TierField = nameof(_tier);
        public const string PriceCentsField = nameof(_priceCents);
        public const string VolatilityField = nameof(_volatility);

        [SerializeField]
        private RarityTier _tier;

        [SerializeField, Tooltip("Base price in cents for the in-print set; other sets scale it.")]
        private long _priceCents;

        [SerializeField, Tooltip("How strongly this tier's market price swings (read by the market from F4).")]
        private VolatilityTier _volatility;

        public TierPrice(RarityTier tier, long priceCents, VolatilityTier volatility)
        {
            _tier = tier;
            _priceCents = priceCents;
            _volatility = volatility;
        }

        public RarityTier Tier => _tier;

        public long PriceCents => _priceCents;

        public VolatilityTier Volatility => _volatility;
    }
}
