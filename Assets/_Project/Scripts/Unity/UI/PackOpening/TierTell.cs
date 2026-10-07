using System;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// How one rarity tier reacts when its card is revealed: a single quick flash on the card (a light
    /// pass over the face and a halo in the tier colour behind it), and for the top tier a burst of
    /// sparkles. Every effect is local to the card, plays once and never repeats or pulses; a full-screen
    /// or strobing flash is a photosensitivity risk, so none exists. The halo and sparkle colour is the
    /// tier's colour in the Rarity Palette.
    /// </summary>
    [Serializable]
    public sealed class TierTell
    {
        [SerializeField]
        private RarityTier _tier;

        [SerializeField, Range(0f, 1f), Tooltip("Peak opacity of the flash. 0 = no reaction. Keep it subtle.")]
        private float _flashIntensity;

        [SerializeField, Range(0.05f, 0.5f), Tooltip("Seconds the flash takes to fade. One shot: it never repeats.")]
        private float _flashSeconds = 0.2f;

        [SerializeField, Min(0f), Tooltip("How far the tier-coloured halo reaches beyond the card edge, in card pixels.")]
        private float _haloSpread;

        [SerializeField, Min(0), Tooltip("Sparkles in the burst. 0 = none; by design only Special Full Art Holo sparkles.")]
        private int _sparkleCount;

        public TierTell()
        {
        }

        public TierTell(RarityTier tier, float flashIntensity, float flashSeconds, float haloSpread, int sparkleCount)
        {
            _tier = tier;
            _flashIntensity = flashIntensity;
            _flashSeconds = flashSeconds;
            _haloSpread = haloSpread;
            _sparkleCount = sparkleCount;
        }

        public RarityTier Tier => _tier;

        public float FlashIntensity => _flashIntensity;

        public float FlashSeconds => _flashSeconds;

        public float HaloSpread => _haloSpread;

        public int SparkleCount => _sparkleCount;

        /// <summary>What a card with this tell does when revealed. No tell, or no flash, means nothing.</summary>
        public static TierTellKind KindOf(TierTell tell)
        {
            if (tell == null || tell._flashIntensity <= 0f)
            {
                return TierTellKind.None;
            }

            return tell._sparkleCount > 0 ? TierTellKind.FlashAndSparkle : TierTellKind.Flash;
        }

        /// <summary>The tell for <paramref name="tier"/>, or null.</summary>
        public static TierTell Find(TierTell[] tells, RarityTier tier)
        {
            if (tells == null)
            {
                return null;
            }

            foreach (TierTell tell in tells)
            {
                if (tell != null && tell._tier == tier)
                {
                    return tell;
                }
            }

            return null;
        }

        /// <summary>
        /// Starting values, erring subtle: nothing for bulk, a light flash for Holographic Full Art, the
        /// same flash a little stronger plus sparkles for Special Full Art Holo.
        /// </summary>
        public static TierTell[] CreateDefaults()
        {
            return new[]
            {
                new TierTell(RarityTier.Common, 0f, 0.2f, 0f, 0),
                new TierTell(RarityTier.Uncommon, 0f, 0.2f, 0f, 0),
                new TierTell(RarityTier.HoloFullArt, 0.25f, 0.18f, 12f, 0),
                new TierTell(RarityTier.SpecialFullArtHolo, 0.35f, 0.22f, 16f, 14),
            };
        }
    }
}
