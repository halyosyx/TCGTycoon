using System;
using System.Collections.Generic;

namespace Game.Core.Store
{
    /// <summary>What <see cref="StoreService.PlaceOrder"/> did: success or the failed check, and the lines ordered.</summary>
    public sealed class OrderResult
    {
        private static readonly IReadOnlyList<CartLine> s_noLines = Array.Empty<CartLine>();

        private OrderResult(OrderCheck check, IReadOnlyList<CartLine> lines, long totalCents)
        {
            Check = check;
            Lines = lines;
            TotalCents = totalCents;
        }

        public bool IsSuccess => Check == OrderCheck.Ok;

        public OrderCheck Check { get; }

        /// <summary>The ordered lines, in cart order; empty on failure.</summary>
        public IReadOnlyList<CartLine> Lines { get; }

        public long TotalCents { get; }

        internal static OrderResult Failed(OrderCheck check) => new OrderResult(check, s_noLines, 0);

        internal static OrderResult Placed(IReadOnlyList<CartLine> lines, long totalCents) => new OrderResult(OrderCheck.Ok, lines, totalCents);
    }
}
