using System;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// How strongly one rarity tier glows during and after its reveal. The glow's colour is the tier's
    /// colour in the Rarity Palette, so tier colours stay defined in one place.
    /// </summary>
    [Serializable]
    public sealed class TierTell
    {
        [SerializeField]
        private RarityTier _tier;

        [SerializeField, Range(0f, 1f), Tooltip("Peak glow opacity. 0 = no glow.")]
        private float _intensity;

        [SerializeField, Min(0f), Tooltip("How far the glow spreads beyond the card edge, in pixels.")]
        private float _spread;

        [SerializeField, Min(0f), Tooltip("Glow pulses per second.")]
        private float _pulsesPerSecond;

        public TierTell()
        {
        }

        public TierTell(RarityTier tier, float intensity, float spread, float pulsesPerSecond)
        {
            _tier = tier;
            _intensity = intensity;
            _spread = spread;
            _pulsesPerSecond = pulsesPerSecond;
        }

        public RarityTier Tier => _tier;

        public float Intensity => _intensity;

        public float Spread => _spread;

        public float PulsesPerSecond => _pulsesPerSecond;

        /// <summary>Starting values: nothing for bulk, a hint for Rare and Holo, strong from FullArt up.</summary>
        public static TierTell[] CreateDefaults()
        {
            return new[]
            {
                new TierTell(RarityTier.Common, 0f, 0f, 0f),
                new TierTell(RarityTier.Uncommon, 0f, 0f, 0f),
                new TierTell(RarityTier.Rare, 0.3f, 14f, 1f),
                new TierTell(RarityTier.Holographic, 0.5f, 20f, 1.5f),
                new TierTell(RarityTier.FullArt, 0.8f, 32f, 2f),
                new TierTell(RarityTier.AlternateIllustration, 0.9f, 38f, 2.5f),
                new TierTell(RarityTier.SpecialIllustration, 1f, 48f, 3f),
            };
        }
    }
}
