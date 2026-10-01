using System;
using System.Collections.Generic;
using Game.Core.Economy;
using NUnit.Framework;

namespace Game.Core.Tests.Economy
{
    public sealed class EconomyServiceTests
    {
        private const long StartingCents = 50_000;

        [Test]
        public void Constructor_StartingCash_IsTheBalanceWithEmptyLedger()
        {
            var economy = new EconomyService(StartingCents);

            Assert.That(economy.BalanceCents, Is.EqualTo(StartingCents));
            Assert.That(economy.Ledger, Is.Empty);
        }

        [Test]
        public void Constructor_NegativeStartingCash_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EconomyService(-1));
        }

        [TestCase(0L, true)]
        [TestCase(50_000L, true)]
        [TestCase(50_001L, false)]
        public void CanAfford_Amount_ComparesWithBalance(long cents, bool expected)
        {
            Assert.That(new EconomyService(StartingCents).CanAfford(cents), Is.EqualTo(expected));
        }

        [Test]
        public void TryDebit_Affordable_LowersBalanceAndRecordsOneEntry()
        {
            var economy = new EconomyService(StartingCents);

            bool isPaid = economy.TryDebit(6_000, TransactionReason.TableFee, "table");

            Assert.That(isPaid, Is.True);
            Assert.That(economy.BalanceCents, Is.EqualTo(44_000));
            Assert.That(economy.Ledger.Count, Is.EqualTo(1));
            Transaction entry = economy.Ledger[0];
            Assert.That(entry.Reason, Is.EqualTo(TransactionReason.TableFee));
            Assert.That(entry.ItemId, Is.EqualTo("table"));
            Assert.That(entry.Quantity, Is.EqualTo(1));
            Assert.That(entry.UnitPriceCents, Is.EqualTo(6_000));
            Assert.That(entry.AmountCents, Is.EqualTo(-6_000));
            Assert.That(entry.BalanceAfterCents, Is.EqualTo(44_000));
        }

        [Test]
        public void TryDebit_ExactBalance_LeavesZero()
        {
            var economy = new EconomyService(StartingCents);

            Assert.That(economy.TryDebit(StartingCents, TransactionReason.ProductPurchase, "box"), Is.True);
            Assert.That(economy.BalanceCents, Is.EqualTo(0));
        }

        [Test]
        public void TryDebit_MoreThanBalance_ReturnsFalseBalanceUnchanged()
        {
            var economy = new EconomyService(StartingCents);

            Assert.That(economy.TryDebit(StartingCents + 1, TransactionReason.ProductPurchase, "box"), Is.False);
            Assert.That(economy.BalanceCents, Is.EqualTo(StartingCents));
        }

        [Test]
        public void TryDebit_Insufficient_NoEventNoLedger()
        {
            var economy = new EconomyService(100);
            int raised = 0;
            economy.BalanceChanged += _ => raised++;

            economy.TryDebit(101, TransactionReason.ProductPurchase, "pack");
            economy.TryDebit(TransactionReason.ProductPurchase, new[] { new TransactionLine("pack", 3, 50) });

            Assert.That(raised, Is.EqualTo(0));
            Assert.That(economy.Ledger, Is.Empty);
            Assert.That(economy.BalanceCents, Is.EqualTo(100));
        }

        [Test]
        public void TryDebit_Success_RaisesOnceWithDeltaAndReason()
        {
            var economy = new EconomyService(StartingCents);
            var changes = new List<BalanceChange>();
            economy.BalanceChanged += changes.Add;

            economy.TryDebit(1_250, TransactionReason.ProductPurchase, "pack");

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].OldCents, Is.EqualTo(StartingCents));
            Assert.That(changes[0].NewCents, Is.EqualTo(StartingCents - 1_250));
            Assert.That(changes[0].DeltaCents, Is.EqualTo(-1_250));
            Assert.That(changes[0].Reason, Is.EqualTo(TransactionReason.ProductPurchase));
        }

        [Test]
        public void TryDebit_Lines_RaisesOnceWithTotalDeltaAndReason()
        {
            var economy = new EconomyService(StartingCents);
            var changes = new List<BalanceChange>();
            economy.BalanceChanged += changes.Add;

            bool isPaid = economy.TryDebit(
                TransactionReason.ProductPurchase,
                new[] { new TransactionLine("SetA_Pack", 12, 500), new TransactionLine("SetB_Pack", 3, 900) });

            Assert.That(isPaid, Is.True);
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].DeltaCents, Is.EqualTo(-8_700));
            Assert.That(changes[0].Reason, Is.EqualTo(TransactionReason.ProductPurchase));
            Assert.That(economy.BalanceCents, Is.EqualTo(StartingCents - 8_700));
        }

        [Test]
        public void TryDebit_Lines_OneLedgerEntryPerLineWithUnitPrice()
        {
            var economy = new EconomyService(StartingCents);

            economy.TryDebit(
                TransactionReason.ProductPurchase,
                new[] { new TransactionLine("SetA_Pack", 12, 500), new TransactionLine("SetB_Pack", 3, 900) });

            Assert.That(economy.Ledger.Count, Is.EqualTo(2));
            Assert.That(economy.Ledger[0].ItemId, Is.EqualTo("SetA_Pack"));
            Assert.That(economy.Ledger[0].Quantity, Is.EqualTo(12));
            Assert.That(economy.Ledger[0].UnitPriceCents, Is.EqualTo(500));
            Assert.That(economy.Ledger[0].AmountCents, Is.EqualTo(-6_000));
            Assert.That(economy.Ledger[0].BalanceAfterCents, Is.EqualTo(StartingCents - 6_000));
            Assert.That(economy.Ledger[1].ItemId, Is.EqualTo("SetB_Pack"));
            Assert.That(economy.Ledger[1].AmountCents, Is.EqualTo(-2_700));
            Assert.That(economy.Ledger[1].BalanceAfterCents, Is.EqualTo(StartingCents - 8_700));
        }

        [Test]
        public void TryDebit_LinesOneCentOverBalance_AllOrNothing()
        {
            var economy = new EconomyService(8_699);

            bool isPaid = economy.TryDebit(
                TransactionReason.ProductPurchase,
                new[] { new TransactionLine("SetA_Pack", 12, 500), new TransactionLine("SetB_Pack", 3, 900) });

            Assert.That(isPaid, Is.False);
            Assert.That(economy.BalanceCents, Is.EqualTo(8_699));
            Assert.That(economy.Ledger, Is.Empty);
        }

        [Test]
        public void TryDebit_NegativeAmount_Throws()
        {
            var economy = new EconomyService(StartingCents);

            Assert.Throws<ArgumentOutOfRangeException>(() => economy.TryDebit(-1, TransactionReason.Debug, "x"));
        }

        [Test]
        public void TryDebit_NoLinesOrBadLine_Throws()
        {
            var economy = new EconomyService(StartingCents);

            Assert.Throws<ArgumentException>(() => economy.TryDebit(TransactionReason.ProductPurchase, Array.Empty<TransactionLine>()));
            Assert.Throws<ArgumentNullException>(() => economy.TryDebit(TransactionReason.ProductPurchase, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionLine("x", 0, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionLine("x", 1, -1));
        }

        [Test]
        public void Credit_RaisesWithPositiveDelta()
        {
            var economy = new EconomyService(StartingCents);
            var changes = new List<BalanceChange>();
            economy.BalanceChanged += changes.Add;

            economy.Credit(1_400, TransactionReason.CardSale, "RC_C_001");

            Assert.That(economy.BalanceCents, Is.EqualTo(51_400));
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].DeltaCents, Is.EqualTo(1_400));
            Assert.That(changes[0].Reason, Is.EqualTo(TransactionReason.CardSale));
            Assert.That(economy.Ledger[0].AmountCents, Is.EqualTo(1_400));
            Assert.That(economy.Ledger[0].ItemId, Is.EqualTo("RC_C_001"));
        }

        [Test]
        public void Credit_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EconomyService(StartingCents).Credit(-5, TransactionReason.Debug, "x"));
        }

        [Test]
        public void Ledger_IsReadOnlyForCallers()
        {
            var economy = new EconomyService(StartingCents);
            economy.Credit(1, TransactionReason.Debug, "x");

            Assert.That(economy.Ledger, Is.Not.InstanceOf<List<Transaction>>());
        }
    }
}
