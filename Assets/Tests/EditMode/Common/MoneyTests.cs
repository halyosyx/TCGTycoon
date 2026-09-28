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

        [TestCase(0, "$0.00")]
        [TestCase(5, "$0.05")]
        [TestCase(128450, "$1,284.50")]
        [TestCase(123456789, "$1,234,567.89")]
        public void FormatDisplay_NonNegativeCents_GroupsThousands(long cents, string expected)
        {
            Assert.That(Money.FormatDisplay(cents), Is.EqualTo(expected));
        }

        [Test]
        public void FormatDisplay_NegativeCents_UsesRealMinusSign()
        {
            Assert.That(Money.FormatDisplay(-15000), Is.EqualTo("−$150.00"));
        }

        [Test]
        public void FormatDisplay_MinimumLong_DoesNotOverflow()
        {
            Assert.That(Money.FormatDisplay(long.MinValue), Does.StartWith("−$92,233,720,368,547,758.08"));
        }

        [TestCase(1400, "+$14.00")]
        [TestCase(0, "+$0.00")]
        [TestCase(-15000, "−$150.00")]
        [TestCase(250000, "+$2,500.00")]
        public void FormatDelta_AnyCents_AlwaysCarriesASign(long cents, string expected)
        {
            Assert.That(Money.FormatDelta(cents), Is.EqualTo(expected));
        }
    }
}
