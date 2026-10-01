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
            Card holo = TestContent.CreateCard("RC_HFA_01", RarityTier.HoloFullArt);
            var pool = new CardPool(new[] { TestContent.CreateCard("RC_C_001", RarityTier.Common), holo });

            bool isFound = pool.TryGetCard("RC_HFA_01", out Card card);

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

        [Test]
        public void TryGetCard_NullId_ReturnsFalse()
        {
            CardPool pool = TestContent.SetAPool();

            Assert.That(pool.TryGetCard(null, out _), Is.False);
        }
    }
}
