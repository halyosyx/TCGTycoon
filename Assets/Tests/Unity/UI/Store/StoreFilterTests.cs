using System.Collections.Generic;
using Game.Core.Store;
using Game.Unity.UI.Store;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.Store
{
    public sealed class StoreFilterTests
    {
        // Catalog order on purpose differs from price order.
        private static readonly StoreFilterItem[] s_items =
        {
            new StoreFilterItem("SetB_Pack", "SetB", "Booster Pack", "Mythical Origins", ProductType.BoosterPack, 900, 0, isHidden: false),
            new StoreFilterItem("SetA_Box", "SetA", "Booster Box", "Reigning Champions", ProductType.Box, 16_000, 1, isHidden: false),
            new StoreFilterItem("SetA_Pack", "SetA", "Booster Pack", "Reigning Champions", ProductType.BoosterPack, 500, 2, isHidden: false),
            new StoreFilterItem("SetA_Bundle", "SetA", "Booster Bundle", "Reigning Champions", ProductType.Bundle, 2_800, 3, isHidden: false),
            new StoreFilterItem("SetB_Secret", "SetB", "Secret Box", "Mythical Origins", ProductType.Box, 100, 4, isHidden: true),
        };

        [Test]
        public void Apply_AllSetsByPrice_CheapestFirstAndHiddenLeftOut()
        {
            Assert.That(Ids(new StoreFilterCriteria()), Is.EqualTo(new[] { "SetA_Pack", "SetB_Pack", "SetA_Bundle", "SetA_Box" }));
        }

        [Test]
        public void Apply_ByName_AlphabeticalThenCatalogOrder()
        {
            var criteria = new StoreFilterCriteria { Sort = StoreSort.Name };

            Assert.That(Ids(criteria), Is.EqualTo(new[] { "SetA_Box", "SetA_Bundle", "SetB_Pack", "SetA_Pack" }));
        }

        [Test]
        public void Apply_SetTab_OnlyThatSet()
        {
            var criteria = new StoreFilterCriteria { SetId = "SetB" };

            Assert.That(Ids(criteria), Is.EqualTo(new[] { "SetB_Pack" }));
        }

        [Test]
        public void Apply_Type_OnlyThatType()
        {
            var criteria = new StoreFilterCriteria { Type = ProductType.BoosterPack };

            Assert.That(Ids(criteria), Is.EqualTo(new[] { "SetA_Pack", "SetB_Pack" }));
        }

        [TestCase("bundle", new[] { "SetA_Bundle" })]
        [TestCase("  ORIGINS ", new[] { "SetB_Pack" })]
        [TestCase("box", new[] { "SetA_Box" })]
        [TestCase("nothing like it", new string[0])]
        [TestCase("", new[] { "SetA_Pack", "SetB_Pack", "SetA_Bundle", "SetA_Box" })]
        public void Apply_Search_MatchesProductOrSetNameIgnoringCase(string search, string[] expected)
        {
            var criteria = new StoreFilterCriteria { Search = search };

            Assert.That(Ids(criteria), Is.EqualTo(expected));
        }

        [Test]
        public void Apply_AllCriteria_Combine()
        {
            var criteria = new StoreFilterCriteria { SetId = "SetA", Type = ProductType.Box, Search = "booster" };

            Assert.That(Ids(criteria), Is.EqualTo(new[] { "SetA_Box" }));
        }

        private static List<string> Ids(StoreFilterCriteria criteria)
        {
            var results = new List<StoreFilterItem>();
            StoreFilter.Apply(s_items, criteria, results);
            var ids = new List<string>();
            foreach (StoreFilterItem item in results)
            {
                ids.Add(item.Id);
            }

            return ids;
        }
    }
}
