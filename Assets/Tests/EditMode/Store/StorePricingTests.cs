using System;
using Game.Core.Store;
using NUnit.Framework;

namespace Game.Core.Tests.Store
{
    public sealed class StorePricingTests
    {
        [TestCase(500L, 100, 500L)]
        [TestCase(500L, 90, 450L)]
        [TestCase(333L, 50, 167L)]   // 166.5 rounds up
        [TestCase(1L, 50, 1L)]       // 0.5 rounds up
        [TestCase(999L, 33, 330L)]   // 329.67
        [TestCase(101L, 49, 49L)]    // 49.49 rounds down
        [TestCase(0L, 100, 0L)]
        [TestCase(900L, 0, 0L)]
        [TestCase(16_000L, 90, 14_400L)]
        public void UnitPriceCents_RoundsHalfUp(long marketCents, int supplierPercent, long expectedCents)
        {
            Assert.That(StorePricing.UnitPriceCents(marketCents, supplierPercent), Is.EqualTo(expectedCents));
        }

        [Test]
        public void UnitPriceCents_NegativeInput_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StorePricing.UnitPriceCents(-1, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => StorePricing.UnitPriceCents(100, -1));
        }
    }
}
