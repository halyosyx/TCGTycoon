using System;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>The base price in cents of cards of one rarity tier.</summary>
    [Serializable]
    public struct TierPrice
    {
        [SerializeField]
        private RarityTier _tier;

        [SerializeField, Tooltip("Base price in cents.")]
        private long _priceCents;

        public TierPrice(RarityTier tier, long priceCents)
        {
            _tier = tier;
            _priceCents = priceCents;
        }

        public RarityTier Tier => _tier;

        public long PriceCents => _priceCents;
    }
}
