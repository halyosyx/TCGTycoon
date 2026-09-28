using System;

namespace Game.Core.Tests.TestUtilities
{
    /// <summary>
    /// Statistical tolerances for probabilistic tests. Five standard deviations: a correct
    /// implementation fails a single check with probability ≈ 6 × 10⁻⁷, so the suite never flakes,
    /// while a wrong weight is still caught (≈ 0.7 points on a 25% tier at 100,000 samples).
    /// </summary>
    public static class Tolerance
    {
        public const double SigmaMultiplier = 5d;

        /// <summary>Allowed absolute error for an observed rate with true probability <paramref name="probability"/> over <paramref name="sampleCount"/> samples.</summary>
        public static double ForRate(double probability, long sampleCount)
        {
            return SigmaMultiplier * Math.Sqrt(probability * (1d - probability) / sampleCount);
        }

        /// <summary>Allowed absolute error for an observed mean, given its standard error.</summary>
        public static double ForMean(double standardError) => SigmaMultiplier * standardError;
    }
}
