using System;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Inventory
{
    public sealed class InventorySealedTests
    {
        private const string PackId = "SetA_Pack";
        private const string OtherPackId = "SetB_Pack";

        private static readonly Card s_common = TestContent.CreateCard("RC_C_001", RarityTier.Common, 5);
        private static readonly Card s_holo = TestContent.CreateCard("RC_HFA_01", RarityTier.HoloFullArt, 240);

        [Test]
        public void AddSealed_Count_OneStackWithCountAndTotalCost()
        {
            var inventory = new InventoryService();

            inventory.AddSealed(PackId, 12, 500);

            Assert.That(inventory.SealedStacks.Count, Is.EqualTo(1));
            Assert.That(inventory.SealedStacks[0].ProductId, Is.EqualTo(PackId));
            Assert.That(inventory.SealedStacks[0].Count, Is.EqualTo(12));
            Assert.That(inventory.SealedStacks[0].CostBasisCents, Is.EqualTo(6_000));
            Assert.That(inventory.CountOfSealed(PackId), Is.EqualTo(12));
            Assert.That(inventory.Stacks, Is.Empty, "Sealed products aren't singles.");
        }

        [Test]
        public void AddSealed_TwiceSameProduct_OneStack()
        {
            var inventory = new InventoryService();

            inventory.AddSealed(PackId, 2, 500);
            inventory.AddSealed(PackId, 3, 450);

            Assert.That(inventory.SealedStacks.Count, Is.EqualTo(1));
            Assert.That(inventory.CountOfSealed(PackId), Is.EqualTo(5));
            Assert.That(inventory.SealedStacks[0].CostBasisCents, Is.EqualTo(2_350));
        }

        [Test]
        public void AddSealed_DifferentProducts_SeparateStacks()
        {
            var inventory = new InventoryService();

            inventory.AddSealed(PackId, 1, 500);
            inventory.AddSealed(OtherPackId, 1, 900);

            Assert.That(inventory.SealedStacks.Count, Is.EqualTo(2));
            Assert.That(inventory.CountOfSealed(OtherPackId), Is.EqualTo(1));
            Assert.That(inventory.CountOfSealed("unknown"), Is.EqualTo(0));
        }

        [Test]
        public void AddSealed_RaisesChangedOnce()
        {
            var inventory = new InventoryService();
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.AddSealed(PackId, 12, 500);

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void AddSealed_BadArguments_ThrowWithoutChange()
        {
            var inventory = new InventoryService();

            Assert.Throws<ArgumentException>(() => inventory.AddSealed("", 1, 500));
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.AddSealed(PackId, 0, 500));
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.AddSealed(PackId, 1, -1));
            Assert.That(inventory.SealedStacks, Is.Empty);
        }

        [Test]
        public void RemoveSealed_UnevenCost_AverageThenLastTakesRemainder()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);
            inventory.AddSealed(PackId, 2, 450);   // 1 400 over 3

            long first = inventory.RemoveSealed(PackId);
            long second = inventory.RemoveSealed(PackId);
            long last = inventory.RemoveSealed(PackId);

            Assert.That(first, Is.EqualTo(466));
            Assert.That(second, Is.EqualTo(467));
            Assert.That(last, Is.EqualTo(467));
            Assert.That(first + second + last, Is.EqualTo(1_400));
            Assert.That(inventory.SealedStacks, Is.Empty);
        }

        [Test]
        public void RemoveSealed_MoreThanOwned_ThrowsWithoutChange()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);

            Assert.Throws<InvalidOperationException>(() => inventory.RemoveSealed(PackId, 2));
            Assert.Throws<InvalidOperationException>(() => inventory.RemoveSealed(OtherPackId));
            Assert.That(inventory.CountOfSealed(PackId), Is.EqualTo(1));
        }

        [Test]
        public void OpenSealed_OnePack_SwapsOneSealedForItsCardsAtThePaidCost()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 2, 503);
            var pack = new OpenedPack("test-pack", new[] { s_common, s_common, s_common, s_holo });

            long paid = inventory.OpenSealed(PackId, pack);

            Assert.That(paid, Is.EqualTo(503));
            Assert.That(inventory.CountOfSealed(PackId), Is.EqualTo(1));
            Assert.That(inventory.CountOf(s_common.Id, RarityTier.Common), Is.EqualTo(3));
            Assert.That(inventory.CountOf(s_holo.Id, RarityTier.HoloFullArt), Is.EqualTo(1));
            // 503 over 4 cards: 126, 126, 126, 125. The singles carry exactly what the pack cost.
            Assert.That(inventory.Stacks[0].CostBasisCents + inventory.Stacks[1].CostBasisCents, Is.EqualTo(503));
            Assert.That(inventory.Stacks[1].CostBasisCents, Is.EqualTo(125));
        }

        [Test]
        public void OpenSealed_RaisesChangedOnceWithCardsInAndPackOut()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);
            int raised = 0;
            int sealedWhenRaised = -1;
            int cardsWhenRaised = -1;
            inventory.Changed += () =>
            {
                raised++;
                sealedWhenRaised = inventory.CountOfSealed(PackId);
                cardsWhenRaised = inventory.CountOf(s_common.Id, RarityTier.Common);
            };

            inventory.OpenSealed(PackId, new OpenedPack("test-pack", new[] { s_common, s_common }));

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(sealedWhenRaised, Is.EqualTo(0));
            Assert.That(cardsWhenRaised, Is.EqualTo(2));
        }

        [Test]
        public void OpenSealed_NotOwned_ThrowsWithoutAddingCards()
        {
            var inventory = new InventoryService();

            Assert.Throws<InvalidOperationException>(() => inventory.OpenSealed(PackId, new OpenedPack("test-pack", new[] { s_common })));
            Assert.That(inventory.Stacks, Is.Empty);
        }

        [Test]
        public void TotalCostBasisCents_CountsSealedAndSingles_UnchangedByOpening()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 3, 500);
            inventory.Add(s_holo, 240);
            Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(1_740));

            inventory.OpenSealed(PackId, new OpenedPack("test-pack", new[] { s_common, s_common, s_common }));

            Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(1_740));
        }

        [Test]
        public void Clear_WithSealed_EmptiesBoth()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 3, 500);

            inventory.Clear();

            Assert.That(inventory.SealedStacks, Is.Empty);
        }
    }
}
