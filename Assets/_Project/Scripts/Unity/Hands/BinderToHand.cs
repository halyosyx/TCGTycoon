using System;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.UI;

namespace Game.Unity.Hands
{
    /// <summary>
    /// <see cref="IBinderActions"/> for the player's hand: the binder takes one copy of a card into the
    /// hand (RMB on a pocket, or T) and puts the last-taken card back (Shift+RMB), through
    /// <see cref="HandCards"/>. Core decides whether it fits (ten cards, never alongside a pack).
    /// </summary>
    public sealed class BinderToHand : IBinderActions
    {
        private readonly CardPool _cards;
        private readonly HandCards _handCards;

        public BinderToHand(CardPool cards, HandCards handCards)
        {
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            _handCards = handCards ?? throw new ArgumentNullException(nameof(handCards));
        }

        public int HeldCardCount => _handCards.HeldCount;

        public int HeldCardCapacity => _handCards.Capacity;

        public bool TryTakeToHand(string itemId)
        {
            // Only single cards come out of the binder this way.
            return _cards.TryGetCard(itemId, out Card card) && _handCards.TryTake(ItemRef.Card(card.Id, card.Tier), ItemLocation.Binder);
        }

        public bool TryPutBackToBinder() => _handCards.TryPutBackLast(ItemLocation.Binder);
    }
}
