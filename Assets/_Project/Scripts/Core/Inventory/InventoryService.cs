using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Packs;

namespace Game.Core.Inventory
{
    /// <summary>
    /// The player's owned items with each stack's cost basis in cents: singles stacked by (card id,
    /// tier) and sealed products stacked by product id. All changes go through this service; its state
    /// is plain data ready for a future save.
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

        /// <summary>
        /// Raised once after any change to what is owned: one card, a whole pack (after every card is
        /// in), sealed products, an opened sealed pack (after the swap), a removal or a clear. Not raised when nothing changed or an operation throws.
        /// </summary>
        public event Action Changed;

        /// <summary>Current stacks, in the order they were first acquired. Read-only for callers.</summary>
        public IReadOnlyList<InventoryStack> Stacks => _state.Stacks;

        /// <summary>Unopened products, in the order they were first acquired. Read-only for callers.</summary>
        public IReadOnlyList<SealedStack> SealedStacks => _state.SealedStacks;

        /// <summary>What everything owned cost: singles and sealed products. Opening a pack doesn't change it.</summary>
        public long TotalCostBasisCents
        {
            get
            {
                long totalCents = 0;
                foreach (InventoryStack stack in _state.Stacks)
                {
                    totalCents += stack.CostBasisCents;
                }

                foreach (SealedStack stack in _state.SealedStacks)
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
            AddCopy(card, costCents);
            Changed?.Invoke();
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

            if (pack.Cards.Count == 0)
            {
                return;
            }

            ValidatePackCards(pack);
            AddPackCards(pack, purchaseCostCents);

            // One notification per pack: listeners see all its cards at once.
            Changed?.Invoke();
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

            Changed?.Invoke();
            return removedCostCents;
        }

        public int CountOfSealed(string productId)
        {
            SealedStack stack = FindSealed(productId);
            return stack == null ? 0 : stack.Count;
        }

        /// <summary>Adds <paramref name="count"/> unopened units of a product that cost <paramref name="unitCostCents"/> each.</summary>
        public void AddSealed(string productId, int count, long unitCostCents)
        {
            if (string.IsNullOrEmpty(productId)) throw new ArgumentException("A sealed product needs an id.", nameof(productId));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Add at least one unit.");
            if (unitCostCents < 0) throw new ArgumentOutOfRangeException(nameof(unitCostCents), unitCostCents, "Cost can't be negative.");

            long addedCostCents = checked(unitCostCents * count);
            SealedStack stack = FindSealed(productId);
            if (stack == null)
            {
                stack = new SealedStack { ProductId = productId };
                _state.SealedStacks.Add(stack);
            }

            stack.Count += count;
            stack.CostBasisCents += addedCostCents;
            Changed?.Invoke();
        }

        /// <summary>
        /// Removes unopened units and returns the cost basis they carried, by the same rule as
        /// <see cref="Remove"/>: average cost rounded down, the last units take what remains.
        /// </summary>
        /// <exception cref="InvalidOperationException">Fewer than <paramref name="count"/> units are owned.</exception>
        public long RemoveSealed(string productId, int count = 1)
        {
            long removedCostCents = TakeSealed(productId, count);
            Changed?.Invoke();
            return removedCostCents;
        }

        /// <summary>
        /// Opens one owned unit of <paramref name="productId"/>: removes it and adds the pack's cards,
        /// splitting the unit's paid cost across them as <see cref="AddPack"/> does. One Changed, after
        /// the swap, so listeners never see the pack gone without its cards. Returns the cost split.
        /// </summary>
        /// <exception cref="InvalidOperationException">No unit of the product is owned.</exception>
        public long OpenSealed(string productId, OpenedPack pack)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            ValidatePackCards(pack);

            long paidCents = TakeSealed(productId, 1);
            AddPackCards(pack, paidCents);
            Changed?.Invoke();
            return paidCents;
        }

        public void Clear()
        {
            if (_state.Stacks.Count == 0 && _state.SealedStacks.Count == 0)
            {
                return;
            }

            _state.Stacks.Clear();
            _state.SealedStacks.Clear();
            Changed?.Invoke();
        }

        // Checked up front so a bad entry can't leave half a pack added with no Changed raised.
        private static void ValidatePackCards(OpenedPack pack)
        {
            foreach (Card card in pack.Cards)
            {
                if (card == null)
                {
                    throw new ArgumentException("The pack contains an empty card entry.", nameof(pack));
                }
            }
        }

        // Even split in whole cents; leftover cents go one each to the earliest slots.
        private void AddPackCards(OpenedPack pack, long costCents)
        {
            int cardCount = pack.Cards.Count;
            if (cardCount == 0)
            {
                return;
            }

            long evenShare = costCents / cardCount;
            long leftoverCents = costCents % cardCount;
            for (int slotIndex = 0; slotIndex < cardCount; slotIndex++)
            {
                long share = evenShare + (slotIndex < leftoverCents ? 1 : 0);
                AddCopy(pack.Cards[slotIndex], share);
            }
        }

        private long TakeSealed(string productId, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Remove at least one unit.");
            }

            SealedStack stack = FindSealed(productId);
            int ownedCount = stack == null ? 0 : stack.Count;
            if (count > ownedCount)
            {
                throw new InvalidOperationException($"Can't take {count} × sealed {productId}; only {ownedCount} owned.");
            }

            long removedCostCents = count == stack.Count
                ? stack.CostBasisCents
                : stack.CostBasisCents * count / stack.Count;

            stack.Count -= count;
            stack.CostBasisCents -= removedCostCents;
            if (stack.Count == 0)
            {
                _state.SealedStacks.Remove(stack);
            }

            return removedCostCents;
        }

        private SealedStack FindSealed(string productId)
        {
            foreach (SealedStack stack in _state.SealedStacks)
            {
                if (string.Equals(stack.ProductId, productId, StringComparison.Ordinal))
                {
                    return stack;
                }
            }

            return null;
        }

        private void AddCopy(Card card, long costCents)
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
