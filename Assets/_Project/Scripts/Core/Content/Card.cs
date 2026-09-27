using System;

namespace Game.Core.Content
{
    /// <summary>
    /// One card in a set: shared, immutable data (flyweight). Owned copies refer to it by
    /// <see cref="Id"/> and never copy its fields.
    /// </summary>
    public sealed class Card
    {
        public Card(string id, string displayName, string setId, RarityTier tier, long valueCents)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A card needs an id.", nameof(id));
            }

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            SetId = setId ?? string.Empty;
            Tier = tier;
            ValueCents = valueCents;
        }

        /// <summary>Stable id used by inventory and, later, saves.</summary>
        public string Id { get; }

        public string DisplayName { get; }

        public string SetId { get; }

        public RarityTier Tier { get; }

        /// <summary>Value in cents used for expected-value calculations until market prices exist (F4).</summary>
        public long ValueCents { get; }
    }
}
