using Game.Unity.UI.PackOpening;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests.UI.PackOpening
{
    public sealed class RevealLayoutTests
    {
        private const float Exact = 1e-4f;
        private const float Gap = 24f;
        private const float LineGap = 24f;

        private static readonly Vector2 s_card = new Vector2(200f, 280f);

        [Test]
        public void RowPosition_SingleRowOfSeven_EvenlySpacedAndCentred()
        {
            float step = s_card.x + Gap;

            for (int slot = 0; slot < 7; slot++)
            {
                Vector2 position = RevealLayout.RowPosition(slot, 7, RowArrangement.SingleRow, s_card, Gap, LineGap);
                Assert.That(position.x, Is.EqualTo((slot - 3) * step).Within(Exact), $"Slot {slot + 1}");
                Assert.That(position.y, Is.EqualTo(0f));
            }
        }

        [Test]
        public void RowPosition_TwoRowsOfSeven_FourOnTopThreeBelowEachCentred()
        {
            float step = s_card.x + Gap;
            float halfLine = (s_card.y + LineGap) * 0.5f;

            float[] expectedX = { -1.5f * step, -0.5f * step, 0.5f * step, 1.5f * step, -step, 0f, step };
            for (int slot = 0; slot < 7; slot++)
            {
                Vector2 position = RevealLayout.RowPosition(slot, 7, RowArrangement.TwoRows, s_card, Gap, LineGap);
                Assert.That(position.x, Is.EqualTo(expectedX[slot]).Within(Exact), $"Slot {slot + 1} x");
                Assert.That(position.y, Is.EqualTo(slot < 4 ? -halfLine : halfLine).Within(Exact), $"Slot {slot + 1} y");
            }
        }

        [TestCase(3, 2)]
        [TestCase(12, 6)]
        public void RowPosition_TwoRowsAnyCount_TopLineHoldsTheLargerHalf(int cardCount, int expectedTop)
        {
            int top = 0;
            for (int slot = 0; slot < cardCount; slot++)
            {
                if (RevealLayout.RowPosition(slot, cardCount, RowArrangement.TwoRows, s_card, Gap, LineGap).y < 0f) top++;
            }

            Assert.That(top, Is.EqualTo(expectedTop));
        }

        [TestCase(RowArrangement.SingleRow)]
        [TestCase(RowArrangement.TwoRows)]
        public void RowPosition_SingleCard_SitsAtTheRestPosition(RowArrangement arrangement)
        {
            Assert.That(RevealLayout.RowPosition(0, 1, arrangement, s_card, Gap, LineGap), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void RowScale_DefaultLayout_SingleRowAtPointEight()
        {
            var layout = new RevealLayout();

            Assert.That(layout.RowArrangement, Is.EqualTo(RowArrangement.SingleRow));
            Assert.That(layout.RowScale, Is.EqualTo(0.8f));
        }
    }
}
