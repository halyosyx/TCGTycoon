using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Inventory
{
    /// <summary>
    /// F2c: placing the held card stack into the display case in one go (<see cref="InventoryService.MoveAllCards"/>).
    /// Whatever doesn't fit stays in the hand; nothing is dropped and nothing is created.
    /// </summary>
    public sealed class DisplayCasePlacementTests
    {
        private const string PackId = "SetA_Pack";

        private static readonly Card s_common = TestContent.CreateCard("RC_C_001", RarityTier.Common, 5);
        private static readonly Card s_uncommon = TestContent.CreateCard("RC_U_001", RarityTier.Uncommon, 15);
        private static readonly Card s_holo = TestContent.CreateCard("RC_HFA_01", RarityTier.HoloFullArt, 240);
        private static readonly Card s_filler = TestContent.CreateCard("RC_C_002", RarityTier.Common, 5);

        [Test]
        public void MoveAllCards_TenHeldIntoEmptyCase_TenInCaseNoneHeld()
        {
            InventoryService inventory = CreateHolding(10);

            CardsMoveResult result = inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            Assert.That(result.Moved, Is.EqualTo(10));
            Assert.That(result.Left, Is.EqualTo(0));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Failure, Is.EqualTo(MoveFailure.None));
            Assert.That(inventory.CountIn(ItemLocation.DisplayCase), Is.EqualTo(10));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(0));
        }

        [Test]
        public void MoveAllCards_FullCase_FailsCapacityFullAndCardsStayHeld()
        {
            InventoryService inventory = CreateHolding(10);
            FillCase(inventory, 25);
            int events = 0;
            inventory.Changed += () => events++;

            CardsMoveResult result = inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            Assert.That(result.Moved, Is.EqualTo(0));
            Assert.That(result.Left, Is.EqualTo(10));
            Assert.That(result.Failure, Is.EqualTo(MoveFailure.CapacityFull));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(10));
            Assert.That(inventory.CountIn(ItemLocation.DisplayCase), Is.EqualTo(25));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void MoveAllCards_PartialFit_MovesExactlyTheRoomLeft()
        {
            InventoryService inventory = CreateHolding(10);
            FillCase(inventory, 20);

            CardsMoveResult result = inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            Assert.That(result.Moved, Is.EqualTo(5));
            Assert.That(result.Left, Is.EqualTo(5));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Failure, Is.EqualTo(MoveFailure.CapacityFull));
            Assert.That(inventory.CountIn(ItemLocation.DisplayCase), Is.EqualTo(25));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(5));
        }

        [Test]
        public void PlaceSealed_InTheCase_IsRejected()
        {
            var inventory = new InventoryService();
            inventory.AddSealed(PackId, 1, 500);
            inventory.Move(ItemRef.Sealed(PackId), ItemLocation.Binder, ItemLocation.Held);

            MoveResult single = inventory.Move(ItemRef.Sealed(PackId), ItemLocation.Held, ItemLocation.DisplayCase);
            CardsMoveResult all = inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            Assert.That(single.Failure, Is.EqualTo(MoveFailure.NotAllowedThere));
            Assert.That(all.Moved, Is.EqualTo(0));
            Assert.That(inventory.CountOfSealed(PackId, ItemLocation.Held), Is.EqualTo(1));
            Assert.That(inventory.CountIn(ItemLocation.DisplayCase), Is.EqualTo(0));
        }

        [Test]
        public void MoveAllCards_MixedCards_KeepsTotalsAndCostBasis()
        {
            var inventory = new InventoryService();
            AddAndHold(inventory, s_common, 4);
            AddAndHold(inventory, s_uncommon, 3);
            AddAndHold(inventory, s_holo, 2);
            int items = inventory.TotalItemCount;
            long basis = inventory.TotalCostBasisCents;

            inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            Assert.That(inventory.CountOf(s_common.Id, s_common.Tier, ItemLocation.DisplayCase), Is.EqualTo(4));
            Assert.That(inventory.CountOf(s_uncommon.Id, s_uncommon.Tier, ItemLocation.DisplayCase), Is.EqualTo(3));
            Assert.That(inventory.CountOf(s_holo.Id, s_holo.Tier, ItemLocation.DisplayCase), Is.EqualTo(2));
            Assert.That(inventory.TotalItemCount, Is.EqualTo(items));
            Assert.That(inventory.TotalCostBasisCents, Is.EqualTo(basis));
        }

        [Test]
        public void MoveAllCards_Success_RaisesChangedOnce()
        {
            var inventory = new InventoryService();
            AddAndHold(inventory, s_common, 2);
            AddAndHold(inventory, s_holo, 2);
            int events = 0;
            inventory.Changed += () => events++;

            inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void MoveAllCards_NothingHeld_MovesNothingAndRaisesNothing()
        {
            var inventory = new InventoryService();
            inventory.Add(s_common, 5);
            int events = 0;
            inventory.Changed += () => events++;

            CardsMoveResult result = inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            Assert.That(result.Moved, Is.EqualTo(0));
            Assert.That(result.Left, Is.EqualTo(0));
            Assert.That(result.Failure, Is.EqualTo(MoveFailure.NotOwned));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void MoveAllCards_SameLocation_IsInvalid()
        {
            InventoryService inventory = CreateHolding(3);

            CardsMoveResult result = inventory.MoveAllCards(ItemLocation.Held, ItemLocation.Held);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.InvalidMove));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(3));
        }

        [Test]
        public void MoveAllCards_ToPlaced_IsNotAllowedAndCardsStay()
        {
            InventoryService inventory = CreateHolding(3);

            CardsMoveResult result = inventory.MoveAllCards(ItemLocation.Held, ItemLocation.Placed);

            Assert.That(result.Failure, Is.EqualTo(MoveFailure.NotAllowedThere));
            Assert.That(result.Left, Is.EqualTo(3));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(3));
        }

        [Test]
        public void MoveAllCards_CaseBackToHand_TakesOnlyWhatTheHandHoldsRoomFor()
        {
            var inventory = new InventoryService();
            FillCase(inventory, 12);

            CardsMoveResult result = inventory.MoveAllCards(ItemLocation.DisplayCase, ItemLocation.Held);

            Assert.That(result.Moved, Is.EqualTo(10));
            Assert.That(result.Left, Is.EqualTo(2));
            Assert.That(inventory.CountIn(ItemLocation.Held), Is.EqualTo(10));
        }

        [Test]
        public void IsForSale_CardsPlacedInTheCase_ReportForSaleAndHeldOnesDoNot()
        {
            InventoryService inventory = CreateHolding(10);
            FillCase(inventory, 20);

            inventory.MoveAllCards(ItemLocation.Held, ItemLocation.DisplayCase);

            foreach (InventoryStack stack in inventory.Stacks)
            {
                Assert.That(stack.IsForSale, Is.EqualTo(stack.Location == ItemLocation.DisplayCase), $"{stack.CardId} in {stack.Location}");
            }
        }

        private static InventoryService CreateHolding(int count)
        {
            var inventory = new InventoryService();
            AddAndHold(inventory, s_common, count);
            return inventory;
        }

        private static void AddAndHold(InventoryService inventory, Card card, int count)
        {
            for (int i = 0; i < count; i++)
            {
                inventory.Add(card, card.ValueCents);
            }

            Assert.That(inventory.Move(ItemRef.Card(card.Id, card.Tier), ItemLocation.Binder, ItemLocation.Held, count).IsSuccess, Is.True, "Setup: cards in the hand.");
        }

        // Straight from the binder, so the hand is untouched.
        private static void FillCase(InventoryService inventory, int count)
        {
            for (int i = 0; i < count; i++)
            {
                inventory.Add(s_filler, s_filler.ValueCents);
            }

            Assert.That(inventory.Move(ItemRef.Card(s_filler.Id, s_filler.Tier), ItemLocation.Binder, ItemLocation.DisplayCase, count).IsSuccess, Is.True, "Setup: cards in the case.");
        }
    }
}
