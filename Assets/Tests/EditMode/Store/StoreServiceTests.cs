using System;
using System.Collections.Generic;
using Game.Core.Economy;
using Game.Core.Inventory;
using Game.Core.Store;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;
using static Game.Core.Tests.TestUtilities.StoreFixtures;

namespace Game.Core.Tests.Store
{
    public sealed class StoreServiceTests
    {
        private const long StartingCents = 50_000;

        private EconomyService _economy;
        private InventoryService _inventory;

        [SetUp]
        public void CreateServices()
        {
            _economy = new EconomyService(StartingCents);
            _inventory = new InventoryService();
        }

        // --- Listings and price ---

        [Test]
        public void Listings_DefaultCatalog_InCatalogOrderWithUnitPrices()
        {
            StoreService store = CreateStore(DefaultCatalog());

            Assert.That(store.Listings.Count, Is.EqualTo(3));
            Assert.That(store.Listings[0].Id, Is.EqualTo(ChampionsPackId));
            Assert.That(store.Listings[0].ProductId, Is.EqualTo(ChampionsPackId));
            Assert.That(store.Listings[0].SetId, Is.EqualTo(TestContent.SetId));
            Assert.That(store.Listings[0].UnitPriceCents, Is.EqualTo(500));
            Assert.That(store.Listings[0].Availability, Is.EqualTo(ListingAvailability.Available));
            Assert.That(store.Listings[0].StockRemaining, Is.EqualTo(StoreCatalogListing.UnlimitedStock));
            Assert.That(store.Listings[1].UnitPriceCents, Is.EqualTo(900));
            Assert.That(store.Listings[2].Availability, Is.EqualTo(ListingAvailability.ComingSoon));
        }

        [Test]
        public void GetUnitPriceCents_SupplierPercent_IsMarketTimesPercentRoundedHalfUp()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(333, 50))));

            Assert.That(store.GetUnitPriceCents(ChampionsPackId), Is.EqualTo(167));
        }

        [Test]
        public void GetUnitPriceCents_UnknownListing_Throws()
        {
            StoreService store = CreateStore(DefaultCatalog());

            Assert.Throws<KeyNotFoundException>(() => store.GetUnitPriceCents("nope"));
        }

        [Test]
        public void CatalogWithDifferentPriceOrPercent_UnitSubtotalAndLedgerFollow()
        {
            // Same code, two catalogs: the authored numbers alone decide every price.
            StoreService full = CreateStore(Catalog(Listing(ChampionsPack(500, 100))));
            var otherEconomy = new EconomyService(StartingCents);
            var discounted = new StoreService(Catalog(Listing(ChampionsPack(600, 90))), otherEconomy, new InventoryService());

            full.SetQuantity(ChampionsPackId, 4);
            discounted.SetQuantity(ChampionsPackId, 4);

            Assert.That(full.GetUnitPriceCents(ChampionsPackId), Is.EqualTo(500));
            Assert.That(discounted.GetUnitPriceCents(ChampionsPackId), Is.EqualTo(540));
            Assert.That(full.SubtotalCents, Is.EqualTo(2_000));
            Assert.That(discounted.SubtotalCents, Is.EqualTo(2_160));

            full.PlaceOrder();
            discounted.PlaceOrder();

            Assert.That(_economy.Ledger[0].UnitPriceCents, Is.EqualTo(500));
            Assert.That(otherEconomy.Ledger[0].UnitPriceCents, Is.EqualTo(540));
            Assert.That(otherEconomy.BalanceCents, Is.EqualTo(StartingCents - 2_160));
        }

        [Test]
        public void Constructor_DuplicateListingIds_Throws()
        {
            Assert.Throws<ArgumentException>(() => Catalog(Listing(ChampionsPack()), Listing(ChampionsPack())));
        }

        [Test]
        public void Listing_StockBelowUnlimited_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Listing(ChampionsPack(), ListingAvailability.Available, -2));
        }

        // --- Cart ---

        [Test]
        public void SetQuantity_Available_AddsLineWithTotals()
        {
            StoreService store = CreateStore(DefaultCatalog());

            store.SetQuantity(ChampionsPackId, 3);

            Assert.That(store.Lines.Count, Is.EqualTo(1));
            Assert.That(store.Lines[0].ListingId, Is.EqualTo(ChampionsPackId));
            Assert.That(store.Lines[0].Quantity, Is.EqualTo(3));
            Assert.That(store.Lines[0].UnitPriceCents, Is.EqualTo(500));
            Assert.That(store.Lines[0].LineTotalCents, Is.EqualTo(1_500));
            Assert.That(store.ItemCount, Is.EqualTo(3));
            Assert.That(store.SubtotalCents, Is.EqualTo(1_500));
        }

        [Test]
        public void SetQuantity_AboveStock_ClampsToStock()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(), ListingAvailability.Available, 12)));

            store.SetQuantity(ChampionsPackId, 20);

            Assert.That(store.Lines[0].Quantity, Is.EqualTo(12));
        }

        [Test]
        public void SetQuantity_Unlimited_NotClamped()
        {
            StoreService store = CreateStore(DefaultCatalog());

            store.SetQuantity(ChampionsPackId, 500);

            Assert.That(store.Lines[0].Quantity, Is.EqualTo(500));
        }

        [Test]
        public void SetQuantity_Negative_ClampsToZeroRemovesLine()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 2);

            store.SetQuantity(ChampionsPackId, -3);

            Assert.That(store.Lines, Is.Empty);
            Assert.That(store.ItemCount, Is.EqualTo(0));
        }

        [Test]
        public void SetQuantity_ZeroStock_CartStaysEmpty()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(), ListingAvailability.Available, 0)));

            store.SetQuantity(ChampionsPackId, 1);

            Assert.That(store.Lines, Is.Empty);
        }

        [TestCase(ListingAvailability.ComingSoon)]
        [TestCase(ListingAvailability.Hidden)]
        public void Add_ComingSoonOrHidden_CartUnchangedNoEvent(ListingAvailability availability)
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsBundle(), availability)));
            int raised = 0;
            store.CartChanged += () => raised++;

            store.Add(ChampionsBundleId);
            store.SetQuantity(ChampionsBundleId, 2);

            Assert.That(store.Lines, Is.Empty);
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Add_Twice_AccumulatesInOneLine()
        {
            StoreService store = CreateStore(DefaultCatalog());

            store.Add(ChampionsPackId);
            store.Add(ChampionsPackId, 4);

            Assert.That(store.Lines.Count, Is.EqualTo(1));
            Assert.That(store.Lines[0].Quantity, Is.EqualTo(5));
        }

        [Test]
        public void Lines_KeepOrderOfFirstAdd()
        {
            StoreService store = CreateStore(DefaultCatalog());

            store.Add(OriginsPackId);
            store.Add(ChampionsPackId);
            store.Add(OriginsPackId);

            Assert.That(store.Lines[0].ListingId, Is.EqualTo(OriginsPackId));
            Assert.That(store.Lines[1].ListingId, Is.EqualTo(ChampionsPackId));
        }

        [Test]
        public void Remove_ExistingLine_RemovesItAndRaisesOnce()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.Add(ChampionsPackId, 2);
            store.Add(OriginsPackId, 1);
            int raised = 0;
            store.CartChanged += () => raised++;

            store.Remove(ChampionsPackId);
            store.Remove(ChampionsPackId);

            Assert.That(store.Lines.Count, Is.EqualTo(1));
            Assert.That(store.SubtotalCents, Is.EqualTo(900));
            Assert.That(raised, Is.EqualTo(1), "Removing a line that isn't there changes nothing.");
        }

        [Test]
        public void CartChanged_RaisedOnlyWhenTheCartChanges()
        {
            StoreService store = CreateStore(DefaultCatalog());
            int raised = 0;
            store.CartChanged += () => raised++;

            store.SetQuantity(ChampionsPackId, 2);
            store.SetQuantity(ChampionsPackId, 2);
            store.Clear();
            store.Clear();

            Assert.That(raised, Is.EqualTo(2));
        }

        [Test]
        public void SetQuantity_UnknownListing_Throws()
        {
            StoreService store = CreateStore(DefaultCatalog());

            Assert.Throws<KeyNotFoundException>(() => store.SetQuantity("nope", 1));
        }

        // --- Order checks ---

        [Test]
        public void CanPlaceOrder_EmptyCart_IsEmpty()
        {
            Assert.That(CreateStore(DefaultCatalog()).CanPlaceOrder(), Is.EqualTo(OrderCheck.Empty));
        }

        [Test]
        public void CanPlaceOrder_AffordableCart_IsOk()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 100);

            Assert.That(store.CanPlaceOrder(), Is.EqualTo(OrderCheck.Ok), "100 × 500 is exactly the balance.");
        }

        [Test]
        public void PlaceOrder_OverBalance_FailsAndCheckIsOverBalance()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 101);

            OrderResult result = store.PlaceOrder();

            Assert.That(store.CanPlaceOrder(), Is.EqualTo(OrderCheck.OverBalance));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Check, Is.EqualTo(OrderCheck.OverBalance));
            Assert.That(_economy.BalanceCents, Is.EqualTo(StartingCents));
        }

        [Test]
        public void PlaceOrder_Failure_NoEventsNoLedgerNoStockOrCartChange()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(), ListingAvailability.Available, 200)));
            store.SetQuantity(ChampionsPackId, 150);
            int balanceEvents = 0;
            int cartEvents = 0;
            int inventoryEvents = 0;
            _economy.BalanceChanged += _ => balanceEvents++;
            store.CartChanged += () => cartEvents++;
            _inventory.Changed += () => inventoryEvents++;

            OrderResult result = store.PlaceOrder();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(balanceEvents + cartEvents + inventoryEvents, Is.EqualTo(0));
            Assert.That(_economy.Ledger, Is.Empty);
            Assert.That(store.Listings[0].StockRemaining, Is.EqualTo(200));
            Assert.That(store.Lines[0].Quantity, Is.EqualTo(150));
            Assert.That(_inventory.SealedStacks, Is.Empty);
        }

        [Test]
        public void PlaceOrder_EmptyCart_FailsWithEmpty()
        {
            OrderResult result = CreateStore(DefaultCatalog()).PlaceOrder();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Check, Is.EqualTo(OrderCheck.Empty));
            Assert.That(result.Lines, Is.Empty);
        }

        // --- Successful orders ---

        [Test]
        public void Buy12ChampionsFrom500Dollars_Leaves440_OneEntryAt500PerUnit()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 12);

            OrderResult result = store.PlaceOrder();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_economy.BalanceCents, Is.EqualTo(44_000));
            Assert.That(_economy.Ledger.Count, Is.EqualTo(1));
            Assert.That(_economy.Ledger[0].Reason, Is.EqualTo(TransactionReason.ProductPurchase));
            Assert.That(_economy.Ledger[0].ItemId, Is.EqualTo(ChampionsPackId));
            Assert.That(_economy.Ledger[0].Quantity, Is.EqualTo(12));
            Assert.That(_economy.Ledger[0].UnitPriceCents, Is.EqualTo(500));
        }

        [Test]
        public void PlaceOrder_TwoListings_DebitsExactSumOfLines()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 12);
            store.SetQuantity(OriginsPackId, 3);

            OrderResult result = store.PlaceOrder();

            Assert.That(result.TotalCents, Is.EqualTo(8_700));
            Assert.That(_economy.BalanceCents, Is.EqualTo(StartingCents - 8_700));
        }

        [Test]
        public void PlaceOrder_TwoLines_TwoProductPurchaseEntriesWithUnitPrices()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 12);
            store.SetQuantity(OriginsPackId, 3);

            store.PlaceOrder();

            Assert.That(_economy.Ledger.Count, Is.EqualTo(2));
            Assert.That(_economy.Ledger[0].Reason, Is.EqualTo(TransactionReason.ProductPurchase));
            Assert.That(_economy.Ledger[0].UnitPriceCents, Is.EqualTo(500));
            Assert.That(_economy.Ledger[0].Quantity, Is.EqualTo(12));
            Assert.That(_economy.Ledger[1].Reason, Is.EqualTo(TransactionReason.ProductPurchase));
            Assert.That(_economy.Ledger[1].UnitPriceCents, Is.EqualTo(900));
            Assert.That(_economy.Ledger[1].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void PlaceOrder_Success_RaisesBalanceChangedOnce()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 2);
            store.SetQuantity(OriginsPackId, 1);
            var changes = new List<BalanceChange>();
            _economy.BalanceChanged += changes.Add;

            store.PlaceOrder();

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].DeltaCents, Is.EqualTo(-1_900));
            Assert.That(changes[0].Reason, Is.EqualTo(TransactionReason.ProductPurchase));
        }

        [Test]
        public void PlaceOrder_TwelvePacks_OneSealedStackOfTwelveAtUnitCost()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 12);

            store.PlaceOrder();

            Assert.That(_inventory.SealedStacks.Count, Is.EqualTo(1));
            Assert.That(_inventory.SealedStacks[0].ProductId, Is.EqualTo(ChampionsPackId));
            Assert.That(_inventory.SealedStacks[0].Count, Is.EqualTo(12));
            Assert.That(_inventory.SealedStacks[0].CostBasisCents, Is.EqualTo(6_000));
        }

        [Test]
        public void PlaceOrder_Success_ClearsCartDecrementsStockAndReportsLines()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(), ListingAvailability.Available, 12), Listing(OriginsPack())));
            store.SetQuantity(ChampionsPackId, 5);
            store.SetQuantity(OriginsPackId, 2);
            int cartEvents = 0;
            store.CartChanged += () => cartEvents++;

            OrderResult result = store.PlaceOrder();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Check, Is.EqualTo(OrderCheck.Ok));
            Assert.That(result.Lines.Count, Is.EqualTo(2));
            Assert.That(result.Lines[0].Quantity, Is.EqualTo(5));
            Assert.That(result.Lines[1].LineTotalCents, Is.EqualTo(1_800));
            Assert.That(store.Lines, Is.Empty);
            Assert.That(cartEvents, Is.EqualTo(1));
            Assert.That(store.Listings[0].StockRemaining, Is.EqualTo(7));
            Assert.That(store.Listings[1].StockRemaining, Is.EqualTo(StoreCatalogListing.UnlimitedStock));
        }

        [Test]
        public void SetQuantity_AfterOrder_ClampsToRemainingStock()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(), ListingAvailability.Available, 12)));
            store.SetQuantity(ChampionsPackId, 10);
            store.PlaceOrder();

            store.SetQuantity(ChampionsPackId, 5);

            Assert.That(store.Lines[0].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void ResetNightlyStock_AfterOrder_RestoresStockPerNight()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(), ListingAvailability.Available, 12)));
            store.SetQuantity(ChampionsPackId, 12);
            store.PlaceOrder();
            Assert.That(store.Listings[0].StockRemaining, Is.EqualTo(0));

            store.ResetNightlyStock();

            Assert.That(store.Listings[0].StockRemaining, Is.EqualTo(12));
        }

        // --- Stock ---

        [Test]
        public void SetStockRemaining_Limited_ChangesStockAndRaisesStockChanged()
        {
            StoreService store = CreateStore(DefaultCatalog());
            int raised = 0;
            store.StockChanged += () => raised++;

            store.SetStockRemaining(ChampionsPackId, 12);
            store.SetStockRemaining(ChampionsPackId, 12);

            Assert.That(store.Listings[0].StockRemaining, Is.EqualTo(12));
            Assert.That(raised, Is.EqualTo(1), "Setting the same stock changes nothing.");
        }

        [Test]
        public void SetStockRemaining_BelowUnlimited_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateStore(DefaultCatalog()).SetStockRemaining(ChampionsPackId, -2));
        }

        [Test]
        public void CanPlaceOrder_StockDroppedBelowCart_IsOutOfStockAndOrderFails()
        {
            StoreService store = CreateStore(DefaultCatalog());
            store.SetQuantity(ChampionsPackId, 10);
            store.SetStockRemaining(ChampionsPackId, 4);

            OrderResult result = store.PlaceOrder();

            Assert.That(store.CanPlaceOrder(), Is.EqualTo(OrderCheck.OutOfStock));
            Assert.That(result.Check, Is.EqualTo(OrderCheck.OutOfStock));
            Assert.That(_economy.Ledger, Is.Empty);
        }

        [Test]
        public void StockChanged_RaisedByAnOrderOnLimitedStockAndByTheNightlyReset()
        {
            StoreService store = CreateStore(Catalog(Listing(ChampionsPack(), ListingAvailability.Available, 12), Listing(OriginsPack())));
            int raised = 0;
            store.StockChanged += () => raised++;

            store.SetQuantity(OriginsPackId, 2);
            store.PlaceOrder();
            Assert.That(raised, Is.EqualTo(0), "Unlimited stock doesn't change.");

            store.SetQuantity(ChampionsPackId, 2);
            store.PlaceOrder();
            Assert.That(raised, Is.EqualTo(1));

            store.ResetNightlyStock();
            store.ResetNightlyStock();
            Assert.That(raised, Is.EqualTo(2), "A reset that changes nothing doesn't raise.");
        }

        private StoreService CreateStore(StoreCatalog catalog) => new StoreService(catalog, _economy, _inventory);
    }
}
