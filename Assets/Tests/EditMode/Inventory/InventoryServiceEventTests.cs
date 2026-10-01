using System;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Inventory
{
    public sealed class InventoryServiceEventTests
    {
        private static readonly Card s_holo = TestContent.CreateCard("RC_HFA_01", RarityTier.HoloFullArt, 240);
        private static readonly Card s_common = TestContent.CreateCard("RC_C_001", RarityTier.Common, 5);

        [Test]
        public void Add_OneCard_RaisesChangedOnce()
        {
            var inventory = new InventoryService();
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.Add(s_holo, 60);

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void AddPack_FiveCards_RaisesChangedOnceAfterEveryCardIsIn()
        {
            var inventory = new InventoryService();
            int raised = 0;
            int cardsWhenRaised = -1;
            inventory.Changed += () =>
            {
                raised++;
                cardsWhenRaised = inventory.CountOf(s_holo.Id, RarityTier.HoloFullArt) + inventory.CountOf(s_common.Id, RarityTier.Common);
            };
            var pack = new OpenedPack("test-pack", new[] { s_common, s_common, s_common, s_common, s_holo });

            inventory.AddPack(pack, 425);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(cardsWhenRaised, Is.EqualTo(5));
        }

        [Test]
        public void AddPack_EmptyPack_DoesNotRaiseChanged()
        {
            var inventory = new InventoryService();
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.AddPack(new OpenedPack("test-pack", Array.Empty<Card>()), 425);

            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Remove_OwnedCopy_RaisesChangedOnce()
        {
            var inventory = new InventoryService();
            inventory.Add(s_holo, 60);
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.Remove(s_holo.Id, RarityTier.HoloFullArt);

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Remove_MoreThanOwned_ThrowsWithoutRaisingChanged()
        {
            var inventory = new InventoryService();
            int raised = 0;
            inventory.Changed += () => raised++;

            Assert.Throws<InvalidOperationException>(() => inventory.Remove(s_holo.Id, RarityTier.HoloFullArt));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Clear_WithCards_RaisesChangedOnce()
        {
            var inventory = new InventoryService();
            inventory.Add(s_holo, 60);
            inventory.Add(s_common, 5);
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.Clear();

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Clear_AlreadyEmpty_DoesNotRaiseChanged()
        {
            var inventory = new InventoryService();
            int raised = 0;
            inventory.Changed += () => raised++;

            inventory.Clear();

            Assert.That(raised, Is.EqualTo(0));
        }
    }
}
