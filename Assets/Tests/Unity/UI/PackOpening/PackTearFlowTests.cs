using System;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Session;
using Game.Core.Store;
using Game.Unity.UI.PackOpening;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.PackOpening
{
    /// <summary>
    /// The opening's commit seam against a real <see cref="GameSession"/>: the pack opening screen zooms
    /// with <see cref="PackRevealStateMachine.BeginZoom"/> and rips with exactly this commit, so these pin
    /// down "the zoom commits nothing, the rip commits once before anything is shown, and no path loses
    /// a card".
    /// </summary>
    public sealed class PackTearFlowTests
    {
        private const string PackId = "SetA_Pack";
        private const long PackCents = 500;
        private const int PackSize = 7;

        [Test]
        public void ZoomThenBackOut_CommitsNothing()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            int itemsBefore = session.Inventory.TotalItemCount;
            long costBefore = session.Inventory.TotalCostBasisCents;
            var reveal = new PackRevealStateMachine();

            reveal.BeginZoom();
            reveal.CancelZoom();

            Assert.That(CountCards(session.Inventory), Is.EqualTo(0));
            Assert.That(session.Inventory.CountOfSealed(PackId, ItemLocation.Held), Is.EqualTo(1), "The pack is still in the hand.");
            Assert.That(session.Inventory.TotalItemCount, Is.EqualTo(itemsBefore));
            Assert.That(session.Inventory.TotalCostBasisCents, Is.EqualTo(costBefore));
        }

        [Test]
        public void Zooming_CommitsNothingUntilTheRip()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();

            reveal.BeginZoom();

            Assert.That(CountCards(session.Inventory), Is.EqualTo(0));
            Assert.That(session.Inventory.CountOfSealed(PackId, ItemLocation.Held), Is.EqualTo(1));
        }

        [Test]
        public void TryBeginRip_RepeatedClicks_CommitsOnce()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 1);
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();
            int commits = 0;
            Func<OpenedPack> commit = () =>
            {
                commits++;
                return session.OpenSealedPack(PackId, ItemLocation.Held);
            };

            for (int click = 0; click < 5; click++)
            {
                reveal.TryBeginRip(commit);
            }

            Assert.That(commits, Is.EqualTo(1));
            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountOfSealed(PackId, ItemLocation.Held), Is.EqualTo(0));
            Assert.That(session.Inventory.CountOfSealed(PackId, ItemLocation.Binder), Is.EqualTo(1), "The pack in the binder is untouched.");
        }

        [Test]
        public void TryBeginRip_CardsOwnedBeforeTheRipAnimates()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();

            reveal.TryBeginRip(() => session.OpenSealedPack(PackId, ItemLocation.Held));

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Ripping));
            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountIn(ItemLocation.Binder), Is.EqualTo(PackSize));
        }

        [Test]
        public void FinishRip_RevealShowsTheCommittedCardsInSlotOrder()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();
            OpenedPack committed = null;
            reveal.BeginZoom();
            reveal.TryBeginRip(() => committed = session.OpenSealedPack(PackId, ItemLocation.Held));

            reveal.FinishRip();

            Assert.That(reveal.Pack, Is.SameAs(committed));
            for (int expectedSlot = 0; expectedSlot < committed.Cards.Count; expectedSlot++)
            {
                int slot = reveal.RevealNext();
                Assert.That(slot, Is.EqualTo(expectedSlot));
                Card card = reveal.Pack.Cards[slot];
                Assert.That(card, Is.SameAs(committed.Cards[expectedSlot]));
                Assert.That(session.Inventory.CountOf(card.Id, card.Tier), Is.GreaterThan(0), $"Slot {slot + 1} is in the inventory.");
            }
        }

        [Test]
        public void Store_MidRip_KeepsEveryCard()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            long costBefore = session.Inventory.TotalCostBasisCents;
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();
            reveal.TryBeginRip(() => session.OpenSealedPack(PackId, ItemLocation.Held));

            reveal.Store();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountOfSealed(PackId), Is.EqualTo(0));
            Assert.That(session.Inventory.TotalCostBasisCents, Is.EqualTo(costBefore));
        }

        [Test]
        public void Store_MidReveal_KeepsEveryCard()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();
            reveal.TryBeginRip(() => session.OpenSealedPack(PackId, ItemLocation.Held));
            reveal.FinishRip();
            reveal.RevealNext();
            reveal.RevealNext();

            reveal.Store();

            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountOfSealed(PackId), Is.EqualTo(0));
        }

        // Space during the zoom counts as the rip click, then goes straight to the row.
        [Test]
        public void SpaceDuringZoom_CommitsOnceThenShowsTheRow()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();
            reveal.BeginZoom();
            int commits = 0;
            Func<OpenedPack> commit = () =>
            {
                commits++;
                return session.OpenSealedPack(PackId, ItemLocation.Held);
            };

            reveal.TryBeginRip(commit);
            reveal.ShowRow();
            reveal.TryBeginRip(commit);

            Assert.That(commits, Is.EqualTo(1));
            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Row));
            Assert.That(reveal.RevealedCount, Is.EqualTo(PackSize));
            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
        }

        private static GameSession CreateSessionHoldingOnePack(int packsInBinder)
        {
            var pool = new CardPool(new[]
            {
                new Card("C1", "Common one", "SetA", RarityTier.Common, 5),
                new Card("C2", "Common two", "SetA", RarityTier.Common, 5),
                new Card("C3", "Common three", "SetA", RarityTier.Common, 5),
            });
            var slots = new PackSlot[PackSize];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new PackSlot(new[] { new TierWeight(RarityTier.Common, 1) });
            }

            var pack = new PackConfig("test-pack", "Test Pack", PackCents, slots);
            var product = new Product(PackId, "Booster Pack", "SetA", ProductType.BoosterPack, 1, PackCents, 100, pack, pool);
            var catalog = new StoreCatalog(new[] { new StoreCatalogListing(product, ListingAvailability.Available, StoreCatalogListing.UnlimitedStock) });
            var session = new GameSession(pack, pool, 1, 0, catalog);

            session.Inventory.AddSealed(PackId, packsInBinder + 1, PackCents);
            MoveResult taken = session.Inventory.Move(ItemRef.Sealed(PackId), ItemLocation.Binder, ItemLocation.Held);
            Assert.That(taken.IsSuccess, Is.True, "Setup: one pack in the hand.");
            return session;
        }

        private static int CountCards(InventoryService inventory)
        {
            int total = 0;
            foreach (InventoryStack stack in inventory.Stacks)
            {
                total += stack.Count;
            }

            return total;
        }
    }
}
