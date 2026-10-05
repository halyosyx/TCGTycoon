using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Economy;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Session;
using Game.Core.Store;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Session
{
    public sealed class GameSessionTests
    {
        private const int Seed = 20260926;
        private const long StartingCashCents = 50_000;

        [Test]
        public void OpenPack_OnePack_AddsEveryCardToInventoryBeforeReturning()
        {
            var session = CreateSession();

            OpenedPack pack = session.OpenPack();

            // Nothing runs between the roll and the return, so the cards are owned by the time any
            // caller (a reveal animation) sees them.
            Assert.That(TotalCards(session.Inventory), Is.EqualTo(pack.Cards.Count));
            foreach (Card card in pack.Cards)
            {
                Assert.That(session.Inventory.CountOf(card.Id, card.Tier), Is.GreaterThanOrEqualTo(1), card.Id);
            }
        }

        [Test]
        public void OpenPack_ThreePacks_InventoryHoldsTwentyOneCards()
        {
            var session = CreateSession();

            session.OpenPack();
            session.OpenPack();
            session.OpenPack();

            Assert.That(TotalCards(session.Inventory), Is.EqualTo(21));
        }

        [Test]
        public void OpenPack_OnePack_CostBasisSumsToPackPrice()
        {
            var session = CreateSession();

            session.OpenPack();

            Assert.That(session.Inventory.TotalCostBasisCents, Is.EqualTo(TestContent.StartingPackPriceCents));
        }

        [Test]
        public void OpenPack_SameSeed_ReturnsSameCards()
        {
            var first = CreateSession();
            var second = CreateSession();

            for (int packNumber = 0; packNumber < 20; packNumber++)
            {
                Assert.That(Ids(second.OpenPack()), Is.EqualTo(Ids(first.OpenPack())));
            }
        }

        [Test]
        public void Constructor_NullPack_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GameSession(null, TestContent.SetAPool(), Seed));
        }

        [Test]
        public void Constructor_NullPool_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GameSession(TestContent.StartingPack(), null, Seed));
        }

        [Test]
        public void Constructor_ValidContent_ExposesPackPoolAndEmptyInventory()
        {
            PackConfig pack = TestContent.StartingPack();
            CardPool pool = TestContent.SetAPool();

            var session = new GameSession(pack, pool, Seed);

            Assert.That(session.Pack, Is.SameAs(pack));
            Assert.That(session.Pool, Is.SameAs(pool));
            Assert.That(session.Inventory.Stacks, Is.Empty);
        }

        [TestCase(3)]
        [TestCase(12)]
        public void OpenPack_PackOfNSlots_InventoryGainsNCardsAtOnce(int slotCount)
        {
            var slots = new PackSlot[slotCount];
            for (int slot = 0; slot < slotCount; slot++)
            {
                slots[slot] = TestContent.Slot(TestContent.Weight(RarityTier.Common, 1));
            }

            var session = new GameSession(TestContent.Pack(100, slots), TestContent.SetAPool(), Seed);

            OpenedPack pack = session.OpenPack();

            Assert.That(pack.Cards.Count, Is.EqualTo(slotCount));
            Assert.That(TotalCards(session.Inventory), Is.EqualTo(slotCount), "Every card is owned as soon as OpenPack returns.");
        }

        [Test]
        public void Constructor_ThreeArguments_NoCashAndEmptyStore()
        {
            var session = CreateSession();

            Assert.That(session.Economy.BalanceCents, Is.EqualTo(0));
            Assert.That(session.Store.Listings, Is.Empty);
        }

        [Test]
        public void Constructor_WithStore_StartingCashAndListings()
        {
            var session = CreateStoreSession();

            Assert.That(session.Economy.BalanceCents, Is.EqualTo(StartingCashCents));
            Assert.That(session.Store.Listings.Count, Is.EqualTo(3));
        }

        [Test]
        public void Store_PlaceOrder_SpendsSessionCashAndFillsSessionInventory()
        {
            var session = CreateStoreSession();
            session.Store.SetQuantity(StoreFixtures.ChampionsPackId, 12);

            session.Store.PlaceOrder();

            Assert.That(session.Economy.BalanceCents, Is.EqualTo(44_000));
            Assert.That(session.Inventory.CountOfSealed(StoreFixtures.ChampionsPackId), Is.EqualTo(12));
        }

        [Test]
        public void OpenSealedPack_CardCostBasesSumToPaidUnitPrice()
        {
            // 90% of 555 = 499.5, paid 500: an uneven share over 7 cards still sums exactly.
            var catalog = StoreFixtures.Catalog(StoreFixtures.Listing(StoreFixtures.ChampionsPack(555, 90)));
            var session = new GameSession(TestContent.StartingPack(), TestContent.SetAPool(), Seed, StartingCashCents, catalog);
            session.Store.SetQuantity(StoreFixtures.ChampionsPackId, 1);
            session.Store.PlaceOrder();

            OpenedPack pack = session.OpenSealedPack(StoreFixtures.ChampionsPackId);

            Assert.That(pack.Cards.Count, Is.EqualTo(7));
            Assert.That(session.Inventory.SealedStacks, Is.Empty);
            Assert.That(TotalCards(session.Inventory), Is.EqualTo(7), "Every card is owned as soon as OpenSealedPack returns.");
            Assert.That(session.Inventory.TotalCostBasisCents, Is.EqualTo(500));
        }

        [Test]
        public void OpenSealedPack_TwelveOwned_ElevenLeft()
        {
            var session = CreateStoreSession();
            session.Store.SetQuantity(StoreFixtures.OriginsPackId, 12);
            session.Store.PlaceOrder();

            session.OpenSealedPack(StoreFixtures.OriginsPackId);

            Assert.That(session.Inventory.CountOfSealed(StoreFixtures.OriginsPackId), Is.EqualTo(11));
            Assert.That(session.Inventory.TotalCostBasisCents, Is.EqualTo(12 * StoreFixtures.OriginsPackMarketCents));
        }

        [Test]
        public void OpenSealedPack_FromHeld_OpensTheHeldPackAndCardsLandInBinder()
        {
            var session = CreateStoreSession();
            session.Store.SetQuantity(StoreFixtures.ChampionsPackId, 3);
            session.Store.PlaceOrder();
            session.Inventory.Move(ItemRef.Sealed(StoreFixtures.ChampionsPackId), ItemLocation.Binder, ItemLocation.Held);

            OpenedPack pack = session.OpenSealedPack(StoreFixtures.ChampionsPackId, ItemLocation.Held);

            Assert.That(session.Inventory.CountOfSealed(StoreFixtures.ChampionsPackId, ItemLocation.Held), Is.EqualTo(0));
            Assert.That(session.Inventory.CountOfSealed(StoreFixtures.ChampionsPackId, ItemLocation.Binder), Is.EqualTo(2));
            Assert.That(session.Inventory.CountIn(ItemLocation.Binder), Is.EqualTo(2 + pack.Cards.Count));
        }

        [Test]
        public void OpenSealedPack_NoneHeld_Throws()
        {
            var session = CreateStoreSession();
            session.Store.SetQuantity(StoreFixtures.ChampionsPackId, 1);
            session.Store.PlaceOrder();

            Assert.Throws<InvalidOperationException>(() => session.OpenSealedPack(StoreFixtures.ChampionsPackId, ItemLocation.Held));
            Assert.That(session.Inventory.CountOfSealed(StoreFixtures.ChampionsPackId), Is.EqualTo(1));
        }

        [Test]
        public void OpenSealedPack_NoneOwned_ThrowsAndChangesNothing()
        {
            var session = CreateStoreSession();

            Assert.Throws<InvalidOperationException>(() => session.OpenSealedPack(StoreFixtures.ChampionsPackId));
            Assert.That(session.Inventory.Stacks, Is.Empty);
        }

        [Test]
        public void OpenSealedPack_ProductIsNotAPack_Throws()
        {
            var session = CreateStoreSession();
            session.Inventory.AddSealed(StoreFixtures.ChampionsBundleId, 1, 2_800);

            Assert.Throws<InvalidOperationException>(() => session.OpenSealedPack(StoreFixtures.ChampionsBundleId));
            Assert.That(session.Inventory.CountOfSealed(StoreFixtures.ChampionsBundleId), Is.EqualTo(1));
        }

        [Test]
        public void OpenSealedPack_SameSeed_SameCards()
        {
            var first = CreateStoreSession();
            var second = CreateStoreSession();
            foreach (GameSession session in new[] { first, second })
            {
                session.Store.SetQuantity(StoreFixtures.ChampionsPackId, 5);
                session.Store.PlaceOrder();
            }

            for (int packNumber = 0; packNumber < 5; packNumber++)
            {
                Assert.That(Ids(second.OpenSealedPack(StoreFixtures.ChampionsPackId)), Is.EqualTo(Ids(first.OpenSealedPack(StoreFixtures.ChampionsPackId))));
            }
        }

        private static GameSession CreateStoreSession()
        {
            return new GameSession(TestContent.StartingPack(), TestContent.SetAPool(), Seed, StartingCashCents, StoreFixtures.DefaultCatalog());
        }

        private static GameSession CreateSession()
        {
            return new GameSession(TestContent.StartingPack(), TestContent.SetAPool(), Seed);
        }

        private static int TotalCards(InventoryService inventory)
        {
            int total = 0;
            foreach (InventoryStack stack in inventory.Stacks)
            {
                total += stack.Count;
            }

            return total;
        }

        private static List<string> Ids(OpenedPack pack)
        {
            var ids = new List<string>();
            foreach (Card card in pack.Cards)
            {
                ids.Add(card.Id);
            }

            return ids;
        }
    }
}
