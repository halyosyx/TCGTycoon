using System;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>Authoring form of one slot entry: a rarity tier and its integer weight.</summary>
    [Serializable]
    public struct TierWeightData
    {
        public const string TierField = nameof(_tier);
        public const string WeightField = nameof(_weight);

        [SerializeField]
        private RarityTier _tier;

        [SerializeField, Min(0), Tooltip("Relative weight. Weights in a slot don't need to sum to 100.")]
        private int _weight;

        public TierWeight ToTierWeight() => new TierWeight(_tier, _weight);
    }
}
