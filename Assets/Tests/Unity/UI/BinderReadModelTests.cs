using System;
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
        private const string SetC = "SetC";
        private const int SetATab = 0;
        private const int SetBTab = 1;
        private const int SealedTab = 2;

        private static readonly Card s_aCommon = CreateCard("RC_C_001", "Zenkin", SetA, RarityTier.Common);
        private static readonly Card s_aUncommon = CreateCard("RC_U_001", "Silmir", SetA, RarityTier.Uncommon);
        private static readonly Card s_aUncommon2 = CreateCard("RC_U_002", "Coralin", SetA, RarityTier.Uncommon);
        private static readonly Card s_aHolo2 = CreateCard("RC_HFA_02", "Rynorth", SetA, RarityTier.HoloFullArt);
        private static readonly Card s_aHolo1 = CreateCard("RC_HFA_01", "Myraeth", SetA, RarityTier.HoloFullArt);
        private static readonly Card s_aSpecial = CreateCard("RC_SFAH_01", "Braolin", SetA, RarityTier.SpecialFullArtHolo);
        private static readonly Card s_bCommon = CreateCard("MO_C_001", "Ember", SetB, RarityTier.Common);
        private static readonly Card s_bHolo = CreateCard("MO_HFA_01", "Titan", SetB, RarityTier.HoloFullArt);
        private static readonly Card s_cHolo = CreateCard("XX_HFA_01", "Vesk", SetC, RarityTier.HoloFullArt);

        private static readonly CardPool s_allCards = new CardPool(new[] { s_aCommon, s_aUncommon, s_aUncommon2, s_aHolo2, s_aHolo1, s_aSpecial, s_bCommon, s_bHolo, s_cHolo });

        private InventoryService _inventory;
        private InventoryBinderReadModel _binder;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryService();
            _binder = CreateBinder(new BinderSet(SetA, "Set A"), new BinderSet(SetB, "Set B"));
        }

        [TearDown]
        public void TearDown() => _binder.Dispose();

        [Test]
        public void Tabs_TwoSets_OneTabPerSetInOrderThenSealed()
        {
            IReadOnlyList<BinderTabInfo> tabs = _binder.Tabs;

            Assert.That(tabs.Count, Is.EqualTo(3));
            AssertTab(tabs[SetATab], BinderTabKind.CardSet, SetA, "Set A");
            AssertTab(tabs[SetBTab], BinderTabKind.CardSet, SetB, "Set B");
            AssertTab(tabs[SealedTab], BinderTabKind.Sealed, InventoryBinderReadModel.SealedTabId, "Sealed");
        }

        [Test]
        public void Tabs_OneSet_SetThenSealed()
        {
            using (InventoryBinderReadModel binder = CreateBinder(new BinderSet(SetA, "Set A")))
            {
                Assert.That(binder.Tabs.Count, Is.EqualTo(2));
                AssertTab(binder.Tabs[0], BinderTabKind.CardSet, SetA, "Set A");
                AssertTab(binder.Tabs[1], BinderTabKind.Sealed, InventoryBinderReadModel.SealedTabId, "Sealed");
            }
        }

        [Test]
        public void Tabs_NoSets_OnlySealed()
        {
            using (InventoryBinderReadModel binder = CreateBinder())
            {
                Own(s_aHolo1);

                Assert.That(binder.Tabs.Count, Is.EqualTo(1));
                Assert.That(binder.Tabs[0].Kind, Is.EqualTo(BinderTabKind.Sealed));
                Assert.That(binder.GetEntries(0), Is.Empty);
            }
        }

        [Test]
        public void Tabs_RepeatedOrEmptySets_Skipped()
        {
            using (InventoryBinderReadModel binder = CreateBinder(new BinderSet(SetB, "Set B"), null, new BinderSet(SetB, "Again"), new BinderSet(SetA, "Set A")))
            {
                Assert.That(binder.Tabs.Count, Is.EqualTo(3));
                AssertTab(binder.Tabs[0], BinderTabKind.CardSet, SetB, "Set B");
                AssertTab(binder.Tabs[1], BinderTabKind.CardSet, SetA, "Set A");
            }
        }

        [Test]
        public void BinderSet_NoTitle_UsesSetId()
        {
            Assert.That(new BinderSet(SetC, null).Title, Is.EqualTo(SetC));
        }

        [Test]
        public void GetEntries_EmptyInventory_EveryTabEmptyWithZeroCount()
        {
            for (int i = 0; i < _binder.Tabs.Count; i++)
            {
                Assert.That(_binder.Tabs[i].Count, Is.EqualTo(0), _binder.Tabs[i].Title);
                Assert.That(_binder.GetEntries(i), Is.Empty, _binder.Tabs[i].Title);
            }
        }

        [Test]
        public void GetEntries_SetTab_EveryTierOfThatSetIncludingCommons()
        {
            Own(s_aCommon, s_aUncommon, s_aHolo1, s_bHolo);

            Assert.That(Ids(_binder.GetEntries(SetATab)), Is.EqualTo(new[] { "RC_HFA_01", "RC_U_001", "RC_C_001" }));
            Assert.That(Ids(_binder.GetEntries(SetBTab)), Is.EqualTo(new[] { "MO_HFA_01" }));
        }

        [Test]
        public void GetEntries_SetTab_RarestFirstDownToCommonThenCardId()
        {
            Own(s_aCommon, s_aHolo2, s_aUncommon, s_aUncommon2, s_aHolo1, s_aSpecial);

            Assert.That(
                Ids(_binder.GetEntries(SetATab)),
                Is.EqualTo(new[] { "RC_SFAH_01", "RC_HFA_01", "RC_HFA_02", "RC_U_001", "RC_U_002", "RC_C_001" }));
        }

        [Test]
        public void GetEntries_SetWithNoCardsOwned_IsEmptyWithZeroCount()
        {
            Own(s_aHolo1, s_aCommon);

            Assert.That(_binder.GetEntries(SetBTab), Is.Empty);
            Assert.That(_binder.Tabs[SetBTab].Count, Is.EqualTo(0));
        }

        [Test]
        public void GetEntries_CardOfSetWithoutTab_LeftOut()
        {
            Own(s_cHolo, s_aHolo1);

            Assert.That(Ids(_binder.GetEntries(SetATab)), Is.EqualTo(new[] { "RC_HFA_01" }));
            Assert.That(_binder.GetEntries(SetBTab), Is.Empty);
            Assert.That(_binder.GetEntries(SealedTab), Is.Empty);
        }

        [Test]
        public void GetEntries_Sealed_EmptyUntilSealedProductsExist()
        {
            Own(s_aHolo1, s_aCommon);

            Assert.That(_binder.GetEntries(SealedTab), Is.Empty);
            Assert.That(_binder.Tabs[SealedTab].Count, Is.EqualTo(0));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void GetEntries_IndexOutsideTabs_Throws(int tabIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _binder.GetEntries(tabIndex));
        }

        [Test]
        public void GetEntries_CopiesOfOneCard_OneEntryWithQuantity()
        {
            Own(s_aHolo1, s_aHolo1, s_aHolo1);

            IReadOnlyList<BinderEntry> entries = _binder.GetEntries(SetATab);

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Copies, Is.EqualTo(3));
            Assert.That(entries[0].DisplayName, Is.EqualTo("Myraeth"));
            Assert.That(entries[0].Tier, Is.EqualTo(3));
        }

        [Test]
        public void Tabs_Counts_AreDistinctItemsPerTab()
        {
            Own(s_aHolo1, s_aHolo1, s_aUncommon2, s_aCommon, s_aCommon, s_bCommon, s_bHolo);

            Assert.That(_binder.Tabs[SetATab].Count, Is.EqualTo(3));
            Assert.That(_binder.Tabs[SetBTab].Count, Is.EqualTo(2));
            Assert.That(_binder.Tabs[SealedTab].Count, Is.EqualTo(0));
        }

        [Test]
        public void Changed_InventoryGainsCard_RaisedOnceAndEntriesRefresh()
        {
            Own(s_aHolo1);
            _binder.GetEntries(SetATab);
            int raised = 0;
            _binder.Changed += () => raised++;

            Own(s_aUncommon2);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(Ids(_binder.GetEntries(SetATab)), Is.EqualTo(new[] { "RC_HFA_01", "RC_U_002" }));
            Assert.That(_binder.Tabs[SetATab].Count, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_AfterwardsInventoryChanges_NotRaised()
        {
            int raised = 0;
            _binder.Changed += () => raised++;

            _binder.Dispose();
            Own(s_aHolo1);

            Assert.That(raised, Is.EqualTo(0));
        }

        private InventoryBinderReadModel CreateBinder(params BinderSet[] sets)
        {
            return new InventoryBinderReadModel(_inventory, s_allCards, sets);
        }

        private void Own(params Card[] cards)
        {
            foreach (Card card in cards)
            {
                _inventory.Add(card, card.ValueCents);
            }
        }

        private static void AssertTab(BinderTabInfo tab, BinderTabKind kind, string id, string title)
        {
            Assert.That(tab.Kind, Is.EqualTo(kind));
            Assert.That(tab.Id, Is.EqualTo(id));
            Assert.That(tab.Title, Is.EqualTo(title));
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
