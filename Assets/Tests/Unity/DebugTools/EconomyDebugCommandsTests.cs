using Game.Core.Economy;
using Game.Unity.DebugTools;
using NUnit.Framework;

namespace Game.Unity.Tests.DebugTools
{
    public sealed class EconomyDebugCommandsTests
    {
        [TestCase("money.set 100", true)]
        [TestCase("  MONEY.add 5", true)]
        [TestCase("buy SetA_Pack 1", false)]
        [TestCase(null, false)]
        public void Handles_CommandLine_OnlyMoneyPrefix(string commandLine, bool expected)
        {
            Assert.That(EconomyDebugCommands.Handles(commandLine), Is.EqualTo(expected));
        }

        [TestCase(120_000L, 70_000L)]
        [TestCase(0L, -50_000L)]
        public void Execute_Set_ChangesBalanceThroughOneDebugEntry(long target, long expectedDelta)
        {
            var economy = new EconomyService(50_000);

            new EconomyDebugCommands(economy).Execute($"money.set {target}");

            Assert.That(economy.BalanceCents, Is.EqualTo(target));
            Assert.That(economy.Ledger.Count, Is.EqualTo(1));
            Assert.That(economy.Ledger[0].Reason, Is.EqualTo(TransactionReason.Debug));
            Assert.That(economy.Ledger[0].AmountCents, Is.EqualTo(expectedDelta));
        }

        [Test]
        public void Execute_Add_RaisesBalanceChangedWithDebugReason()
        {
            var economy = new EconomyService(50_000);
            BalanceChange? change = null;
            economy.BalanceChanged += raised => change = raised;

            string output = new EconomyDebugCommands(economy).Execute("money.add 1000");

            Assert.That(economy.BalanceCents, Is.EqualTo(51_000));
            Assert.That(change.HasValue, Is.True);
            Assert.That(change.Value.DeltaCents, Is.EqualTo(1_000));
            Assert.That(change.Value.Reason, Is.EqualTo(TransactionReason.Debug));
            Assert.That(output, Does.Contain("$510.00"));
        }

        [Test]
        public void Execute_AddNegativeBeyondBalance_ChangesNothing()
        {
            var economy = new EconomyService(500);

            string output = new EconomyDebugCommands(economy).Execute("money.add -501");

            Assert.That(output, Does.StartWith("Can't take"));
            Assert.That(economy.BalanceCents, Is.EqualTo(500));
            Assert.That(economy.Ledger, Is.Empty);
        }

        [TestCase("money.set -1")]
        [TestCase("money.set")]
        [TestCase("money.add 0")]
        [TestCase("money.add lots")]
        public void Execute_BadArguments_UsageAndNoChange(string commandLine)
        {
            var economy = new EconomyService(500);

            string output = new EconomyDebugCommands(economy).Execute(commandLine);

            Assert.That(output, Does.StartWith("Usage"));
            Assert.That(economy.Ledger, Is.Empty);
        }

        [Test]
        public void Execute_SetToCurrentBalance_NoLedgerEntry()
        {
            var economy = new EconomyService(500);

            new EconomyDebugCommands(economy).Execute("money.set 500");

            Assert.That(economy.Ledger, Is.Empty);
        }
    }
}
