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
            Assert.That(session.Inventory.CountOf("H1", RarityTier.HoloFullArt), Is.EqualTo(2));
            Assert.That(session.Inventory.CountOf("H2", RarityTier.HoloFullArt), Is.EqualTo(1));
            Assert.That(session.Inventory.CountOf("S1", RarityTier.SpecialFullArtHolo), Is.EqualTo(1));
        }

        [Test]
        public void Execute_Where_ListsUnitsPerLocation()
        {
            GameSession session = CreateSession();
            new InventoryDebugCommands(session).Execute("inventory.sample");
            session.Inventory.Move(Game.Core.Inventory.ItemRef.Card("C1", RarityTier.Common), Game.Core.Inventory.ItemLocation.Binder, Game.Core.Inventory.ItemLocation.Held, 2);

            string output = new InventoryDebugCommands(session).Execute("inventory.where");

            Assert.That(output, Does.Contain("Held: 2"));
            Assert.That(output, Does.Contain("Binder: " + (session.Inventory.TotalItemCount - 2)));
            Assert.That(output, Does.Contain("DisplayCase: 0"));
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
                new Card("H1", "Holo one", "SetA", RarityTier.HoloFullArt, 240),
                new Card("H2", "Holo two", "SetA", RarityTier.HoloFullArt, 240),
                new Card("S1", "Special", "SetA", RarityTier.SpecialFullArtHolo, 7000),
            });
            var pack = new PackConfig("test-pack", "Test Pack", 425, new[]
            {
                new PackSlot(new[] { new TierWeight(RarityTier.Common, 1) }),
                new PackSlot(new[] { new TierWeight(RarityTier.HoloFullArt, 1) }),
            });
            return new GameSession(pack, pool, 1);
        }
    }
}
