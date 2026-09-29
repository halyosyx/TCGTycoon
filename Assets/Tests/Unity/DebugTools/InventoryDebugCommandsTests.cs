using Game.Core.Content;
using Game.Core.Session;
using Game.Unity.DebugTools;
using NUnit.Framework;

namespace Game.Unity.Tests.DebugTools
{
    public sealed class InventoryDebugCommandsTests
    {
        [TestCase("inventory.sample", true)]
        [TestCase("  Inventory.Sample", true)]
        [TestCase("inventory", false)]
        [TestCase("open 5", false)]
        public void Handles_CommandLine_OnlyInventoryDotPrefix(string commandLine, bool expected)
        {
            Assert.That(InventoryDebugCommands.Handles(commandLine), Is.EqualTo(expected));
        }

        [Test]
        public void Execute_Sample_GrantsEveryCardWithExtraCopiesOfCommonsUncommonsAndOneHit()
        {
            GameSession session = CreateSession();

            new InventoryDebugCommands(session).Execute("inventory.sample");

            Assert.That(session.Inventory.CountOf("C1", RarityTier.Common), Is.EqualTo(3));
            Assert.That(session.Inventory.CountOf("U1", RarityTier.Uncommon), Is.EqualTo(2));
            Assert.That(session.Inventory.CountOf("R1", RarityTier.Rare), Is.EqualTo(2));
            Assert.That(session.Inventory.CountOf("R2", RarityTier.Rare), Is.EqualTo(1));
            Assert.That(session.Inventory.CountOf("S1", RarityTier.SpecialIllustration), Is.EqualTo(1));
        }

        [Test]
        public void Execute_UnknownCommand_ChangesNothing()
        {
            GameSession session = CreateSession();

            string output = new InventoryDebugCommands(session).Execute("inventory.clear");

            Assert.That(output, Does.StartWith("Unknown"));
            Assert.That(session.Inventory.Stacks, Is.Empty);
        }

        // A pack whose slots match the pool's tiers, so the session validates.
        private static GameSession CreateSession()
        {
            var pool = new CardPool(new[]
            {
                new Card("C1", "Common", "SetA", RarityTier.Common, 5),
                new Card("U1", "Uncommon", "SetA", RarityTier.Uncommon, 15),
                new Card("R1", "Rare one", "SetA", RarityTier.Rare, 60),
                new Card("R2", "Rare two", "SetA", RarityTier.Rare, 60),
                new Card("S1", "Special", "SetA", RarityTier.SpecialIllustration, 9000),
            });
            var pack = new PackConfig("test-pack", "Test Pack", 425, new[]
            {
                new PackSlot(new[] { new TierWeight(RarityTier.Common, 1) }),
                new PackSlot(new[] { new TierWeight(RarityTier.Rare, 1) }),
            });
            return new GameSession(pack, pool, 1);
        }
    }
}
