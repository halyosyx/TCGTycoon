namespace Game.Core.Economy
{
    /// <summary>
    /// Why money moved. Every ledger entry carries one, so Results can compute revenue, cost of goods
    /// and fees by filtering the ledger instead of keeping counters that can drift.
    /// </summary>
    public enum TransactionReason
    {
        ProductPurchase = 0,
        CardSale = 1,
        SealedSale = 2,
        TableFee = 3,
        Debug = 4,
    }
}
