using System;

namespace Game.Core.Store
{
    /// <summary>
    /// The one place a store price is derived: market price × supplier percent, rounded half up to
    /// whole cents. Read prices through <see cref="StoreService.GetUnitPriceCents"/> so the F4 market
    /// can change the source of the market price without touching callers.
    /// </summary>
    public static class StorePricing
    {
        private const long PercentScale = 100;

        public static long UnitPriceCents(long marketPriceCents, int supplierPercent)
        {
            if (marketPriceCents < 0) throw new ArgumentOutOfRangeException(nameof(marketPriceCents), marketPriceCents, "A market price can't be negative.");
            if (supplierPercent < 0) throw new ArgumentOutOfRangeException(nameof(supplierPercent), supplierPercent, "A supplier percent can't be negative.");

            // Both operands are non-negative, so adding half the divisor rounds half up.
            return checked(marketPriceCents * supplierPercent + PercentScale / 2) / PercentScale;
        }
    }
}
