using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Session;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Session
{
    public sealed class GameSessionTests
    {
        private const int Seed = 20260926;

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
        public void OpenPack_ThreePacks_InventoryHoldsFifteenCards()
        {
            var session = CreateSession();

            session.OpenPack();
            session.OpenPack();
            session.OpenPack();

            Assert.That(TotalCards(session.Inventory), Is.EqualTo(15));
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
