using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.Hands;
using NUnit.Framework;

namespace Game.Unity.Tests.Hands
{
    public sealed class HandCardsTests
    {
        private static readonly Card s_common = new Card("RC_C_001", "Common one", "SetA", RarityTier.Common, 5);
        private static readonly Card s_holo = new Card("RC_HFA_01", "Holo one", "SetA", RarityTier.HoloFullArt, 240);
        private static readonly Card s_other = new Card("RC_U_001", "Uncommon one", "SetA", RarityTier.Uncommon, 15);

        [Test]
        public void TryFindLastHeld_SeveralHeld_ReturnsTheMostRecentlyStartedHeldStack()
        {
            var inventory = new InventoryService();
            AddAndMove(inventory, s_common, ItemLocation.Held);
            AddAndMove(inventory, s_other, ItemLocation.DisplayCase);
            AddAndMove(inventory, s_holo, ItemLocation.Held);

            bool isFound = HandCards.TryFindLastHeld(inventory.Stacks, out ItemRef card);

            Assert.That(isFound, Is.True);
            Assert.That(card, Is.EqualTo(ItemRef.Card(s_holo.Id, s_holo.Tier)));
        }

        [Test]
        public void TryFindLastHeld_NothingHeld_ReturnsFalse()
        {
            var inventory = new InventoryService();
            AddAndMove(inventory, s_common, ItemLocation.Binder);
            AddAndMove(inventory, s_holo, ItemLocation.DisplayCase);

            Assert.That(HandCards.TryFindLastHeld(inventory.Stacks, out _), Is.False);
        }

        private static void AddAndMove(InventoryService inventory, Card card, ItemLocation location)
        {
            inventory.Add(card, card.ValueCents);
            if (location != ItemLocation.Binder)
            {
                Assert.That(inventory.Move(ItemRef.Card(card.Id, card.Tier), ItemLocation.Binder, location).IsSuccess, Is.True, "Setup");
            }
        }
    }
}
