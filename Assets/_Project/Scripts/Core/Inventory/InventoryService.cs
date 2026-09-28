using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Packs;

namespace Game.Core.Inventory
{
    /// <summary>
    /// The player's owned cards, stacked by (card id, tier), with each stack's cost basis in cents.
    /// All changes go through this service; its state is plain data ready for a future save.
    /// </summary>
    public sealed class InventoryService
    {
        private readonly InventoryState _state;

        public InventoryService()
            : this(new InventoryState())
        {
        }

        public InventoryService(InventoryState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Current stacks, in the order they were first acquired. Read-only for callers.</summary>
        public IReadOnlyList<InventoryStack> Stacks => _state.Stacks;

        public long TotalCostBasisCents
        {
            get
            {
                long totalCents = 0;
                foreach (InventoryStack stack in _state.Stacks)
                {
                    totalCents += stack.CostBasisCents;
                }

                return totalCents;
            }
        }

        public int CountOf(string cardId, RarityTier tier)
        {
            InventoryStack stack = Find(cardId, tier);
            return stack == null ? 0 : stack.Count;
        }

        /// <summary>Adds one copy of <paramref name="card"/> that cost <paramref name="costCents"/>.</summary>
        public void Add(Card card, long costCents)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (costCents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(costCents), costCents, "Cost can't be negative.");
            }

            InventoryStack stack = Find(card.Id, card.Tier);
            if (stack == null)
            {
                stack = new InventoryStack { CardId = card.Id, Tier = card.Tier };
                _state.Stacks.Add(stack);
            }

            stack.Count++;
            stack.CostBasisCents += costCents;
        }

        /// <summary>
        /// Adds every card from an opened pack and splits the pack's purchase cost across them.
        /// Rule: an even split in whole cents, with leftover cents going one each to the earliest
        /// slots (425 over 3 cards → 142, 142, 141), so the shares always sum to exactly the purchase
        /// cost. Even split is a placeholder: it makes commons look like losses and hits look cheap;
        /// a value-weighted split may replace it once selling exists.
        /// </summary>
        public void AddPack(OpenedPack pack, long purchaseCostCents)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            if (purchaseCostCents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(purchaseCostCents), purchaseCostCents, "Cost can't be negative.");
            }

            int cardCount = pack.Cards.Count;
            if (cardCount == 0)
            {
                return;
            }

            long evenShare = purchaseCostCents / cardCount;
            long leftoverCents = purchaseCostCents % cardCount;
            for (int slotIndex = 0; slotIndex < cardCount; slotIndex++)
            {
                long share = evenShare + (slotIndex < leftoverCents ? 1 : 0);
                Add(pack.Cards[slotIndex], share);
            }
        }

        /// <summary>
        /// Removes copies and returns the cost basis they carried, using average cost (rounded down).
        /// Removing the last copies takes whatever basis remains, so a stack's basis reaches exactly
        /// zero, and the empty stack is deleted.
        /// </summary>
        /// <exception cref="InvalidOperationException">Fewer than <paramref name="count"/> copies are owned.</exception>
        public long Remove(string cardId, RarityTier tier, int count = 1)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Remove at least one card.");
            }

            InventoryStack stack = Find(cardId, tier);
            int ownedCount = stack == null ? 0 : stack.Count;
            if (count > ownedCount)
            {
                throw new InvalidOperationException($"Can't remove {count} × {cardId} ({tier}); only {ownedCount} owned.");
            }

            long removedCostCents = count == stack.Count
                ? stack.CostBasisCents
                : stack.CostBasisCents * count / stack.Count;

            stack.Count -= count;
            stack.CostBasisCents -= removedCostCents;
            if (stack.Count == 0)
            {
                _state.Stacks.Remove(stack);
            }

            return removedCostCents;
        }

        public void Clear() => _state.Stacks.Clear();

        private InventoryStack Find(string cardId, RarityTier tier)
        {
            foreach (InventoryStack stack in _state.Stacks)
            {
                if (stack.Tier == tier && string.Equals(stack.CardId, cardId, StringComparison.Ordinal))
                {
                    return stack;
                }
            }

            return null;
        }
    }
}
