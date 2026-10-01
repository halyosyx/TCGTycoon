using System;

namespace Game.Core.Store
{
    /// <summary>One product on the store's shelf, as authored: availability and nightly stock. Immutable.</summary>
    public sealed class StoreCatalogListing
    {
        /// <summary><see cref="StockPerNight"/> value meaning no limit.</summary>
        public const int UnlimitedStock = -1;

        public StoreCatalogListing(Product product, ListingAvailability availability, int stockPerNight)
        {
            if (stockPerNight < UnlimitedStock)
            {
                throw new ArgumentOutOfRangeException(nameof(stockPerNight), stockPerNight, "Stock is -1 (unlimited) or more.");
            }

            Product = product ?? throw new ArgumentNullException(nameof(product));
            Availability = availability;
            StockPerNight = stockPerNight;
        }

        /// <summary>The listing's id: its product's id (one listing per product).</summary>
        public string Id => Product.Id;

        public Product Product { get; }

        public ListingAvailability Availability { get; }

        /// <summary>Units the supplier has each Prep Night; <see cref="UnlimitedStock"/> for no limit.</summary>
        public int StockPerNight { get; }
    }
}
