using System;
using System.Collections.Generic;
using Game.Unity.UI;
using NUnit.Framework;

namespace Game.Unity.Tests.UI
{
    public sealed class BinderPagingTests
    {
        [TestCase(0, 2, 1)]
        [TestCase(1, 2, 1)]
        [TestCase(9, 2, 1)]
        [TestCase(10, 2, 1)]
        [TestCase(18, 2, 1)]
        [TestCase(19, 4, 2)]
        public void PageAndSpreadCount_ItemCount_AlwaysWholeSpreads(int itemCount, int expectedPages, int expectedSpreads)
        {
            Assert.That(BinderPaging.PageCount(itemCount), Is.EqualTo(expectedPages));
            Assert.That(BinderPaging.SpreadCount(itemCount), Is.EqualTo(expectedSpreads));
        }

        [Test]
        public void GetSpread_NoItems_TwoPagesOfNineEmptySlots()
        {
            BinderSpread spread = BinderPaging.GetSpread(Entries(0), 0);

            Assert.That(spread.LeftPage.Count, Is.EqualTo(9));
            Assert.That(spread.RightPage.Count, Is.EqualTo(9));
            Assert.That(spread.LeftPage, Has.All.Null);
            Assert.That(spread.RightPage, Has.All.Null);
            Assert.That(spread.LeftPageNumber, Is.EqualTo(1));
            Assert.That(spread.RightPageNumber, Is.EqualTo(2));
            Assert.That(spread.PageCount, Is.EqualTo(2));
        }

        [Test]
        public void GetSpread_OneItem_FirstSlotFilledRestNull()
        {
            IReadOnlyList<BinderEntry> entries = Entries(1);

            BinderSpread spread = BinderPaging.GetSpread(entries, 0);

            Assert.That(spread.LeftPage[0], Is.SameAs(entries[0]));
            Assert.That(Filled(spread.LeftPage), Is.EqualTo(1));
            Assert.That(Filled(spread.RightPage), Is.EqualTo(0));
        }

        [Test]
        public void GetSpread_NineItems_FillsLeftPageExactly()
        {
            BinderSpread spread = BinderPaging.GetSpread(Entries(9), 0);

            Assert.That(Filled(spread.LeftPage), Is.EqualTo(9));
            Assert.That(Filled(spread.RightPage), Is.EqualTo(0));
        }

        [Test]
        public void GetSpread_TenItems_TenthStartsRightPage()
        {
            IReadOnlyList<BinderEntry> entries = Entries(10);

            BinderSpread spread = BinderPaging.GetSpread(entries, 0);

            Assert.That(Filled(spread.LeftPage), Is.EqualTo(9));
            Assert.That(spread.RightPage[0], Is.SameAs(entries[9]));
            Assert.That(Filled(spread.RightPage), Is.EqualTo(1));
        }

        [Test]
        public void GetSpread_EighteenItems_BothPagesFullAndOnlyOneSpread()
        {
            BinderSpread spread = BinderPaging.GetSpread(Entries(18), 0);

            Assert.That(Filled(spread.LeftPage), Is.EqualTo(9));
            Assert.That(Filled(spread.RightPage), Is.EqualTo(9));
            Assert.That(spread.SpreadCount, Is.EqualTo(1));
        }

        [Test]
        public void GetSpread_NineteenItemsSecondSpread_NineteenthAloneOnPageThree()
        {
            IReadOnlyList<BinderEntry> entries = Entries(19);

            BinderSpread spread = BinderPaging.GetSpread(entries, 1);

            Assert.That(spread.LeftPage[0], Is.SameAs(entries[18]));
            Assert.That(Filled(spread.LeftPage), Is.EqualTo(1));
            Assert.That(Filled(spread.RightPage), Is.EqualTo(0));
            Assert.That(spread.LeftPageNumber, Is.EqualTo(3));
            Assert.That(spread.RightPageNumber, Is.EqualTo(4));
            Assert.That(spread.PageCount, Is.EqualTo(4));
            Assert.That(spread.SpreadIndex, Is.EqualTo(1));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void GetSpread_SpreadOutsideRange_Throws(int spreadIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BinderPaging.GetSpread(Entries(18), spreadIndex));
        }

        [Test]
        public void GetSpread_NullEntries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BinderPaging.GetSpread(null, 0));
        }

        private static IReadOnlyList<BinderEntry> Entries(int count)
        {
            var entries = new List<BinderEntry>();
            for (int i = 0; i < count; i++)
            {
                entries.Add(new BinderEntry("item" + i, "Item " + i, 1, 1));
            }

            return entries;
        }

        private static int Filled(IReadOnlyList<BinderEntry> page)
        {
            int filled = 0;
            foreach (BinderEntry slot in page)
            {
                if (slot != null)
                {
                    filled++;
                }
            }

            return filled;
        }
    }
}
