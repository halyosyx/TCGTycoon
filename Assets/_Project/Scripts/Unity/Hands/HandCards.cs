using System;
using System.Collections.Generic;
using Game.Core.Inventory;

namespace Game.Unity.Hands
{
    /// <summary>
    /// Single cards in and out of the player's hand, shared by everything that moves them: the binder
    /// (take one, put the last one back) and the display case (take one, place them all). Every change
    /// is a Core move; this keeps the hand's <see cref="CardStack"/> in step: it starts when the first card
    /// arrives and goes away when the last one leaves.
    /// </summary>
    public sealed class HandCards
    {
        private readonly InventoryService _inventory;
        private readonly PlayerHands _hands;
        private readonly HoldableFactory _holdables;

        public HandCards(InventoryService inventory, PlayerHands hands, HoldableFactory holdables)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _hands = hands ?? throw new ArgumentNullException(nameof(hands));
            _holdables = holdables ?? throw new ArgumentNullException(nameof(holdables));
        }

        /// <summary>True while the hand holds single cards (not a pack, not nothing).</summary>
        public bool IsHoldingCards => _hands.Held is CardStack;

        /// <summary>Single cards in the hand.</summary>
        public int HeldCount => IsHoldingCards ? _inventory.CountIn(ItemLocation.Held) : 0;

        public int Capacity => _inventory.Capacities.HeldCards;

        /// <summary>The hand can take one more card: empty, or a card stack with room.</summary>
        public bool CanTakeMore => (_hands.IsEmpty || IsHoldingCards) && HeldCount < Capacity;

        /// <summary>Moves one copy of <paramref name="card"/> from <paramref name="from"/> into the hand.</summary>
        public bool TryTake(ItemRef card, ItemLocation from)
        {
            if (card.Kind != ItemKind.Card || !CanTakeMore)
            {
                return false;
            }

            if (!_inventory.Move(card, from, ItemLocation.Held).IsSuccess)
            {
                return false;
            }

            if (_hands.IsEmpty)
            {
                _hands.Hold(_holdables.GetCardStack());
            }

            return true;
        }

        /// <summary>Puts the most recently taken card (the one on top of the stack) back at <paramref name="to"/>.</summary>
        public bool TryPutBackLast(ItemLocation to)
        {
            if (!IsHoldingCards || !TryFindLastHeld(_inventory.Stacks, out ItemRef card))
            {
                return false;
            }

            bool isMoved = _inventory.Move(card, ItemLocation.Held, to).IsSuccess;
            ReleaseIfEmpty();
            return isMoved;
        }

        /// <summary>Places every held card at <paramref name="to"/>, as many as fit; the rest stay in the hand.</summary>
        public CardsMoveResult PlaceAll(ItemLocation to)
        {
            if (!IsHoldingCards)
            {
                return new CardsMoveResult(0, 0, MoveFailure.NotOwned);
            }

            CardsMoveResult result = _inventory.MoveAllCards(ItemLocation.Held, to);
            ReleaseIfEmpty();
            return result;
        }

        /// <summary>The card on top of the held stack: the last held stack in Core order.</summary>
        public static bool TryFindLastHeld(IReadOnlyList<InventoryStack> stacks, out ItemRef card)
        {
            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                if (stacks[i].Location == ItemLocation.Held)
                {
                    card = ItemRef.Card(stacks[i].CardId, stacks[i].Tier);
                    return true;
                }
            }

            card = default;
            return false;
        }

        // The last card left the hand some way other than Put down: the stack goes too.
        private void ReleaseIfEmpty()
        {
            if (_hands.Held is CardStack stack && _inventory.CountIn(ItemLocation.Held) == 0)
            {
                _hands.Clear();
                _holdables.ReleaseCardStack(stack);
            }
        }
    }
}
