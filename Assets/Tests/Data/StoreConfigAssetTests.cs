using System.Collections.Generic;
using Game.Core.Store;
using Game.Unity.Definitions;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    /// <summary>
    /// Checks the real store assets (STORE_UI_REQUIREMENTS STR-02, STR-03): the default store config
    /// converts, the Champions pack costs $5.00 and the Origins pack $9.00 (each pack's market price is
    /// its pack configuration's), packs are on sale, and bundles and boxes are coming soon.
    /// </summary>
    public sealed class StoreConfigAssetTests
    {
        private const string StoreConfigPath = "Assets/_Project/Data/Store/StoreConfig.asset";
        private const string EconomyConfigPath = "Assets/_Project/Data/Balance/EconomyConfig.asset";
        private const string ChampionsPackId = "SetA_Pack";
        private const string OriginsPackId = "SetB_Pack";

        private StoreConfigDefinition _config;
        private StoreCatalog _catalog;

        [SetUp]
        public void LoadStore()
        {
            _config = AssetDatabase.LoadAssetAtPath<StoreConfigDefinition>(StoreConfigPath);
            Assert.That(_config != null, $"Store config not found at {StoreConfigPath}.");
            _catalog = _config.ToCatalog();
        }

        [Test]
        public void EconomyConfig_StartingCash_Is500Dollars()
        {
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfigDefinition>(EconomyConfigPath);

            Assert.That(economy != null, $"Economy config not found at {EconomyConfigPath}.");
            Assert.That(economy.StartingCashCents, Is.EqualTo(50_000));
        }

        [Test]
        public void StoreConfig_SiteNames_AreTheDefaults()
        {
            Assert.That(_config.SiteTitle, Is.EqualTo("VendorSupply"));
            Assert.That(_config.BrowserTabTitle, Is.EqualTo("VendorSupply · Sealed"));
            Assert.That(_config.AddressText, Is.EqualTo("vendorsupply.mb/sealed"));
            Assert.That(_config.Strings.AddToCart, Is.EqualTo("Add to cart"));
        }

        [Test]
        public void Catalog_EveryListing_HasAProductWithASet()
        {
            Assert.That(_catalog.Listings.Count, Is.EqualTo(_config.Listings.Count), "Every listing converts.");
            foreach (StoreCatalogListing listing in _catalog.Listings)
            {
                Assert.That(listing.Product.SetId, Is.Not.Empty, listing.Id);
            }
        }

        [TestCase(ChampionsPackId, 500L)]
        [TestCase(OriginsPackId, 900L)]
        public void Catalog_BoosterPack_StorePriceAndMarketMatchItsPack(string listingId, long expectedCents)
        {
            var store = new StoreService(_catalog, new Game.Core.Economy.EconomyService(0), new Game.Core.Inventory.InventoryService());
            Assert.That(store.TryGetListing(listingId, out StoreListingState listing), $"No listing '{listingId}'.");

            Assert.That(store.GetUnitPriceCents(listingId), Is.EqualTo(expectedCents));
            Assert.That(listing.Product.MarketPriceCents, Is.EqualTo(listing.Product.Pack.PriceCents), "A pack's market price is its pack configuration's.");
            Assert.That(listing.Availability, Is.EqualTo(ListingAvailability.Available));
            Assert.That(listing.Product.Type, Is.EqualTo(ProductType.BoosterPack));
        }

        [Test]
        public void Catalog_BundlesAndBoxes_AreComingSoon()
        {
            var types = new HashSet<ProductType>();
            foreach (StoreCatalogListing listing in _catalog.Listings)
            {
                types.Add(listing.Product.Type);
                if (listing.Product.Type != ProductType.BoosterPack)
                {
                    Assert.That(listing.Availability, Is.EqualTo(ListingAvailability.ComingSoon), listing.Id);
                }
            }

            Assert.That(types, Is.EquivalentTo(new[] { ProductType.BoosterPack, ProductType.Bundle, ProductType.Box }));
        }
    }
}
