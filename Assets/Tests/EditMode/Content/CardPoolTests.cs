using Game.Core.Content;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Content
{
    public sealed class CardPoolTests
    {
        [Test]
        public void TryGetCard_KnownId_ReturnsThatCard()
        {
            Card holo = TestContent.CreateCard("SetA_Holo_01", RarityTier.Holographic);
            var pool = new CardPool(new[] { TestContent.CreateCard("SetA_Common_01", RarityTier.Common), holo });

            bool isFound = pool.TryGetCard("SetA_Holo_01", out Card card);

            Assert.That(isFound, Is.True);
            Assert.That(card, Is.SameAs(holo));
        }

        [Test]
        public void TryGetCard_UnknownId_ReturnsFalseAndNull()
        {
            CardPool pool = TestContent.SetAPool();

            bool isFound = pool.TryGetCard("SetB_Missing_01", out Card card);

            Assert.That(isFound, Is.False);
            Assert.That(card, Is.Null);
        }

        [TestCase(RarityTier.Common, true)]
        [TestCase(RarityTier.Uncommon, true)]
        [TestCase(RarityTier.Rare, false)]
        [TestCase(RarityTier.Holographic, false)]
        [TestCase(RarityTier.FullArt, false)]
        [TestCase(RarityTier.AlternateIllustration, false)]
        [TestCase(RarityTier.SpecialIllustration, false)]
        public void RarityTiers_IsBulk_OnlyCommonAndUncommon(RarityTier tier, bool expected)
        {
            Assert.That(RarityTiers.IsBulk(tier), Is.EqualTo(expected));
        }

        [Test]
        public void TryGetCard_NullId_ReturnsFalse()
        {
            CardPool pool = TestContent.SetAPool();

            Assert.That(pool.TryGetCard(null, out _), Is.False);
        }
    }
}
