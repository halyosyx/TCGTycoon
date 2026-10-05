using Game.Core.Content;
using Game.Core.Store;

namespace Game.Core.Tests.TestUtilities
{
    /// <summary>
    /// Builds store products and catalogs in code: a Champions pack (Set A, 500 cents, like the real
    /// asset), an Origins pack at 900 cents (the real asset is 500; a different price here makes
    /// multi-line orders exercise two unit prices), both at 100% supplier, and a coming-soon bundle.
    /// Game.Data.Tests checks the real assets.
    /// </summary>
    public static class StoreFixtures
    {
        public const string ChampionsPackId = "SetA_Pack";
        public const string OriginsPackId = "SetB_Pack";
        public const string ChampionsBundleId = "SetA_Bundle";

        public const long ChampionsPackMarketCents = 500;
        public const long OriginsPackMarketCents = 900;
        public const long ChampionsBundleMarketCents = 2_800;

        public static Product ChampionsPack(long marketCents = ChampionsPackMarketCents, int supplierPercent = 100)
        {
            PackConfig pack = TestContent.Pack(marketCents, TestContent.StartingPack().Slots.ToArray());
            return new Product(ChampionsPackId, "Booster Pack", TestContent.SetId, ProductType.BoosterPack, 1, marketCents, supplierPercent, pack, TestContent.SetAPool());
        }

        public static Product OriginsPack(long marketCents = OriginsPackMarketCents, int supplierPercent = 100)
        {
            PackConfig pack = TestContent.Pack(marketCents, TestContent.StartingPack().Slots.ToArray());
            return new Product(OriginsPackId, "Booster Pack", "SetB", ProductType.BoosterPack, 1, marketCents, supplierPercent, pack, TestContent.SetAPool());
        }

        public static Product ChampionsBundle()
        {
            return new Product(ChampionsBundleId, "Bundle", TestContent.SetId, ProductType.Bundle, 6, ChampionsBundleMarketCents, 100, null, null);
        }

        public static StoreCatalogListing Listing(Product product, ListingAvailability availability = ListingAvailability.Available, int stockPerNight = StoreCatalogListing.UnlimitedStock)
        {
            return new StoreCatalogListing(product, availability, stockPerNight);
        }

        /// <summary>Champions and Origins packs (unlimited) and a coming-soon Champions bundle.</summary>
        public static StoreCatalog DefaultCatalog()
        {
            return new StoreCatalog(new[]
            {
                Listing(ChampionsPack()),
                Listing(OriginsPack()),
                Listing(ChampionsBundle(), ListingAvailability.ComingSoon),
            });
        }

        public static StoreCatalog Catalog(params StoreCatalogListing[] listings) => new StoreCatalog(listings);

        private static PackSlot[] ToArray(this System.Collections.Generic.IReadOnlyList<PackSlot> slots)
        {
            var array = new PackSlot[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                array[i] = slots[i];
            }

            return array;
        }
    }
}
