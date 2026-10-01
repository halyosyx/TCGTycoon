using System;
using Game.Core.Content;
using NUnit.Framework;

namespace Game.Core.Tests.Content
{
    public sealed class RarityTiersTests
    {
        [Test]
        public void All_FourTiersFromMostCommonToRarest()
        {
            Assert.That(
                RarityTiers.All,
                Is.EqualTo(new[] { RarityTier.Common, RarityTier.Uncommon, RarityTier.HoloFullArt, RarityTier.SpecialFullArtHolo }));
            Assert.That(RarityTiers.Count, Is.EqualTo(4));
        }

        [Test]
        public void All_ValuesAscend_SoTierOrBetterComparisonsHold()
        {
            for (int i = 1; i < RarityTiers.Count; i++)
            {
                Assert.That(RarityTiers.All[i], Is.GreaterThan(RarityTiers.All[i - 1]));
            }
        }

        [TestCase(RarityTier.Common, 0)]
        [TestCase(RarityTier.Uncommon, 1)]
        [TestCase(RarityTier.HoloFullArt, 2)]
        [TestCase(RarityTier.SpecialFullArtHolo, 3)]
        public void IndexOf_DefinedTier_IsItsPositionInTheLadder(RarityTier tier, int expected)
        {
            Assert.That(RarityTiers.IndexOf(tier), Is.EqualTo(expected));
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(12)]
        public void IndexOf_UndefinedValue_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RarityTiers.IndexOf((RarityTier)value));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void IsDefined_RetiredSevenTierValue_IsFalseAndIsRemoved(int value)
        {
            Assert.That(RarityTiers.IsDefined((RarityTier)value), Is.False);
            Assert.That(RarityTiers.IsRemoved((RarityTier)value), Is.True);
        }

        [TestCase(RarityTier.HoloFullArt, "HoloFullArt")]
        [TestCase((RarityTier)4, "removed tier FullArt (4)")]
        [TestCase((RarityTier)2, "removed tier Rare (2)")]
        [TestCase((RarityTier)12, "unknown tier value 12")]
        public void Describe_NamesTheTierOrWhatIsWrongWithIt(RarityTier tier, string expected)
        {
            Assert.That(RarityTiers.Describe(tier), Is.EqualTo(expected));
        }

        [TestCase("SpecialFullArtHolo", true, RarityTier.SpecialFullArtHolo)]
        [TestCase("Common", true, RarityTier.Common)]
        [TestCase("FullArt", false, RarityTier.Common)]
        [TestCase("common", false, RarityTier.Common)]
        [TestCase("", false, RarityTier.Common)]
        public void TryParse_OnlyCurrentTierNamesExactly(string name, bool expectedParsed, RarityTier expectedTier)
        {
            bool isParsed = RarityTiers.TryParse(name, out RarityTier tier);

            Assert.That(isParsed, Is.EqualTo(expectedParsed));
            if (expectedParsed)
            {
                Assert.That(tier, Is.EqualTo(expectedTier));
            }
        }

        [TestCase("Rare", true)]
        [TestCase("AlternateIllustration", true)]
        [TestCase("HoloFullArt", false)]
        public void IsRemovedName_RetiredNamesOnly(string name, bool expected)
        {
            Assert.That(RarityTiers.IsRemovedName(name), Is.EqualTo(expected));
        }

        [TestCase(RarityTier.Common, true)]
        [TestCase(RarityTier.Uncommon, true)]
        [TestCase(RarityTier.HoloFullArt, false)]
        [TestCase(RarityTier.SpecialFullArtHolo, false)]
        public void IsBulk_OnlyCommonAndUncommon(RarityTier tier, bool expected)
        {
            Assert.That(RarityTiers.IsBulk(tier), Is.EqualTo(expected));
        }
    }
}
