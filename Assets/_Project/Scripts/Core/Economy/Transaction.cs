namespace Game.Core.Economy
{
    /// <summary>One ledger entry: a reason-coded movement of cash. Immutable.</summary>
    public sealed class Transaction
    {
        public Transaction(TransactionReason reason, string itemId, int quantity, long unitPriceCents, long amountCents, long balanceAfterCents)
        {
            Reason = reason;
            ItemId = itemId ?? string.Empty;
            Quantity = quantity;
            UnitPriceCents = unitPriceCents;
            AmountCents = amountCents;
            BalanceAfterCents = balanceAfterCents;
        }

        public TransactionReason Reason { get; }

        /// <summary>What the money was for: a product id, a card id, or a label for fees and debug.</summary>
        public string ItemId { get; }

        public int Quantity { get; }

        /// <summary>What one unit cost or sold for. Cost basis depends on it.</summary>
        public long UnitPriceCents { get; }

        /// <summary>Signed: negative for money out, positive for money in.</summary>
        public long AmountCents { get; }

        public long BalanceAfterCents { get; }
    }
}
