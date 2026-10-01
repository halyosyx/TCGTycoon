using System;
using System.Collections.Generic;

namespace Game.Core.Economy
{
    /// <summary>
    /// The single owner of cash. Nothing else stores a balance or does arithmetic on it: features call
    /// this service, and views show what <see cref="BalanceChanged"/> tells them. Every movement is
    /// reason-coded in the <see cref="Ledger"/>. A debit that can't be paid returns false and changes
    /// nothing; the balance never goes negative by accident.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly List<Transaction> _ledger = new List<Transaction>();

        public EconomyService(long startingCashCents)
        {
            if (startingCashCents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingCashCents), startingCashCents, "Starting cash can't be negative.");
            }

            BalanceCents = startingCashCents;
            Ledger = _ledger.AsReadOnly();
        }

        /// <summary>Raised once per successful debit or credit, after the balance and ledger are updated.</summary>
        public event Action<BalanceChange> BalanceChanged;

        public long BalanceCents { get; private set; }

        /// <summary>Every movement in order. Read-only for callers.</summary>
        public IReadOnlyList<Transaction> Ledger { get; }

        public bool CanAfford(long cents) => cents <= BalanceCents;

        /// <summary>Takes <paramref name="cents"/> if the balance covers it; otherwise returns false and changes nothing.</summary>
        public bool TryDebit(long cents, TransactionReason reason, string itemId)
        {
            if (cents < 0) throw new ArgumentOutOfRangeException(nameof(cents), cents, "Debit a positive amount.");

            return TryDebit(reason, new[] { new TransactionLine(itemId, 1, cents) });
        }

        /// <summary>
        /// Takes the sum of <paramref name="lines"/> in one movement, all or nothing: on success one
        /// <see cref="BalanceChanged"/> for the total and one ledger entry per line (each with its unit
        /// price, because cost basis depends on what was paid). If the balance can't cover the total,
        /// returns false and nothing changes or fires.
        /// </summary>
        public bool TryDebit(TransactionReason reason, IReadOnlyList<TransactionLine> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            if (lines.Count == 0) throw new ArgumentException("Debit at least one line.", nameof(lines));

            long totalCents = 0;
            foreach (TransactionLine line in lines)
            {
                if (line.Quantity <= 0)
                {
                    throw new ArgumentException("A line needs at least one unit (use the TransactionLine constructor).", nameof(lines));
                }

                totalCents = checked(totalCents + line.TotalCents);
            }

            if (!CanAfford(totalCents))
            {
                return false;
            }

            long oldCents = BalanceCents;
            long runningCents = oldCents;
            foreach (TransactionLine line in lines)
            {
                runningCents -= line.TotalCents;
                _ledger.Add(new Transaction(reason, line.ItemId, line.Quantity, line.UnitPriceCents, -line.TotalCents, runningCents));
            }

            BalanceCents = runningCents;
            BalanceChanged?.Invoke(new BalanceChange(oldCents, BalanceCents, reason));
            return true;
        }

        public void Credit(long cents, TransactionReason reason, string itemId)
        {
            if (cents < 0) throw new ArgumentOutOfRangeException(nameof(cents), cents, "Credit a positive amount.");

            long oldCents = BalanceCents;
            BalanceCents = checked(oldCents + cents);
            _ledger.Add(new Transaction(reason, itemId, 1, cents, cents, BalanceCents));
            BalanceChanged?.Invoke(new BalanceChange(oldCents, BalanceCents, reason));
        }
    }
}
