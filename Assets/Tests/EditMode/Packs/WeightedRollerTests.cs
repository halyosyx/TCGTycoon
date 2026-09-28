using System;
using Game.Core.Common;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Packs
{
    public sealed class WeightedRollerTests
    {
        [Test]
        public void Roll_BoundaryRolls_PicksItemsByCumulativeWeight()
        {
            // Weights 2, 0, 3: rolls 0-1 pick A, 2-4 pick C, B (weight 0) owns no rolls.
            var roller = new WeightedRoller<string>(new[] { "A", "B", "C" }, new[] { 2, 0, 3 });
            var rng = new ScriptedRng(0, 1, 2, 4);

            Assert.That(roller.Roll(rng), Is.EqualTo("A"));
            Assert.That(roller.Roll(rng), Is.EqualTo("A"));
            Assert.That(roller.Roll(rng), Is.EqualTo("C"));
            Assert.That(roller.Roll(rng), Is.EqualTo("C"));
        }

        [Test]
        public void Roll_ZeroWeightItem_NeverRolled()
        {
            var roller = new WeightedRoller<string>(new[] { "A", "Never", "C" }, new[] { 1, 0, 1 });
            var rng = new SeededRng(3);

            for (int i = 0; i < 100_000; i++)
            {
                Assert.That(roller.Roll(rng), Is.Not.EqualTo("Never"));
            }
        }

        [Test]
        public void Roll_SingleItem_AlwaysReturnsIt()
        {
            var roller = new WeightedRoller<string>(new[] { "Only" }, new[] { 7 });
            var rng = new SeededRng(5);

            for (int i = 0; i < 1_000; i++)
            {
                Assert.That(roller.Roll(rng), Is.EqualTo("Only"));
            }
        }

        [Test]
        public void TotalWeight_SeveralItems_IsSumOfWeights()
        {
            var roller = new WeightedRoller<string>(new[] { "A", "B" }, new[] { 90, 10 });

            Assert.That(roller.TotalWeight, Is.EqualTo(100));
        }

        [Test]
        public void Constructor_NegativeWeight_Throws()
        {
            Assert.Throws<ArgumentException>(() => new WeightedRoller<string>(new[] { "A", "B" }, new[] { 5, -1 }));
        }

        [Test]
        public void Constructor_AllZeroWeights_Throws()
        {
            Assert.Throws<ArgumentException>(() => new WeightedRoller<string>(new[] { "A", "B" }, new[] { 0, 0 }));
        }

        [Test]
        public void Constructor_MismatchedCounts_Throws()
        {
            Assert.Throws<ArgumentException>(() => new WeightedRoller<string>(new[] { "A", "B" }, new[] { 1 }));
        }
    }
}
