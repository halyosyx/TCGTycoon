namespace Game.Core.Store
{
    /// <summary>One cart line: a listing, how many, and the price. Immutable; the cart replaces a line to change it.</summary>
    public sealed class CartLine
    {
        public CartLine(string listingId, int quantity, long unitPriceCents)
        {
            ListingId = listingId;
            Quantity = quantity;
            UnitPriceCents = unitPriceCents;
        }

        public string ListingId { get; }

        public int Quantity { get; }

        public long UnitPriceCents { get; }

        public long LineTotalCents => checked(UnitPriceCents * Quantity);
    }
}
