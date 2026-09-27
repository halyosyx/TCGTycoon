using System;
using System.Collections.Generic;

namespace Game.Core.Content
{
    /// <summary>The cards a pack can contain, grouped by rarity tier for fast lookup.</summary>
    public sealed class CardPool
    {
        private static readonly IReadOnlyList<Card> s_noCards = Array.Empty<Card>();

        private readonly Dictionary<RarityTier, List<Card>> _cardsByTier = new Dictionary<RarityTier, List<Card>>();

        public CardPool(IEnumerable<Card> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var allCards = new List<Card>();
            foreach (Card card in cards)
            {
                if (card == null)
                {
                    throw new ArgumentException("The card list contains an empty entry.", nameof(cards));
                }

                allCards.Add(card);
                if (!_cardsByTier.TryGetValue(card.Tier, out List<Card> tierCards))
                {
                    tierCards = new List<Card>();
                    _cardsByTier.Add(card.Tier, tierCards);
                }

                tierCards.Add(card);
            }

            Cards = allCards.AsReadOnly();
        }

        /// <summary>Every card in the pool, in the order given.</summary>
        public IReadOnlyList<Card> Cards { get; }

        /// <summary>The pool's cards of one tier; empty when there are none.</summary>
        public IReadOnlyList<Card> CardsOf(RarityTier tier)
        {
            return _cardsByTier.TryGetValue(tier, out List<Card> tierCards) ? tierCards : s_noCards;
        }

        public bool HasCards(RarityTier tier) => CardsOf(tier).Count > 0;

        /// <summary>
        /// Mean value of the tier's cards in cents. Cards are picked uniformly within a tier, so this is
        /// the tier's expected pull value. A statistic, not stored money, hence <see cref="double"/>.
        /// </summary>
        public double AverageValueCents(RarityTier tier)
        {
            IReadOnlyList<Card> tierCards = CardsOf(tier);
            if (tierCards.Count == 0)
            {
                return 0d;
            }

            long totalCents = 0;
            foreach (Card card in tierCards)
            {
                totalCents += card.ValueCents;
            }

            return (double)totalCents / tierCards.Count;
        }
    }
}
