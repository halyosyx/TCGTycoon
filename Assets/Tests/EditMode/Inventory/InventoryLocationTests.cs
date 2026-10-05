using System.Collections.Generic;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Inventory
{
    public sealed class InventoryLocationTests
    {
        private const string PackId = "SetA_Pack";
        private const string OtherPackId = "SetB_Pack";

        private static readonly Card s_common = TestContent.CreateCard("RC_C_001", RarityTier.Common, 5);
        private static readonly Card s_holo = TestContent.CreateCard("RC_HFA_01", RarityTier.HoloFullArt, 240);
        private static readonly ItemRef s_commonRef = ItemRef.Card(s_common.Id, s_common.Tier);
        private static readonly ItemRef s_holoRef = ItemRef.Card(s_holo.Id, s_holo.Tier);
        private static readonly ItemRef s_packRef = ItemRef.Sealed(PackId);

        // --- Where new items land ---

        [Test]
        public void Add_NewCardsAndSealed_LandInBinder()
        {
            var inventory = new InventoryService();

            inventory.Add(s_common, 5);
            inventory.AddSealed(PackId, 2, 500);

            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Binder), Is.EqualTo(1));
            Assert.That(inventory.CountOfSealed(PackId, ItemLocation.Binder), Is.EqualTo(2));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(0));
        }

        // --- Total invariant ---

        [Test]
        public void Move_SeededRandomSequence_TotalItemCountAndCostBasisInvariant()
        {
            var inventory = new InventoryService();
            for (int i = 0; i < 20; i++) inventory.Add(s_common, 5);
            for (int i = 0; i < 8; i++) inventory.Add(s_holo, 240);
            inventory.AddSealed(PackId, 4, 500);
            inventory.AddSealed(OtherPackId, 3, 900);
            int total = inventory.TotalItemCount;
            long basis = inventory.TotalCostBasisCents;

            var rng = new SeededRng(4242);
            ItemRef[] items = { s_commonRef, s_holoRef, s_packRef, ItemRef.Sealed(OtherPackId), ItemRef.Card("RC_C_999", RarityTier.Common) };
            ItemLocation[] places = { ItemLocation.Binder, ItemLocation.Held, ItemLocation.DisplayCase, ItemLocation.Placed };
            int successes = 0;
            for (int step = 0; step < 500; step++)
            {
                MoveResult result = inventory.Move(items[rng.NextInt(items.Length)], places[rng.NextInt(places.Length)], places[rng.NextInt(places.Length)], 1 + rng.NextInt(3));
                if (result.IsSuccess) successes++;

                Assert.That(inventory.TotalItemCount, Is.EqualTo(total), $"step {step}");
                Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(basis), $"step {step}");
            }

            Assert.That(successes, Is.GreaterThan(50), "The sequence should exercise real moves, not only failures.");
        }

        // --- Capacity ---

        [Test]
        public void Move_EleventhCardToHeld_FailsCapacityFullUnmoved()
        {
            var inventory = new InventoryService();
            for (int i = 0; i < 11; i++) inventory.Add(s_common, 5);
            Assert.That(inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held, 10).IsSuccess, Is.True);

            MoveResult result = inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(MoveFailure.CapacityFull));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(10));
            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Binder), Is.EqualTo(1));
        }

        [Test]
        public void Move_TwentySixthCardToDisplayCase_FailsCapacityFullUnmoved()
        {
            var inventory = new InventoryService();
            for (int i = 0; i < 26; i++) inventory.Add(s_common, 5);
            Assert.That(inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.DisplayCase, 25).IsSuccess, Is.True);

            MoveResult result = inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.DisplayCase);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.CapacityFull));
            Assert.That(inventory.CountIn(ItemLocation.DisplayCase), Is.EqualTo(25));
            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Binder), Is.EqualTo(1));
        }

        [Test]
        public void Move_MoreThanFitsInOneGo_FailsWithoutMovingAny()
        {
            var inventory = new InventoryService();
            for (int i = 0; i < 12; i++) inventory.Add(s_common, 5);

            MoveResult result = inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held, 11);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.CapacityFull));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(0), "All or nothing.");
        }

        // --- What may go where ---

        [Test]
        public void Move_SealedToDisplayCase_FailsNotAllowedThere()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);

            MoveResult result = inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.DisplayCase);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.NotAllowedThere));
            Assert.That(inventory.CountOfSealed(PackId, ItemLocation.Binder), Is.EqualTo(1));
        }

        [Test]
        public void Move_CardToPlaced_FailsNotAllowedThere()
        {
            // Single cards never exist loose in the world: a pulled card can't be lost.
            var inventory = new InventoryService();
            inventory.Add(s_common, 5);

            Assert.That(inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Placed).Failure, Is.EqualTo(MoveFailure.NotAllowedThere));
            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Binder), Is.EqualTo(1));
        }

        [Test]
        public void Move_CardToHeldWhilePackHeld_FailsHeldMixed()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);
            inventory.Add(s_common, 5);
            inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.Held);

            MoveResult result = inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.HeldMixed));
            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Held), Is.EqualTo(0));
        }

        [Test]
        public void Move_PackToHeldWhileCardsHeld_FailsHeldMixed()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);
            inventory.Add(s_common, 5);
            inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held);

            Assert.That(inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.Held).Failure, Is.EqualTo(MoveFailure.HeldMixed));
        }

        [Test]
        public void Move_SecondPackToHeld_FailsHeldMixed()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 2, 500);
            inventory.AddSealed(OtherPackId, 1, 900);
            inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.Held);

            Assert.That(inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.Held).Failure, Is.EqualTo(MoveFailure.HeldMixed));
            Assert.That(inventory.Move(ItemRef.Sealed(OtherPackId), ItemLocation.Binder, ItemLocation.Held).Failure, Is.EqualTo(MoveFailure.HeldMixed));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(1));
        }

        [Test]
        public void Move_PackHeldThenPlacedThenHeld_Succeeds()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);

            Assert.That(inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.Held).IsSuccess, Is.True);
            Assert.That(inventory.Move(s_packRef, ItemLocation.Held, ItemLocation.Placed).IsSuccess, Is.True);
            Assert.That(inventory.Move(s_packRef, ItemLocation.Placed, ItemLocation.Held).IsSuccess, Is.True);
            Assert.That(inventory.CountOfSealed(PackId, ItemLocation.Held), Is.EqualTo(1));
        }

        // --- For sale is derived ---

        [Test]
        public void IsForSale_TrueExactlyForDisplayCaseStacks()
        {
            var inventory = new InventoryService();
            for (int i = 0; i < 4; i++) inventory.Add(s_common, 5);
            inventory.Add(s_holo, 240);
            inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.DisplayCase, 2);
            inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held);
            inventory.Move(s_holoRef, ItemLocation.Binder, ItemLocation.DisplayCase);

            foreach (InventoryStack stack in inventory.Stacks)
            {
                Assert.That(stack.IsForSale, Is.EqualTo(stack.Location == ItemLocation.DisplayCase), $"{stack.CardId} in {stack.Location}");
            }

            Assert.That(inventory.CountIn(ItemLocation.DisplayCase), Is.EqualTo(3));
        }

        // --- Round trip ---

        [Test]
        public void Move_BinderHeldBinder_CountsAndCostBasisUnchanged()
        {
            var inventory = new InventoryService();
            inventory.Add(s_common, 3);
            inventory.Add(s_common, 4);
            inventory.Add(s_common, 4);   // 3 copies, 11 cents: uneven average
            long before = inventory.TotalCostBasisCents;

            inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held, 2);
            inventory.Move(s_commonRef, ItemLocation.Held, ItemLocation.Binder, 2);

            Assert.That(inventory.Stacks.Count, Is.EqualTo(1), "The emptied Held stack is gone, the copies merge back.");
            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Binder), Is.EqualTo(3));
            Assert.That(inventory.Stacks[0].CostBasisCents, Is.EqualTo(11));
            Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(before));
        }

        // --- Never creates or loses ---

        [Test]
        public void Move_UnownedItem_FailsNotOwnedNothingCreated()
        {
            var inventory = new InventoryService();
            inventory.Add(s_common, 5);

            MoveResult result = inventory.Move(s_holoRef, ItemLocation.Binder, ItemLocation.Held);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.NotOwned));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(1));
            Assert.That(inventory.CountOf(s_holo.Id, s_holo.Tier), Is.EqualTo(0));
        }

        [Test]
        public void Move_MoreThanOwnedAtSource_FailsNotOwned()
        {
            var inventory = new InventoryService();
            inventory.Add(s_common, 5);
            inventory.Add(s_common, 5);
            inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held);

            MoveResult result = inventory.Move(s_commonRef, ItemLocation.Held, ItemLocation.DisplayCase, 2);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.NotOwned), "Only one copy is in Held, even though two are owned.");
            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Held), Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Move_ZeroOrNegativeCount_FailsInvalidMove(int count)
        {
            var inventory = new InventoryService();
            inventory.Add(s_common, 5);

            Assert.That(inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held, count).Failure, Is.EqualTo(MoveFailure.InvalidMove));
        }

        [Test]
        public void Move_SameLocation_FailsInvalidMove()
        {
            var inventory = new InventoryService();
            inventory.Add(s_common, 5);

            Assert.That(inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Binder).Failure, Is.EqualTo(MoveFailure.InvalidMove));
        }

        // --- Events ---

        [Test]
        public void Move_Success_RaisesChangedOnce()
        {
            var inventory = new InventoryService();
            for (int i = 0; i < 3; i++) inventory.Add(s_common, 5);
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held, 3);

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Move_Failure_RaisesNothing()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.DisplayCase);
            inventory.Move(s_holoRef, ItemLocation.Binder, ItemLocation.Held);

            Assert.That(raised, Is.EqualTo(0));
        }

        // --- Opening and capacities ---

        [Test]
        public void OpenSealed_FromHeld_TakesTheHeldPackAndCardsLandInBinder()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 3, 500);
            inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.Held);

            long paid = inventory.OpenSealed(PackId, new OpenedPack("pack", new[] { s_common, s_holo }), ItemLocation.Held);

            Assert.That(paid, Is.EqualTo(500));
            Assert.That(inventory.CountOfSealed(PackId, ItemLocation.Held), Is.EqualTo(0));
            Assert.That(inventory.CountOfSealed(PackId, ItemLocation.Binder), Is.EqualTo(2));
            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.Binder), Is.EqualTo(1));
        }

        [Test]
        public void Capacities_Custom_AreEnforced()
        {
            var inventory = new InventoryService(new InventoryState(), new LocationCapacities(heldCards: 2, heldSealed: 1, displayCase: 1));
            for (int i = 0; i < 3; i++) inventory.Add(s_common, 5);

            Assert.That(inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.Held, 3).Failure, Is.EqualTo(MoveFailure.CapacityFull));
            Assert.That(inventory.Move(s_commonRef, ItemLocation.Binder, ItemLocation.DisplayCase, 2).Failure, Is.EqualTo(MoveFailure.CapacityFull));
        }

        [Test]
        public void CountIn_CountsSinglesAndSealedPerLocation()
        {
            var inventory = new InventoryService();
            inventory.Add(s_common, 5);
            inventory.Add(s_holo, 240);
            inventory.AddSealed(PackId, 2, 500);
            inventory.Move(s_packRef, ItemLocation.Binder, ItemLocation.Placed);

            Assert.That(inventory.CountIn(ItemLocation.Binder), Is.EqualTo(3));
            Assert.That(inventory.CountIn(ItemLocation.Placed), Is.EqualTo(1));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(4));
        }
    }
}
