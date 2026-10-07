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

        // --- Zoom and rip (F2e): the zoom never commits; the rip click commits once ---

        [Test]
        public void BeginZoom_FromIdle_EntersZoomingWithNoPack()
        {
            var reveal = new PackRevealStateMachine();

            reveal.BeginZoom();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Zooming));
            Assert.That(reveal.Pack, Is.Null);
            Assert.That(reveal.CardCount, Is.EqualTo(0));
        }

        [Test]
        public void BeginZoom_WhileRevealing_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.Begin(CreatePack(PackSize));

            Assert.Throws<InvalidOperationException>(() => reveal.BeginZoom());
        }

        [Test]
        public void CancelZoom_FromZooming_BackToIdleAndAcceptsTheNextZoom()
        {
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();

            reveal.CancelZoom();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(reveal.Pack, Is.Null);
            reveal.BeginZoom();
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Zooming));
        }

        [Test]
        public void CancelZoom_WhileRipping_Throws()
        {
            PackRevealStateMachine reveal = CreateRipping(PackSize);

            Assert.Throws<InvalidOperationException>(() => reveal.CancelZoom());
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Ripping));
        }

        [Test]
        public void TryBeginRip_FromZooming_CommitsOnceAndEntersRipping()
        {
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();
            int commits = 0;

            bool isStarted = reveal.TryBeginRip(() => { commits++; return CreatePack(PackSize); });

            Assert.That(isStarted, Is.True);
            Assert.That(commits, Is.EqualTo(1));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Ripping));
            Assert.That(reveal.CardCount, Is.EqualTo(PackSize));
            Assert.That(reveal.RevealedCount, Is.EqualTo(0));
        }

        [Test]
        public void TryBeginRip_WhileRipping_ReturnsFalseWithoutCommitting()
        {
            PackRevealStateMachine reveal = CreateRipping(PackSize);
            int commits = 0;

            bool isStarted = reveal.TryBeginRip(() => { commits++; return CreatePack(PackSize); });

            Assert.That(isStarted, Is.False);
            Assert.That(commits, Is.EqualTo(0));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Ripping));
        }

        [Test]
        public void TryBeginRip_WhileIdleOrRevealing_ReturnsFalseWithoutCommitting()
        {
            var idle = new PackRevealStateMachine();
            var revealing = new PackRevealStateMachine();
            revealing.Begin(CreatePack(PackSize));
            int commits = 0;

            Assert.That(idle.TryBeginRip(() => { commits++; return CreatePack(PackSize); }), Is.False);
            Assert.That(revealing.TryBeginRip(() => { commits++; return CreatePack(PackSize); }), Is.False);
            Assert.That(commits, Is.EqualTo(0));
        }

        [Test]
        public void TryBeginRip_CommitThrows_StaysZooming()
        {
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();

            Assert.Throws<InvalidOperationException>(() => reveal.TryBeginRip(() => throw new InvalidOperationException("No pack held.")));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Zooming));
            Assert.That(reveal.Pack, Is.Null);
        }

        [Test]
        public void TryBeginRip_CommitReturnsNull_ThrowsAndStaysZooming()
        {
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();

            Assert.Throws<InvalidOperationException>(() => reveal.TryBeginRip(() => null));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Zooming));
        }

        [Test]
        public void FinishRip_FromRipping_EntersRevealingWithNothingRevealed()
        {
            PackRevealStateMachine reveal = CreateRipping(PackSize);

            reveal.FinishRip();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Revealing));
            Assert.That(reveal.RevealedCount, Is.EqualTo(0));
            Assert.That(reveal.RevealNext(), Is.EqualTo(0));
        }

        [Test]
        public void FinishRip_EmptyPack_GoesStraightToRow()
        {
            PackRevealStateMachine reveal = CreateRipping(0);

            reveal.FinishRip();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
        }

        [Test]
        public void FinishRip_WhileZoomingOrRevealing_Throws()
        {
            var zooming = new PackRevealStateMachine();
            zooming.BeginZoom();
            var revealing = new PackRevealStateMachine();
            revealing.Begin(CreatePack(PackSize));

            Assert.Throws<InvalidOperationException>(() => zooming.FinishRip());
            Assert.Throws<InvalidOperationException>(() => revealing.FinishRip());
        }

        [Test]
        public void ShowRow_FromRipping_EntersRowWithEveryCardRevealed()
        {
            PackRevealStateMachine reveal = CreateRipping(PackSize);

            reveal.ShowRow();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
            Assert.That(reveal.RevealedCount, Is.EqualTo(PackSize));
        }

        [Test]
        public void ShowRow_WhileZooming_Throws()
        {
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();

            Assert.Throws<InvalidOperationException>(() => reveal.ShowRow());
        }

        [Test]
        public void Store_MidRip_ReturnsToIdleAndAcceptsTheNextZoom()
        {
            PackRevealStateMachine reveal = CreateRipping(PackSize);

            reveal.Store();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(reveal.Pack, Is.Null);
            reveal.BeginZoom();
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Zooming));
        }

        [Test]
        public void RevealShowcaseAndBegin_WhileZoomingOrRipping_Throw()
        {
            var zooming = new PackRevealStateMachine();
            zooming.BeginZoom();
            PackRevealStateMachine ripping = CreateRipping(PackSize);

            foreach (PackRevealStateMachine reveal in new[] { zooming, ripping })
            {
                Assert.Throws<InvalidOperationException>(() => reveal.RevealNext());
                Assert.Throws<InvalidOperationException>(() => reveal.Showcase(0));
                Assert.Throws<InvalidOperationException>(() => reveal.ReturnToRow());
                Assert.Throws<InvalidOperationException>(() => reveal.Begin(CreatePack(PackSize)));
            }
        }

        private static PackRevealStateMachine CreateRipping(int cardCount)
        {
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();
            reveal.TryBeginRip(() => CreatePack(cardCount));
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
