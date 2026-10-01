using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Core.Common;
using Game.Core.Store;
using static System.FormattableString;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Play Mode store commands on the running session, for buying without the store UI. An order goes
    /// through <see cref="StoreService"/> exactly as the website's will (one debit, ledger, sealed
    /// stock in the inventory); whatever was in the cart is put back afterwards. Plain C#, like
    /// <see cref="PackDebugCommands"/>.
    /// </summary>
    public sealed class StoreDebugCommands
    {
        public const string BuyCommand = "buy";

        public const string HelpText =
            "Store commands (Play Mode, on the scene's session):\n" +
            "  buy <listingId> <count>          order through the store (count clamps to tonight's stock)";

        private readonly StoreService _store;

        public StoreDebugCommands(StoreService store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>True for command lines this class runs ("buy", alone or with arguments).</summary>
        public static bool Handles(string commandLine)
        {
            if (commandLine == null)
            {
                return false;
            }

            string trimmed = commandLine.Trim();
            return string.Equals(trimmed, BuyCommand, StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(BuyCommand + " ", StringComparison.OrdinalIgnoreCase);
        }

        public string Execute(string commandLine)
        {
            string[] parts = (commandLine ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3
                || !string.Equals(parts[0], BuyCommand, StringComparison.OrdinalIgnoreCase)
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                || count < 1)
            {
                return "Usage: buy <listingId> <count>, where count is 1 or more.\n" + DescribeListings();
            }

            if (!_store.TryGetListing(parts[1], out StoreListingState listing))
            {
                return $"No listing '{parts[1]}'.\n" + DescribeListings();
            }

            if (listing.Availability != ListingAvailability.Available)
            {
                return $"'{listing.Id}' is {listing.Availability}; it can't be bought.";
            }

            return Buy(listing, count);
        }

        private string Buy(StoreListingState listing, int count)
        {
            var savedCart = new List<CartLine>(_store.Lines);
            _store.Clear();
            _store.SetQuantity(listing.Id, count);

            int ordered = _store.ItemCount;
            OrderResult result = ordered == 0 ? null : _store.PlaceOrder();
            _store.Clear();
            foreach (CartLine line in savedCart)
            {
                _store.SetQuantity(line.ListingId, line.Quantity);
            }

            if (result == null)
            {
                return $"'{listing.Id}' is sold out tonight.";
            }

            if (!result.IsSuccess)
            {
                return Invariant($"Order failed ({result.Check}): {ordered} × {listing.Id} costs {Money.FormatDisplay(ordered * listing.UnitPriceCents)}.");
            }

            string clamped = ordered < count ? Invariant($" (only {ordered} left tonight)") : string.Empty;
            return Invariant($"Bought {ordered} × {listing.Id} at {Money.FormatDisplay(listing.UnitPriceCents)} for {Money.FormatDisplay(result.TotalCents)}{clamped}.");
        }

        private string DescribeListings()
        {
            var output = new StringBuilder("Listings:");
            foreach (StoreListingState listing in _store.Listings)
            {
                string stock = listing.IsUnlimited ? "unlimited" : Invariant($"{listing.StockRemaining} left");
                output.Append(Invariant($"\n  {listing.Id,-16} {Money.FormatDisplay(listing.UnitPriceCents),9}  {listing.Availability,-10} {stock}"));
            }

            return output.ToString();
        }
    }
}
