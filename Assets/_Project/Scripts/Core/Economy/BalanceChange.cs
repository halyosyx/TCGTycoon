namespace Game.Core.Economy
{
    /// <summary>What <see cref="EconomyService.BalanceChanged"/> reports: old and new balance, the signed delta and why.</summary>
    public readonly struct BalanceChange
    {
        public BalanceChange(long oldCents, long newCents, TransactionReason reason)
        {
            OldCents = oldCents;
            NewCents = newCents;
            Reason = reason;
        }

        public long OldCents { get; }

        public long NewCents { get; }

        public long DeltaCents => NewCents - OldCents;

        public TransactionReason Reason { get; }
    }
}
