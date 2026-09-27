using System;
using Game.Core.Common;
using NUnit.Framework;

namespace Game.Core.Tests.Common
{
    public sealed class SeededRngTests
    {
        private const int SequenceLength = 1_000;
        private const long Maximum = 1_000_000;

        [Test]
        public void NextLong_SameSeed_ProducesSameSequence()
        {
            var first = new SeededRng(42);
            var second = new SeededRng(42);

            for (int i = 0; i < SequenceLength; i++)
            {
                Assert.That(second.NextLong(Maximum), Is.EqualTo(first.NextLong(Maximum)), $"Values differ at index {i}.");
            }
        }

        [Test]
        public void NextLong_DifferentSeeds_ProduceDifferentSequences()
        {
            var first = new SeededRng(1);
            var second = new SeededRng(2);
            int matches = 0;

            for (int i = 0; i < SequenceLength; i++)
            {
                if (first.NextLong(Maximum) == second.NextLong(Maximum)) matches++;
            }

            Assert.That(matches, Is.LessThan(5));
        }

        [Test]
        public void NextLong_SmallMaximum_StaysInRangeAndHitsEveryValue()
        {
            const int smallMaximum = 7;
            var rng = new SeededRng(7);
            var seen = new bool[smallMaximum];

            for (int i = 0; i < 10_000; i++)
            {
                long value = rng.NextLong(smallMaximum);
                Assert.That(value, Is.InRange(0, smallMaximum - 1));
                seen[value] = true;
            }

            Assert.That(seen, Is.All.True);
        }

        [Test]
        public void FromState_MidSequence_ContinuesIdentically()
        {
            var original = new SeededRng(99);
            for (int i = 0; i < 10; i++) original.NextLong(Maximum);

            SeededRng restored = SeededRng.FromState(original.State);

            for (int i = 0; i < SequenceLength; i++)
            {
                Assert.That(restored.NextLong(Maximum), Is.EqualTo(original.NextLong(Maximum)));
            }
        }

        [Test]
        public void NextLong_NonPositiveMaximum_Throws()
        {
            var rng = new SeededRng(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextLong(0));
        }
    }
}
