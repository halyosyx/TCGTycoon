using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>One row of <c>TierPrices.csv</c>: a tier's in-print base price and its volatility.</summary>
    public sealed class TierPriceEntry
    {
        public TierPriceEntry(RarityTier tier, long basePriceCents, VolatilityTier volatility, int lineNumber = 0)
        {
            Tier = tier;
            BasePriceCents = basePriceCents;
            Volatility = volatility;
            LineNumber = lineNumber;
        }

        public RarityTier Tier { get; }

        public long BasePriceCents { get; }

        public VolatilityTier Volatility { get; }

        /// <summary>Line in TierPrices.csv, for error messages; 0 when built in code.</summary>
        public int LineNumber { get; }
    }
}
