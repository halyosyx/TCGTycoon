using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.UI;
using NUnit.Framework;

namespace Game.Unity.Tests.UI
{
    public sealed class BinderReadModelTests
    {
        private const string SetA = "SetA";
        private const string SetB = "SetB";

        private static readonly Card s_aCommon = CreateCard("SetA_Common_01", "Zenkin", SetA, RarityTier.Common);
        private static readonly Card s_aUncommon = CreateCard("SetA_Uncommon_01", "Silmir", SetA, RarityTier.Uncommon);
        private static readonly Card s_aRare2 = CreateCard("SetA_Rare_02", "Rynorth", SetA, RarityTier.Rare);
        private static readonly Card s_aRare1 = CreateCard("SetA_Rare_01", "Myraeth", SetA, RarityTier.Rare);
        private static readonly Card s_aHolo = CreateCard("SetA_Holo_01", "Coralin", SetA, RarityTier.Holographic);
        private static readonly Card s_aSpecial = CreateCard("SetA_SpecialArt_01", "Braolin", SetA, RarityTier.SpecialIllustration);
        private static readonly Card s_bCommon = CreateCard("SetB_Common_01", "Ember", SetB, RarityTier.Common);
        private static readonly Card s_bFullArt = CreateCard("SetB_FullArt_01", "Titan", SetB, RarityTier.FullArt);

        private InventoryService _inventory;
        private InventoryBinderReadModel _binder;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryService();
            var pool = new CardPool(new[] { s_aCommon, s_aUncommon, s_aRare2, s_aRare1, s_aHolo, s_aSpecial, s_bCommon, s_bFullArt });
            _binder = new InventoryBinderReadModel(_inventory, pool, SetA, SetB);
        }

        [TearDown]
        public void TearDown() => _binder.Dispose();

        [Test]
        public void Tabs_Always_SetASetBSealedBulkInOrder()
        {
            IReadOnlyList<BinderTabInfo> tabs = _binder.Tabs;

            Assert.That(tabs.Count, Is.EqualTo(4));
            Assert.That(tabs[0].Tab, Is.EqualTo(BinderTab.SetA));
            Assert.That(tabs[0].Title, Is.EqualTo("Set A"));
            Assert.That(tabs[1].Title, Is.EqualTo("Set B"));
            Assert.That(tabs[2].Title, Is.EqualTo("Sealed"));
            Assert.That(tabs[3].Title, Is.EqualTo("Bulk"));
        }

        [Test]
        public void GetEntries_EmptyInventory_EveryTabEmptyWithZeroCount()
        {
            foreach (BinderTabInfo tab in _binder.Tabs)
            {
                Assert.That(tab.Count, Is.EqualTo(0), tab.Title);
                Assert.That(_binder.GetEntries(tab.Tab), Is.Empty, tab.Title);
            }
        }

        [Test]
        public void GetEntries_SetA_OnlyRareAndAboveOfSetA()
        {
            Own(s_aCommon, s_aUncommon, s_aRare1, s_bFullArt);

            IReadOnlyList<BinderEntry> entries = _binder.GetEntries(BinderTab.SetA);

            Assert.That(Ids(entries), Is.EqualTo(new[] { "SetA_Rare_01" }));
        }

        [Test]
        public void GetEntries_SetB_OnlyRareAndAboveOfSetB()
        {
            Own(s_bCommon, s_bFullArt, s_aHolo);

            Assert.That(Ids(_binder.GetEntries(BinderTab.SetB)), Is.EqualTo(new[] { "SetB_FullArt_01" }));
        }

        [Test]
        public void GetEntries_SetBWithNoCardsOwned_IsEmptyWithZeroCount()
        {
            Own(s_aRare1, s_aCommon);

            Assert.That(_binder.GetEntries(BinderTab.SetB), Is.Empty);
            Assert.That(CountOf(BinderTab.SetB), Is.EqualTo(0));
        }

        [Test]
        public void GetEntries_Bulk_CommonsAndUncommonsFromBothSets()
        {
            Own(s_aCommon, s_aUncommon, s_bCommon, s_aRare1, s_bFullArt);

            Assert.That(Ids(_binder.GetEntries(BinderTab.Bulk)), Is.EqualTo(new[] { "SetA_Uncommon_01", "SetA_Common_01", "SetB_Common_01" }));
        }

        [Test]
        public void GetEntries_Sealed_EmptyUntilSealedProductsExist()
        {
            Own(s_aRare1, s_aCommon);

            Assert.That(_binder.GetEntries(BinderTab.Sealed), Is.Empty);
            Assert.That(CountOf(BinderTab.Sealed), Is.EqualTo(0));
        }

        [Test]
        public void GetEntries_SetA_OrdersTierHighToLowThenCardId()
        {
            Own(s_aRare2, s_aHolo, s_aRare1, s_aSpecial);

            Assert.That(
                Ids(_binder.GetEntries(BinderTab.SetA)),
                Is.EqualTo(new[] { "SetA_SpecialArt_01", "SetA_Holo_01", "SetA_Rare_01", "SetA_Rare_02" }));
        }

        [Test]
        public void GetEntries_CopiesOfOneCard_OneEntryWithQuantity()
        {
            Own(s_aRare1, s_aRare1, s_aRare1);

            IReadOnlyList<BinderEntry> entries = _binder.GetEntries(BinderTab.SetA);

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Copies, Is.EqualTo(3));
            Assert.That(entries[0].DisplayName, Is.EqualTo("Myraeth"));
            Assert.That(entries[0].Tier, Is.EqualTo(3));
        }

        [Test]
        public void Tabs_Counts_AreDistinctItemsPerTab()
        {
            Own(s_aRare1, s_aRare1, s_aHolo, s_aCommon, s_aCommon, s_bCommon, s_bFullArt);

            Assert.That(CountOf(BinderTab.SetA), Is.EqualTo(2));
            Assert.That(CountOf(BinderTab.SetB), Is.EqualTo(1));
            Assert.That(CountOf(BinderTab.Sealed), Is.EqualTo(0));
            Assert.That(CountOf(BinderTab.Bulk), Is.EqualTo(2));
        }

        [Test]
        public void Changed_InventoryGainsCard_RaisedOnceAndEntriesRefresh()
        {
            Own(s_aRare1);
            _binder.GetEntries(BinderTab.SetA);
            int raised = 0;
            _binder.Changed += () => raised++;

            Own(s_aHolo);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(Ids(_binder.GetEntries(BinderTab.SetA)), Is.EqualTo(new[] { "SetA_Holo_01", "SetA_Rare_01" }));
            Assert.That(CountOf(BinderTab.SetA), Is.EqualTo(2));
        }

        [Test]
        public void Dispose_AfterwardsInventoryChanges_NotRaised()
        {
            int raised = 0;
            _binder.Changed += () => raised++;

            _binder.Dispose();
            Own(s_aRare1);

            Assert.That(raised, Is.EqualTo(0));
        }

        private void Own(params Card[] cards)
        {
            foreach (Card card in cards)
            {
                _inventory.Add(card, card.ValueCents);
            }
        }

        private int CountOf(BinderTab tab)
        {
            foreach (BinderTabInfo info in _binder.Tabs)
            {
                if (info.Tab == tab)
                {
                    return info.Count;
                }
            }

            return -1;
        }

        private static List<string> Ids(IReadOnlyList<BinderEntry> entries)
        {
            var ids = new List<string>();
            foreach (BinderEntry entry in entries)
            {
                ids.Add(entry.ItemId);
            }

            return ids;
        }

        private static Card CreateCard(string id, string name, string setId, RarityTier tier)
        {
            return new Card(id, name, setId, tier, 100);
        }
    }
}
