using System;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.UI;

namespace Game.Unity.Hands
{
    /// <summary>
    /// <see cref="IBinderActions"/> for the player's hand: T in the binder moves one copy of the selected
    /// card into the hand (Binder to Held) and shows it on the <see cref="CardStack"/>, starting one if the
    /// hand is empty. Core decides whether it fits (ten cards, never alongside a pack).
    /// </summary>
    public sealed class BinderToHand : IBinderActions
    {
        private readonly InventoryService _inventory;
        private readonly CardPool _cards;
        private readonly PlayerHands _hands;
        private readonly HoldableFactory _holdables;

        public BinderToHand(InventoryService inventory, CardPool cards, PlayerHands hands, HoldableFactory holdables)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            _hands = hands ?? throw new ArgumentNullException(nameof(hands));
            _holdables = holdables ?? throw new ArgumentNullException(nameof(holdables));
        }

        public bool TryTakeToHand(string itemId)
        {
            // Only single cards come out of the binder this way; the hand may hold a card stack or nothing.
            if (!_cards.TryGetCard(itemId, out Card card) || (!_hands.IsEmpty && !(_hands.Held is CardStack)))
            {
                return false;
            }

            if (!_inventory.Move(ItemRef.Card(card.Id, card.Tier), ItemLocation.Binder, ItemLocation.Held).IsSuccess)
            {
                return false;
            }

            if (_hands.IsEmpty)
            {
                _hands.Hold(_holdables.GetCardStack());
            }

            return true;
        }
    }
}
