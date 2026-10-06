using System;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Unity.UI.PackOpening;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.PackOpening
{
    public sealed class PackRevealStateMachineTests
    {
        // The default pack's size (GDD v1.7). Tests that don't care about the size use it; the
        // size-specific tests below cover other counts so nothing depends on seven.
        private const int PackSize = 7;

        [Test]
        public void Begin_FromIdle_EntersRevealingWithNothingRevealed()
        {
            var reveal = new PackRevealStateMachine();

            reveal.Begin(CreatePack(PackSize));

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Revealing));
            Assert.That(reveal.RevealedCount, Is.EqualTo(0));
            Assert.That(reveal.CardCount, Is.EqualTo(PackSize));
        }

        [Test]
        public void Begin_WhilePackOnScreen_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));

            Assert.Throws<InvalidOperationException>(() => reveal.Begin(CreatePack(PackSize)));
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
            reveal.Begin(CreatePack(PackSize));

            for (int expectedSlot = 0; expectedSlot < PackSize; expectedSlot++)
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
        [TestCase(PackSize)]
        public void ShowRow_AtAnyRevealPoint_EntersRowWithEveryCardRevealed(int revealedBefore)
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));
            for (int i = 0; i < revealedBefore; i++)
            {
                reveal.RevealNext();
            }

            reveal.ShowRow();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
            Assert.That(reveal.RevealedCount, Is.EqualTo(PackSize));
        }

        [Test]
        public void ShowRow_WhileInRow_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));
            reveal.ShowRow();

            Assert.Throws<InvalidOperationException>(() => reveal.ShowRow());
        }

        [Test]
        public void Store_MidReveal_ReturnsToIdle()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));
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
            reveal.Begin(CreatePack(PackSize));
            reveal.ShowRow();

            reveal.Store();
            reveal.Begin(CreatePack(PackSize));

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
            reveal.Begin(CreatePack(PackSize));

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
        [TestCase(PackSize)]
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
        public void IsSlowSlot_DefaultPacingSevenCards_OnlySlotsSixAndSevenAreSlow()
        {
            var pacing = new RevealPacing();

            bool[] isSlow = new bool[PackSize];
            for (int slot = 0; slot < PackSize; slot++)
            {
                isSlow[slot] = pacing.IsSlowSlot(slot, PackSize);
            }

            Assert.That(isSlow, Is.EqualTo(new[] { false, false, false, false, false, true, true }));
        }

        [TestCase(3)]
        [TestCase(12)]
        public void IsSlowSlot_AnyPackSize_LastTwoSlotsCountedFromTheEnd(int cardCount)
        {
            var pacing = new RevealPacing();

            for (int slot = 0; slot < cardCount; slot++)
            {
                Assert.That(pacing.IsSlowSlot(slot, cardCount), Is.EqualTo(slot >= cardCount - 2), $"Slot {slot + 1} of {cardCount}");
            }
        }

        [TestCase(3)]
        [TestCase(7)]
        [TestCase(12)]
        public void RevealNext_PackOfAnySize_RevealsEverySlotInOrderThenShowsTheRow(int cardCount)
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(cardCount));

            for (int expectedSlot = 0; expectedSlot < cardCount; expectedSlot++)
            {
                Assert.That(reveal.RevealNext(), Is.EqualTo(expectedSlot));
            }

            reveal.ShowRow();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
            Assert.That(reveal.RevealedCount, Is.EqualTo(cardCount));
        }

        [Test]
        public void IsSlowSlot_OutOfRangeSlot_IsFalse()
        {
            var pacing = new RevealPacing();

            Assert.That(pacing.IsSlowSlot(PackSize, PackSize), Is.False);
            Assert.That(pacing.IsSlowSlot(-1, PackSize), Is.False);
        }

        // --- Tearing (F2d): the commit runs once, when the tear starts ---

        [Test]
        public void TryBeginTear_FromIdle_CommitsOnceAndEntersTearing()
        {
            var reveal = new PackRevealStateMachine();
            int commits = 0;

            bool isStarted = reveal.TryBeginTear(() => { commits++; return CreatePack(PackSize); });

            Assert.That(isStarted, Is.True);
            Assert.That(commits, Is.EqualTo(1));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Tearing));
            Assert.That(reveal.CardCount, Is.EqualTo(PackSize));
            Assert.That(reveal.RevealedCount, Is.EqualTo(0));
        }

        [Test]
        public void TryBeginTear_WhileTearing_ReturnsFalseWithoutCommitting()
        {
            var reveal = new PackRevealStateMachine();
            reveal.TryBeginTear(() => CreatePack(PackSize));
            int commits = 0;

            bool isStarted = reveal.TryBeginTear(() => { commits++; return CreatePack(PackSize); });

            Assert.That(isStarted, Is.False);
            Assert.That(commits, Is.EqualTo(0));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Tearing));
        }

        [Test]
        public void TryBeginTear_WhileRevealing_ReturnsFalseWithoutCommitting()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));
            int commits = 0;

            bool isStarted = reveal.TryBeginTear(() => { commits++; return CreatePack(PackSize); });

            Assert.That(isStarted, Is.False);
            Assert.That(commits, Is.EqualTo(0));
        }

        [Test]
        public void TryBeginTear_CommitThrows_StaysIdle()
        {
            var reveal = new PackRevealStateMachine();

            Assert.Throws<InvalidOperationException>(() => reveal.TryBeginTear(() => throw new InvalidOperationException("No pack held.")));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(reveal.Pack, Is.Null);
        }

        [Test]
        public void TryBeginTear_CommitReturnsNull_ThrowsAndStaysIdle()
        {
            var reveal = new PackRevealStateMachine();

            Assert.Throws<InvalidOperationException>(() => reveal.TryBeginTear(() => null));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
        }

        [Test]
        public void FinishTear_FromTearing_EntersRevealingWithNothingRevealed()
        {
            PackRevealStateMachine reveal = CreateTearing(PackSize);

            reveal.FinishTear();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Revealing));
            Assert.That(reveal.RevealedCount, Is.EqualTo(0));
            Assert.That(reveal.RevealNext(), Is.EqualTo(0));
        }

        [Test]
        public void FinishTear_EmptyPack_GoesStraightToRow()
        {
            PackRevealStateMachine reveal = CreateTearing(0);

            reveal.FinishTear();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
        }

        [Test]
        public void FinishTear_WhileIdle_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PackRevealStateMachine().FinishTear());
        }

        [Test]
        public void FinishTear_WhileRevealing_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));

            Assert.Throws<InvalidOperationException>(() => reveal.FinishTear());
        }

        [Test]
        public void ShowRow_FromTearing_EntersRowWithEveryCardRevealed()
        {
            PackRevealStateMachine reveal = CreateTearing(PackSize);

            reveal.ShowRow();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
            Assert.That(reveal.RevealedCount, Is.EqualTo(PackSize));
        }

        [Test]
        public void Store_MidTear_ReturnsToIdleAndAcceptsNextTear()
        {
            PackRevealStateMachine reveal = CreateTearing(PackSize);

            reveal.Store();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(reveal.Pack, Is.Null);
            Assert.That(reveal.TryBeginTear(() => CreatePack(PackSize)), Is.True);
        }

        [Test]
        public void RevealShowcaseAndBegin_WhileTearing_Throw()
        {
            PackRevealStateMachine reveal = CreateTearing(PackSize);

            Assert.Throws<InvalidOperationException>(() => reveal.RevealNext());
            Assert.Throws<InvalidOperationException>(() => reveal.Showcase(0));
            Assert.Throws<InvalidOperationException>(() => reveal.ReturnToRow());
            Assert.Throws<InvalidOperationException>(() => reveal.Begin(CreatePack(PackSize)));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Tearing));
        }

        private static PackRevealStateMachine CreateTearing(int cardCount)
        {
            var reveal = new PackRevealStateMachine();
            reveal.TryBeginTear(() => CreatePack(cardCount));
            return reveal;
        }

        private static PackRevealStateMachine CreateInRow()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));
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
