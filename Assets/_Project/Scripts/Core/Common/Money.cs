using System;
using System.Globalization;

namespace Game.Core.Common
{
    /// <summary>
    /// Formatting for money, which the game always stores as whole cents in a <see cref="long"/>.
    /// The single place currency is turned into text, so the format stays consistent.
    /// </summary>
    public static class Money
    {
        private const ulong CentsPerDollar = 100;

        /// <summary>Formats cents as dollars: 425 → "$4.25", -60 → "-$0.60".</summary>
        public static string Format(long cents)
        {
            // Unsigned magnitude avoids overflow on long.MinValue.
            ulong magnitude = cents < 0 ? unchecked((ulong)(-(cents + 1))) + 1UL : (ulong)cents;
            string sign = cents < 0 ? "-" : string.Empty;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}${1}.{2:00}",
                sign,
                magnitude / CentsPerDollar,
                magnitude % CentsPerDollar);
        }

        /// <summary>
        /// Formats a statistic measured in cents (an average or expected value), rounded to the
        /// nearest cent. Only for display: money itself is never stored as a <see cref="double"/>.
        /// </summary>
        public static string FormatAverage(double averageCents)
        {
            return Format((long)Math.Round(averageCents, MidpointRounding.AwayFromZero));
        }
    }
}
