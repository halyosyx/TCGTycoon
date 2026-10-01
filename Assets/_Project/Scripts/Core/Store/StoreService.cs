using System;
using System.Collections.Generic;
using Game.Core.Economy;
using Game.Core.Inventory;

namespace Game.Core.Store
{
    /// <summary>
    /// The supplier website's rules: listings with tonight's stock, the cart, and placing an order.
    /// An order is one all-or-nothing <see cref="EconomyService.TryDebit(TransactionReason, IReadOnlyList{TransactionLine})"/>
    /// (one ledger entry per line); on success the stock goes down, the products enter the inventory
    /// sealed at the unit price paid, and the cart empties. On failure nothing changes or fires.
    /// Holds no UI state: views read <see cref="Lines"/> and listen to <see cref="CartChanged"/>.
    /// </summary>
    public sealed class StoreService
    {
        private readonly EconomyService _economy;
        private readonly InventoryService _inventory;
        private readonly List<StoreListingState> _listings = new List<StoreListingState>();
        private readonly Dictionary<string, StoreListingState> _listingsById = new Dictionary<string, StoreListingState>(StringComparer.Ordinal);
        private readonly List<CartLine> _lines = new List<CartLine>();

        public StoreService(StoreCatalog catalog, EconomyService economy, InventoryService inventory)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            foreach (StoreCatalogListing listing in catalog.Listings)
            {
                var state = new StoreListingState(listing, PriceOf(listing.Product));
                _listings.Add(state);
                _listingsById.Add(state.Id, state);
            }

            Listings = _listings.AsReadOnly();
            Lines = _lines.AsReadOnly();
        }

        /// <summary>Raised once after any change to the cart's lines or quantities.</summary>
        public event Action CartChanged;

        /// <summary>Every listing in catalog order, Hidden ones included (views skip them).</summary>
        public IReadOnlyList<StoreListingState> Listings { get; }

        /// <summary>Cart lines in the order they were first added.</summary>
        public IReadOnlyList<CartLine> Lines { get; }

        public int ItemCount
        {
            get
            {
                int count = 0;
                foreach (CartLine line in _lines)
                {
                    count += line.Quantity;
                }

                return count;
            }
        }

        public long SubtotalCents
        {
            get
            {
                long totalCents = 0;
                foreach (CartLine line in _lines)
                {
                    totalCents = checked(totalCents + line.LineTotalCents);
                }

                return totalCents;
            }
        }

        public bool TryGetListing(string listingId, out StoreListingState listing)
        {
            listing = null;
            return listingId != null && _listingsById.TryGetValue(listingId, out listing);
        }

        /// <summary>What the player pays for one unit. The only way to read a store price.</summary>
        /// <exception cref="KeyNotFoundException">No listing has that id.</exception>
        public long GetUnitPriceCents(string listingId) => Get(listingId).UnitPriceCents;

        /// <summary>
        /// Sets a listing's cart quantity, clamped to 0…stock remaining (no upper bound when unlimited);
        /// 0 removes the line. Listings that aren't Available can't enter the cart: nothing happens.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No listing has that id.</exception>
        public void SetQuantity(string listingId, int quantity)
        {
            StoreListingState listing = Get(listingId);
            if (listing.Availability != ListingAvailability.Available)
            {
                return;
            }

            int clamped = Math.Max(0, quantity);
            if (!listing.IsUnlimited)
            {
                clamped = Math.Min(clamped, listing.StockRemaining);
            }

            int index = IndexOfLine(listing.Id);
            int current = index < 0 ? 0 : _lines[index].Quantity;
            if (clamped == current)
            {
                return;
            }

            if (clamped == 0)
            {
                _lines.RemoveAt(index);
            }
            else if (index < 0)
            {
                _lines.Add(new CartLine(listing.Id, clamped, listing.UnitPriceCents));
            }
            else
            {
                _lines[index] = new CartLine(listing.Id, clamped, listing.UnitPriceCents);
            }

            CartChanged?.Invoke();
        }

        /// <summary>Adds <paramref name="quantity"/> units to the listing's line, with the same clamping as <see cref="SetQuantity"/>.</summary>
        public void Add(string listingId, int quantity = 1)
        {
            if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Add at least one unit.");

            int index = IndexOfLine(Get(listingId).Id);
            int current = index < 0 ? 0 : _lines[index].Quantity;
            SetQuantity(listingId, current > int.MaxValue - quantity ? int.MaxValue : current + quantity);
        }

        /// <summary>Removes the listing's line, if the cart has one.</summary>
        public void Remove(string listingId)
        {
            int index = IndexOfLine(Get(listingId).Id);
            if (index < 0)
            {
                return;
            }

            _lines.RemoveAt(index);
            CartChanged?.Invoke();
        }

        public void Clear()
        {
            if (_lines.Count == 0)
            {
                return;
            }

            _lines.Clear();
            CartChanged?.Invoke();
        }

        public OrderCheck CanPlaceOrder()
        {
            if (_lines.Count == 0)
            {
                return OrderCheck.Empty;
            }

            foreach (CartLine line in _lines)
            {
                StoreListingState listing = Get(line.ListingId);
                if (!listing.IsUnlimited && line.Quantity > listing.StockRemaining)
                {
                    return OrderCheck.OutOfStock;
                }
            }

            return _economy.CanAfford(SubtotalCents) ? OrderCheck.Ok : OrderCheck.OverBalance;
        }

        /// <summary>
        /// Orders the cart: one debit for the total, then stock down, products into the inventory
        /// sealed at their unit price, and the cart cleared. A failed check changes nothing.
        /// </summary>
        public OrderResult PlaceOrder()
        {
            OrderCheck check = CanPlaceOrder();
            if (check != OrderCheck.Ok)
            {
                return OrderResult.Failed(check);
            }

            var debitLines = new TransactionLine[_lines.Count];
            for (int i = 0; i < _lines.Count; i++)
            {
                CartLine line = _lines[i];
                debitLines[i] = new TransactionLine(Get(line.ListingId).ProductId, line.Quantity, line.UnitPriceCents);
            }

            long totalCents = SubtotalCents;
            if (!_economy.TryDebit(TransactionReason.ProductPurchase, debitLines))
            {
                return OrderResult.Failed(OrderCheck.OverBalance);
            }

            var ordered = new List<CartLine>(_lines);
            foreach (CartLine line in ordered)
            {
                StoreListingState listing = Get(line.ListingId);
                if (!listing.IsUnlimited)
                {
                    listing.StockRemaining -= line.Quantity;
                }

                _inventory.AddSealed(listing.ProductId, line.Quantity, line.UnitPriceCents);
            }

            _lines.Clear();
            CartChanged?.Invoke();
            return OrderResult.Placed(ordered.AsReadOnly(), totalCents);
        }

        /// <summary>
        /// Restores every listing's stock to its nightly amount. Call at the start of each Prep Night.
        /// TODO(F2 day cycle): the GameStateMachine calls this; until then nothing does.
        /// </summary>
        public void ResetNightlyStock()
        {
            foreach (StoreListingState listing in _listings)
            {
                listing.StockRemaining = listing.Listing.StockPerNight;
            }
        }

        // The single place a unit price is computed. F4 swaps the market price source here.
        private static long PriceOf(Product product) => StorePricing.UnitPriceCents(product.MarketPriceCents, product.SupplierPercent);

        private StoreListingState Get(string listingId)
        {
            if (listingId == null || !_listingsById.TryGetValue(listingId, out StoreListingState listing))
            {
                throw new KeyNotFoundException($"The store has no listing '{listingId}'.");
            }

            return listing;
        }

        private int IndexOfLine(string listingId)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (string.Equals(_lines[i].ListingId, listingId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
