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

        // The real minus sign (U+2212): the UI style guide uses it for negative money, because a
        // hyphen reads as a dash at display sizes.
        private const string DisplayMinus = "−";
        private const string DisplayPlus = "+";

        /// <summary>Formats cents as dollars for tools and logs: 425 → "$4.25", -60 → "-$0.60".</summary>
        public static string Format(long cents)
        {
            string sign = cents < 0 ? "-" : string.Empty;
            return string.Format(CultureInfo.InvariantCulture, "{0}${1}.{2:00}", sign, Dollars(cents), RemainderCents(cents));
        }

        /// <summary>
        /// Formats cents for players: thousands grouped and a real minus sign.
        /// 128450 → "$1,284.50", -15000 → "−$150.00".
        /// </summary>
        public static string FormatDisplay(long cents)
        {
            string sign = cents < 0 ? DisplayMinus : string.Empty;
            return string.Format(CultureInfo.InvariantCulture, "{0}${1:N0}.{2:00}", sign, Dollars(cents), RemainderCents(cents));
        }

        /// <summary>
        /// Formats a change in money for players, always signed so it never relies on colour:
        /// 1400 → "+$14.00", -15000 → "−$150.00".
        /// </summary>
        public static string FormatDelta(long cents)
        {
            return cents < 0 ? FormatDisplay(cents) : DisplayPlus + FormatDisplay(cents);
        }

        /// <summary>
        /// Formats a statistic measured in cents (an average or expected value), rounded to the
        /// nearest cent. Only for display: money itself is never stored as a <see cref="double"/>.
        /// </summary>
        public static string FormatAverage(double averageCents)
        {
            return Format((long)Math.Round(averageCents, MidpointRounding.AwayFromZero));
        }

        // Unsigned magnitude avoids overflow on long.MinValue.
        private static ulong Magnitude(long cents) => cents < 0 ? unchecked((ulong)(-(cents + 1))) + 1UL : (ulong)cents;

        private static ulong Dollars(long cents) => Magnitude(cents) / CentsPerDollar;

        private static ulong RemainderCents(long cents) => Magnitude(cents) % CentsPerDollar;
    }
}
