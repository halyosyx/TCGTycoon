using Game.Core.Content;
using Game.Unity.UI.PackOpening;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.PackOpening
{
    /// <summary>
    /// The reveal-time rarity tell: nothing for bulk, a subtle one-shot flash for Holographic Full Art,
    /// the same flash a little stronger plus sparkles for Special Full Art Holo, the only tier that sparkles.
    /// </summary>
    public sealed class TierTellTests
    {
        [TestCase(RarityTier.Common)]
        [TestCase(RarityTier.Uncommon)]
        public void Defaults_Bulk_GetNoEffect(RarityTier tier)
        {
            TierTell tell = TierTell.Find(TierTell.CreateDefaults(), tier);

            Assert.That(TierTell.KindOf(tell), Is.EqualTo(TierTellKind.None));
        }

        [Test]
        public void Defaults_HoloFullArt_FlashOnlyNoSparkles()
        {
            TierTell tell = TierTell.Find(TierTell.CreateDefaults(), RarityTier.HoloFullArt);

            Assert.That(TierTell.KindOf(tell), Is.EqualTo(TierTellKind.Flash));
            Assert.That(tell.SparkleCount, Is.EqualTo(0));
            Assert.That(tell.FlashIntensity, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.5f), "Subtle.");
        }

        [Test]
        public void Defaults_SpecialFullArtHolo_StrongerFlashAndSparkles()
        {
            TierTell[] tells = TierTell.CreateDefaults();
            TierTell special = TierTell.Find(tells, RarityTier.SpecialFullArtHolo);
            TierTell holo = TierTell.Find(tells, RarityTier.HoloFullArt);

            Assert.That(TierTell.KindOf(special), Is.EqualTo(TierTellKind.FlashAndSparkle));
            Assert.That(special.SparkleCount, Is.GreaterThan(0));
            Assert.That(special.FlashIntensity, Is.GreaterThan(holo.FlashIntensity));
        }

        [Test]
        public void Defaults_OnlySpecialFullArtHoloSparkles()
        {
            foreach (TierTell tell in TierTell.CreateDefaults())
            {
                bool isSparkling = TierTell.KindOf(tell) == TierTellKind.FlashAndSparkle;
                Assert.That(isSparkling, Is.EqualTo(tell.Tier == RarityTier.SpecialFullArtHolo), tell.Tier.ToString());
            }
        }

        [Test]
        public void Defaults_EveryFlashIsOneShotUnderASecond()
        {
            foreach (TierTell tell in TierTell.CreateDefaults())
            {
                if (TierTell.KindOf(tell) != TierTellKind.None)
                {
                    Assert.That(tell.FlashSeconds, Is.GreaterThan(0f).And.LessThan(1f), tell.Tier.ToString());
                }
            }
        }

        [Test]
        public void KindOf_NoTellOrZeroFlash_IsNone()
        {
            Assert.That(TierTell.KindOf(null), Is.EqualTo(TierTellKind.None));
            Assert.That(TierTell.KindOf(new TierTell(RarityTier.HoloFullArt, 0f, 0.2f, 20f, 0)), Is.EqualTo(TierTellKind.None));
        }

        [Test]
        public void Find_MissingTier_ReturnsNull()
        {
            Assert.That(TierTell.Find(new TierTell[0], RarityTier.HoloFullArt), Is.Null);
            Assert.That(TierTell.Find(null, RarityTier.HoloFullArt), Is.Null);
        }
    }
}
