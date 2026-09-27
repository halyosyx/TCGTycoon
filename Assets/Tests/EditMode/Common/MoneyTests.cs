using Game.Core.Common;
using NUnit.Framework;

namespace Game.Core.Tests.Common
{
    public sealed class MoneyTests
    {
        [TestCase(425, "$4.25")]
        [TestCase(5, "$0.05")]
        [TestCase(0, "$0.00")]
        [TestCase(123456, "$1234.56")]
        public void Format_NonNegativeCents_ShowsDollarsAndCents(long cents, string expected)
        {
            Assert.That(Money.Format(cents), Is.EqualTo(expected));
        }

        [Test]
        public void Format_NegativeCents_PutsSignBeforeDollarSign()
        {
            Assert.That(Money.Format(-60), Is.EqualTo("-$0.60"));
        }

        [Test]
        public void FormatAverage_FractionalCents_RoundsToNearestCent()
        {
            Assert.That(Money.FormatAverage(362.4), Is.EqualTo("$3.62"));
        }
    }
}
