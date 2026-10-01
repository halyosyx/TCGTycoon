using System;

namespace Game.Core.Economy
{
    /// <summary>One line of a multi-line debit (an order): an item, how many, and the unit price paid.</summary>
    public readonly struct TransactionLine
    {
        public TransactionLine(string itemId, int quantity, long unitPriceCents)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "A line needs at least one unit.");
            if (unitPriceCents < 0) throw new ArgumentOutOfRangeException(nameof(unitPriceCents), unitPriceCents, "A price can't be negative.");

            ItemId = itemId ?? string.Empty;
            Quantity = quantity;
            UnitPriceCents = unitPriceCents;
        }

        public string ItemId { get; }

        public int Quantity { get; }

        public long UnitPriceCents { get; }

        public long TotalCents => checked(UnitPriceCents * Quantity);
    }
}
