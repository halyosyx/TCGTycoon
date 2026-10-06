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
    /// The tear's commit seam against a real <see cref="GameSession"/>: the pack opening screen starts a
    /// tear with exactly this call, so these pin down "commits once, before anything is shown, and no
    /// path loses a card".
    /// </summary>
    public sealed class PackTearFlowTests
    {
        private const string PackId = "SetA_Pack";
        private const long PackCents = 500;
        private const int PackSize = 7;

        [Test]
        public void TryBeginTear_RepeatedPresses_CommitsOnce()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 1);
            var reveal = new PackRevealStateMachine();
            int commits = 0;
            Func<OpenedPack> commit = () =>
            {
                commits++;
                return session.OpenSealedPack(PackId, ItemLocation.Held);
            };

            for (int press = 0; press < 5; press++)
            {
                reveal.TryBeginTear(commit);
            }

            Assert.That(commits, Is.EqualTo(1));
            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountOfSealed(PackId, ItemLocation.Held), Is.EqualTo(0));
            Assert.That(session.Inventory.CountOfSealed(PackId, ItemLocation.Binder), Is.EqualTo(1), "The pack in the binder is untouched.");
        }

        [Test]
        public void TryBeginTear_CardsOwnedBeforeTheTearFinishes()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();

            reveal.TryBeginTear(() => session.OpenSealedPack(PackId, ItemLocation.Held));

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Tearing));
            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountIn(ItemLocation.Binder), Is.EqualTo(PackSize));
        }

        [Test]
        public void FinishTear_RevealShowsTheCommittedCardsInSlotOrder()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();
            OpenedPack committed = null;
            reveal.TryBeginTear(() => committed = session.OpenSealedPack(PackId, ItemLocation.Held));

            reveal.FinishTear();

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
        public void Store_MidTear_KeepsEveryCard()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            long costBefore = session.Inventory.TotalCostBasisCents;
            var reveal = new PackRevealStateMachine();
            reveal.TryBeginTear(() => session.OpenSealedPack(PackId, ItemLocation.Held));

            reveal.Store();

            Assert.That(reveal.State, Is.EqualTo(PackRevealState.Idle));
            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountOfSealed(PackId), Is.EqualTo(0));
            Assert.That(session.Inventory.TotalCostBasisCents, Is.EqualTo(costBefore));
        }

        [Test]
        public void ShowRow_MidTear_KeepsEveryCard()
        {
            GameSession session = CreateSessionHoldingOnePack(packsInBinder: 0);
            var reveal = new PackRevealStateMachine();
            reveal.TryBeginTear(() => session.OpenSealedPack(PackId, ItemLocation.Held));

            reveal.ShowRow();
            reveal.Store();

            Assert.That(CountCards(session.Inventory), Is.EqualTo(PackSize));
            Assert.That(session.Inventory.CountOfSealed(PackId), Is.EqualTo(0));
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
