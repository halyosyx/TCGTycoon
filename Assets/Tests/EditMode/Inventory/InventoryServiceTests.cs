using System;
using System.Linq;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Inventory
{
    public sealed class InventoryServiceTests
    {
        private static readonly Card s_rare = TestContent.CreateCard("SetA_R01", RarityTier.Rare, 60);
        private static readonly Card s_common = TestContent.CreateCard("SetA_C01", RarityTier.Common, 5);

        [Test]
        public void Add_SameCardAndTierTwice_MergesIntoOneStack()
        {
            var inventory = new InventoryService();

            inventory.Add(s_rare, 100);
            inventory.Add(s_rare, 50);

            Assert.That(inventory.Stacks.Count, Is.EqualTo(1));
            Assert.That(inventory.Stacks[0].Count, Is.EqualTo(2));
            Assert.That(inventory.Stacks[0].CostBasisCents, Is.EqualTo(150));
        }

        [Test]
        public void Add_DifferentCards_CreatesSeparateStacks()
        {
            var inventory = new InventoryService();

            inventory.Add(s_rare, 10);
            inventory.Add(s_common, 10);

            Assert.That(inventory.Stacks.Count, Is.EqualTo(2));
            Assert.That(inventory.CountOf(s_rare.Id, RarityTier.Rare), Is.EqualTo(1));
            Assert.That(inventory.CountOf(s_common.Id, RarityTier.Common), Is.EqualTo(1));
        }

        [Test]
        public void AddPack_EvenlyDivisiblePrice_SplitsCostEqually()
        {
            var inventory = new InventoryService();
            OpenedPack pack = PackOf(TestContent.CreateCard("A", RarityTier.Common), TestContent.CreateCard("B", RarityTier.Common),
                TestContent.CreateCard("C", RarityTier.Common), TestContent.CreateCard("D", RarityTier.Uncommon), TestContent.CreateCard("E", RarityTier.Rare));

            inventory.AddPack(pack, 425);

            Assert.That(inventory.Stacks.Select(stack => stack.CostBasisCents), Is.All.EqualTo(85));
        }

        [Test]
        public void AddPack_UnevenPrice_GivesLeftoverCentsToEarliestSlots()
        {
            var inventory = new InventoryService();
            OpenedPack pack = PackOf(TestContent.CreateCard("First", RarityTier.Common), TestContent.CreateCard("Second", RarityTier.Common),
                TestContent.CreateCard("Third", RarityTier.Rare));

            inventory.AddPack(pack, 425);

            Assert.That(inventory.Stacks.Select(stack => stack.CostBasisCents).ToArray(), Is.EqualTo(new long[] { 142, 142, 141 }));
            Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(425));
        }

        [Test]
        public void AddPack_ThousandSeededPacksAtUnevenPrice_TotalCostBasisEqualsTotalSpent()
        {
            const long unevenPriceCents = 424;
            const int packCount = 1_000;
            var inventory = new InventoryService();
            var opener = new PackOpener(TestContent.StartingPack(), TestContent.SetAPool(), new SeededRng(11));

            for (int i = 0; i < packCount; i++)
            {
                inventory.AddPack(opener.Open(), unevenPriceCents);
            }

            Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(unevenPriceCents * packCount));
            Assert.That(inventory.Stacks.Sum(stack => stack.Count), Is.EqualTo(packCount * 5));
        }

        [Test]
        public void Remove_OneOfSeveral_ReducesCountAndCostBasisByAverageCost()
        {
            var inventory = new InventoryService();
            inventory.Add(s_rare, 100);
            inventory.Add(s_rare, 50);

            long removed = inventory.Remove(s_rare.Id, RarityTier.Rare);

            Assert.That(removed, Is.EqualTo(75));
            Assert.That(inventory.Stacks[0].Count, Is.EqualTo(1));
            Assert.That(inventory.Stacks[0].CostBasisCents, Is.EqualTo(75));
        }

        [Test]
        public void Remove_LastCopy_RemovesStackAndReturnsRemainingBasis()
        {
            var inventory = new InventoryService();
            inventory.Add(s_rare, 85);

            long removed = inventory.Remove(s_rare.Id, RarityTier.Rare);

            Assert.That(removed, Is.EqualTo(85));
            Assert.That(inventory.Stacks, Is.Empty);
        }

        [Test]
        public void Remove_AllCopiesOneByOne_BasisReachesExactlyZero()
        {
            var inventory = new InventoryService();
            inventory.Add(s_rare, 100);
            inventory.Add(s_rare, 0);
            inventory.Add(s_rare, 0);
            long totalRemoved = 0;

            for (int i = 0; i < 3; i++)
            {
                totalRemoved += inventory.Remove(s_rare.Id, RarityTier.Rare);
            }

            Assert.That(totalRemoved, Is.EqualTo(100));
            Assert.That(inventory.Stacks, Is.Empty);
        }

        [Test]
        public void Remove_MoreThanOwned_Throws()
        {
            var inventory = new InventoryService();
            inventory.Add(s_rare, 10);

            Assert.Throws<InvalidOperationException>(() => inventory.Remove(s_rare.Id, RarityTier.Rare, count: 2));
            Assert.That(inventory.CountOf(s_rare.Id, RarityTier.Rare), Is.EqualTo(1), "A failed removal must not change the stack.");
        }

        [Test]
        public void Clear_WithStacks_EmptiesInventory()
        {
            var inventory = new InventoryService();
            inventory.Add(s_rare, 10);
            inventory.Add(s_common, 10);

            inventory.Clear();

            Assert.That(inventory.Stacks, Is.Empty);
            Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(0));
        }

        private static OpenedPack PackOf(params Card[] cards) => new OpenedPack("test-pack", cards);
    }
}
