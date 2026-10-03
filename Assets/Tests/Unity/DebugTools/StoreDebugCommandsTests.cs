using Game.Core.Economy;
using Game.Core.Inventory;
using Game.Core.Store;
using Game.Unity.DebugTools;
using NUnit.Framework;

namespace Game.Unity.Tests.DebugTools
{
    public sealed class StoreDebugCommandsTests
    {
        private const string BundleId = "SetA_Bundle";
        private const string BoxId = "SetA_Box";
        private const string SoonId = "SetB_Bundle";

        private EconomyService _economy;
        private InventoryService _inventory;
        private StoreService _store;

        [SetUp]
        public void SetUp()
        {
            _economy = new EconomyService(50_000);
            _inventory = new InventoryService();
            _store = new StoreService(
                new StoreCatalog(new[]
                {
                    Listing(BundleId, 2_800, ListingAvailability.Available, StoreCatalogListing.UnlimitedStock),
                    Listing(BoxId, 16_000, ListingAvailability.Available, 2),
                    Listing(SoonId, 5_000, ListingAvailability.ComingSoon, StoreCatalogListing.UnlimitedStock),
                }),
                _economy,
                _inventory);
        }

        [TestCase("buy SetA_Pack 12", true)]
        [TestCase("  BUY", true)]
        [TestCase("store.stock SetA_Box 3", true)]
        [TestCase("store.open", false)]
        [TestCase("buyer 1", false)]
        [TestCase("money.add 5", false)]
        [TestCase(null, false)]
        public void Handles_CommandLine_OnlyBuy(string commandLine, bool expected)
        {
            Assert.That(StoreDebugCommands.Handles(commandLine), Is.EqualTo(expected));
        }

        [Test]
        public void Execute_Buy_OrdersThroughTheStore()
        {
            string output = new StoreDebugCommands(_store).Execute("buy SetA_Bundle 3");

            Assert.That(output, Does.StartWith("Bought 3"));
            Assert.That(_economy.BalanceCents, Is.EqualTo(50_000 - 8_400));
            Assert.That(_economy.Ledger.Count, Is.EqualTo(1));
            Assert.That(_economy.Ledger[0].Reason, Is.EqualTo(TransactionReason.ProductPurchase));
            Assert.That(_inventory.CountOfSealed(BundleId), Is.EqualTo(3));
        }

        [Test]
        public void Execute_BuyMoreThanStock_ClampsAndSaysSo()
        {
            string output = new StoreDebugCommands(_store).Execute("buy SetA_Box 5");

            Assert.That(output, Does.Contain("only 2 left"));
            Assert.That(_inventory.CountOfSealed(BoxId), Is.EqualTo(2));
        }

        [Test]
        public void Execute_BuyWhenSoldOut_NothingOrdered()
        {
            var commands = new StoreDebugCommands(_store);
            commands.Execute("buy SetA_Box 2");

            string output = commands.Execute("buy SetA_Box 1");

            Assert.That(output, Does.Contain("sold out"));
            Assert.That(_economy.Ledger.Count, Is.EqualTo(1));
        }

        [Test]
        public void Execute_BuyOverBalance_FailsWithoutChange()
        {
            string output = new StoreDebugCommands(_store).Execute("buy SetA_Bundle 18");

            Assert.That(output, Does.StartWith("Order failed (OverBalance)"));
            Assert.That(_economy.BalanceCents, Is.EqualTo(50_000));
            Assert.That(_inventory.SealedStacks, Is.Empty);
        }

        [Test]
        public void Execute_Buy_KeepsWhatWasInTheCart()
        {
            _store.SetQuantity(BoxId, 1);

            new StoreDebugCommands(_store).Execute("buy SetA_Bundle 1");

            Assert.That(_store.Lines.Count, Is.EqualTo(1));
            Assert.That(_store.Lines[0].ListingId, Is.EqualTo(BoxId));
        }

        [TestCase("buy SetB_Bundle 1", "ComingSoon")]
        [TestCase("buy Nope 1", "No listing")]
        [TestCase("buy SetA_Bundle 0", "Usage")]
        [TestCase("buy SetA_Bundle", "Usage")]
        public void Execute_InvalidBuy_ExplainsAndChangesNothing(string commandLine, string expected)
        {
            string output = new StoreDebugCommands(_store).Execute(commandLine);

            Assert.That(output, Does.Contain(expected));
            Assert.That(_economy.Ledger, Is.Empty);
        }

        [Test]
        public void Execute_Stock_SetsTonightsStock()
        {
            string output = new StoreDebugCommands(_store).Execute("store.stock SetA_Bundle 12");

            _store.TryGetListing(BundleId, out StoreListingState listing);
            Assert.That(listing.StockRemaining, Is.EqualTo(12));
            Assert.That(output, Does.Contain("12"));
        }

        [Test]
        public void Execute_StockMinusOne_MakesItUnlimited()
        {
            new StoreDebugCommands(_store).Execute("store.stock SetA_Box -1");

            _store.TryGetListing(BoxId, out StoreListingState listing);
            Assert.That(listing.IsUnlimited, Is.True);
        }

        [TestCase("store.stock SetA_Box -2", "Usage")]
        [TestCase("store.stock SetA_Box", "Usage")]
        [TestCase("store.stock Nope 3", "No listing")]
        public void Execute_InvalidStock_ExplainsAndChangesNothing(string commandLine, string expected)
        {
            string output = new StoreDebugCommands(_store).Execute(commandLine);

            _store.TryGetListing(BoxId, out StoreListingState listing);
            Assert.That(output, Does.Contain(expected));
            Assert.That(listing.StockRemaining, Is.EqualTo(2));
        }

        private static StoreCatalogListing Listing(string id, long marketCents, ListingAvailability availability, int stock)
        {
            var product = new Product(id, "Bundle", "SetA", ProductType.Bundle, 6, marketCents, 100, null, null);
            return new StoreCatalogListing(product, availability, stock);
        }
    }
}
