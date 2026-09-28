using System;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Unity.UI.PackOpening;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.PackOpening
{
    public sealed class PackRevealStateMachineTests
    {
        [Test]
        public void Begin_FromIdle_EntersRevealingWithNothingRevealed()
        {
            var reveal = new PackRevealStateMachine();

            reveal.Begin(CreatePack(5));

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Revealing));
            Assert.That(reveal.RevealedCount, Is.EqualTo(0));
            Assert.That(reveal.CardCount, Is.EqualTo(5));
        }

        [Test]
        public void Begin_WhilePackOnScreen_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));

            Assert.Throws<InvalidOperationException>(() => reveal.Begin(CreatePack(5)));
        }

        [Test]
        public void Begin_EmptyPack_GoesStraightToRow()
        {
            var reveal = new PackRevealStateMachine();

            reveal.Begin(CreatePack(0));

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
        }

        [Test]
        public void RevealNext_WholePack_ReturnsSlotsInOrder()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));

            for (int expectedSlot = 0; expectedSlot < 5; expectedSlot++)
            {
                Assert.That(reveal.RevealNext(), Is.EqualTo(expectedSlot));
            }

            Assert.That(reveal.HasUnrevealedCards, Is.False);
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Revealing));
        }

        [Test]
        public void RevealNext_AfterLastCard_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(2));
            reveal.RevealNext();
            reveal.RevealNext();

            Assert.Throws<InvalidOperationException>(() => reveal.RevealNext());
        }

        [Test]
        public void RevealNext_WhileIdle_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PackRevealStateMachine().RevealNext());
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(5)]
        public void ShowRow_AtAnyRevealPoint_EntersRowWithEveryCardRevealed(int revealedBefore)
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));
            for (int i = 0; i < revealedBefore; i++)
            {
                reveal.RevealNext();
            }

            reveal.ShowRow();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
            Assert.That(reveal.RevealedCount, Is.EqualTo(5));
        }

        [Test]
        public void ShowRow_WhileInRow_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));
            reveal.ShowRow();

            Assert.Throws<InvalidOperationException>(() => reveal.ShowRow());
        }

        [Test]
        public void Store_MidReveal_ReturnsToIdle()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));
            reveal.RevealNext();

            reveal.Store();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(reveal.Pack, Is.Null);
            Assert.That(reveal.RevealedCount, Is.EqualTo(0));
        }

        [Test]
        public void Store_FromRow_ReturnsToIdleAndAcceptsNextPack()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));
            reveal.ShowRow();

            reveal.Store();
            reveal.Begin(CreatePack(5));

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Revealing));
        }

        [Test]
        public void Store_WhileIdle_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PackRevealStateMachine().Store());
        }

        [Test]
        public void Showcase_FromRow_EntersShowcaseWithThatSlot()
        {
            PackRevealStateMachine reveal = CreateInRow();

            reveal.Showcase(3);

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Showcase));
            Assert.That(reveal.ShowcasedSlot, Is.EqualTo(3));
        }

        [Test]
        public void Showcase_WhileRevealing_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));

            Assert.Throws<InvalidOperationException>(() => reveal.Showcase(0));
        }

        [Test]
        public void Showcase_WhileAnotherCardIsShowcased_Throws()
        {
            PackRevealStateMachine reveal = CreateInRow();
            reveal.Showcase(1);

            Assert.Throws<InvalidOperationException>(() => reveal.Showcase(2));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void Showcase_SlotOutsidePack_Throws(int slotIndex)
        {
            PackRevealStateMachine reveal = CreateInRow();

            Assert.Throws<ArgumentOutOfRangeException>(() => reveal.Showcase(slotIndex));
        }

        [Test]
        public void ReturnToRow_FromShowcase_BackToRowWithNoShowcasedSlot()
        {
            PackRevealStateMachine reveal = CreateInRow();
            reveal.Showcase(4);

            reveal.ReturnToRow();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
            Assert.That(reveal.ShowcasedSlot, Is.EqualTo(PackRevealStateMachine.NoSlot));
        }

        [Test]
        public void ReturnToRow_WhileInRow_Throws()
        {
            PackRevealStateMachine reveal = CreateInRow();

            Assert.Throws<InvalidOperationException>(() => reveal.ReturnToRow());
        }

        [Test]
        public void Store_FromShowcase_ReturnsToIdle()
        {
            PackRevealStateMachine reveal = CreateInRow();
            reveal.Showcase(0);

            reveal.Store();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(reveal.ShowcasedSlot, Is.EqualTo(PackRevealStateMachine.NoSlot));
        }

        [Test]
        public void IsSlowSlot_DefaultPacingFiveCards_OnlyLastTwoSlotsAreSlow()
        {
            var pacing = new RevealPacing();

            bool[] isSlow = new bool[5];
            for (int slot = 0; slot < 5; slot++)
            {
                isSlow[slot] = pacing.IsSlowSlot(slot, 5);
            }

            Assert.That(isSlow, Is.EqualTo(new[] { false, false, false, true, true }));
        }

        [Test]
        public void IsSlowSlot_OutOfRangeSlot_IsFalse()
        {
            var pacing = new RevealPacing();

            Assert.That(pacing.IsSlowSlot(5, 5), Is.False);
            Assert.That(pacing.IsSlowSlot(-1, 5), Is.False);
        }

        private static PackRevealStateMachine CreateInRow()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(5));
            reveal.ShowRow();
            return reveal;
        }

        private static OpenedPack CreatePack(int cardCount)
        {
            var cards = new Card[cardCount];
            for (int i = 0; i < cardCount; i++)
            {
                cards[i] = new Card($"SetA_Common_{i + 1:00}", $"Card {i + 1}", "SetA", RarityTier.Common, 5);
            }

            return new OpenedPack("test-pack", cards);
        }
    }
}
