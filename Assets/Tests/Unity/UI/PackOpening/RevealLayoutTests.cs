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

        // F2e: the row is always two lines (4 + 3) at 1.1, 1.375 times the old single row's 0.8.
        [Test]
        public void RowScale_DefaultLayout_TwoRowsAtOnePointOne()
        {
            var layout = new RevealLayout();

            Assert.That(layout.RowScale, Is.EqualTo(1.1f));
            Vector2 first = layout.RowPosition(0, 7, new Vector2(CardView.Width, CardView.Height));
            Vector2 fifth = layout.RowPosition(4, 7, new Vector2(CardView.Width, CardView.Height));
            Assert.That(first.y, Is.LessThan(0f), "Slots 1-4 on the top line.");
            Assert.That(fifth.y, Is.GreaterThan(0f), "Slots 5-7 on the bottom line.");
        }

        [Test]
        public void StackScale_DefaultLayout_CardFillsMostOfTheScreenHeight()
        {
            var layout = new RevealLayout();

            float scale = layout.StackScale(1080f);

            Assert.That(CardView.Height * scale, Is.EqualTo(1080f * layout.StackCardHeightShare).Within(0.01f));
            Assert.That(layout.StackCardHeightShare, Is.GreaterThan(0.6f).And.LessThan(0.9f), "Large, a little smaller than the zoomed pack.");
        }

        // The cards are centred 46% down the panel (.card-view in PackOpening.uss); the footer (hint and
        // Store button) takes the bottom of the panel.
        private const float CardCentreShare = 0.46f;
        private const float FooterHeight = 140f;
        private const float EdgeMargin = 24f;

        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void RowBlock_SevenCardsTwoRows_FitsThePanel(int screenWidth, int screenHeight)
        {
            var layout = new RevealLayout();
            Vector2 panel = RevealLayout.PanelSize(new Vector2(screenWidth, screenHeight), new Vector2(1920f, 1080f), 0.5f);

            Vector2 block = layout.RowBlockSize(7, new Vector2(CardView.Width, CardView.Height));

            float centreY = panel.y * CardCentreShare;
            Assert.That(block.x, Is.LessThanOrEqualTo(panel.x - EdgeMargin * 2f), "Width");
            Assert.That(centreY - block.y * 0.5f, Is.GreaterThanOrEqualTo(EdgeMargin), "Top edge");
            Assert.That(centreY + block.y * 0.5f, Is.LessThanOrEqualTo(panel.y - FooterHeight), "Bottom edge clears the footer");
        }

        [Test]
        public void PanelSize_SameAspectAsReference_IsTheReferenceSize()
        {
            Vector2 panel = RevealLayout.PanelSize(new Vector2(1280f, 720f), new Vector2(1920f, 1080f), 0.5f);

            Assert.That(panel.x, Is.EqualTo(1920f).Within(0.5f));
            Assert.That(panel.y, Is.EqualTo(1080f).Within(0.5f));
        }
    }
}
