using System;
using System.Collections.Generic;
using Game.Core.Common;

namespace Game.Core.Packs
{
    /// <summary>
    /// Picks one item at random with probability proportional to its integer weight. Weights are
    /// relative (they never need to sum to 100). Build once and reuse: rolling allocates nothing.
    /// </summary>
    /// <typeparam name="TItem">What is being picked, e.g. a rarity tier or a customer archetype.</typeparam>
    public sealed class WeightedRoller<TItem>
    {
        private readonly TItem[] _items;
        private readonly long[] _cumulativeWeights;

        public WeightedRoller(IReadOnlyList<TItem> items, IReadOnlyList<int> weights)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (items.Count != weights.Count)
            {
                throw new ArgumentException($"Got {items.Count} items but {weights.Count} weights.", nameof(weights));
            }

            if (items.Count == 0)
            {
                throw new ArgumentException("A roller needs at least one item.", nameof(items));
            }

            _items = new TItem[items.Count];
            _cumulativeWeights = new long[items.Count];
            long runningTotal = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (weights[i] < 0)
                {
                    throw new ArgumentException($"Weight at index {i} is negative ({weights[i]}).", nameof(weights));
                }

                runningTotal += weights[i];
                _items[i] = items[i];
                _cumulativeWeights[i] = runningTotal;
            }

            if (runningTotal == 0)
            {
                throw new ArgumentException("At least one weight must be positive.", nameof(weights));
            }

            TotalWeight = runningTotal;
        }

        /// <summary>Sum of all weights.</summary>
        public long TotalWeight { get; }

        /// <summary>Picks an item: a roll in [0, total) selects the first item whose cumulative weight exceeds it.</summary>
        public TItem Roll(IRng rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            long roll = rng.NextLong(TotalWeight);

            // A zero-weight item has the same cumulative total as the item before it, so no roll can select it.
            for (int i = 0; i < _cumulativeWeights.Length; i++)
            {
                if (roll < _cumulativeWeights[i])
                {
                    return _items[i];
                }
            }

            throw new InvalidOperationException("Roll exceeded the total weight; this can't happen with a valid IRng.");
        }
    }
}
