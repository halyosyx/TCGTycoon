namespace Game.Core.Store
{
    /// <summary>
    /// A listing as the store shows it tonight: its price and how many units are left. Read-only for
    /// callers; <see cref="StoreService"/> updates the stock.
    /// </summary>
    public sealed class StoreListingState
    {
        internal StoreListingState(StoreCatalogListing listing, long unitPriceCents)
        {
            Listing = listing;
            UnitPriceCents = unitPriceCents;
            StockRemaining = listing.StockPerNight;
        }

        public string Id => Listing.Id;

        public string ProductId => Listing.Product.Id;

        public string SetId => Listing.Product.SetId;

        public Product Product => Listing.Product;

        public ListingAvailability Availability => Listing.Availability;

        /// <summary>What the player pays per unit (see <see cref="StoreService.GetUnitPriceCents"/>).</summary>
        public long UnitPriceCents { get; }

        /// <summary>Units left tonight; <see cref="StoreCatalogListing.UnlimitedStock"/> for no limit.</summary>
        public int StockRemaining { get; internal set; }

        public bool IsUnlimited => StockRemaining == StoreCatalogListing.UnlimitedStock;

        internal StoreCatalogListing Listing { get; }
    }
}
