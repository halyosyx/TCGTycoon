using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Packs;

namespace Game.Core.Inventory
{
    /// <summary>
    /// The player's owned items, each in one <see cref="ItemLocation"/>, with each stack's cost basis in
    /// cents: singles stacked by (card id, tier, location) and sealed products by (product id,
    /// location). New items land in <see cref="ItemLocation.Binder"/>; <see cref="Move"/> (and its
    /// whole-stack form <see cref="MoveAllCards"/>) is the only way an item changes place, and it
    /// enforces each location's rules and capacity. All changes go
    /// through this service; its state is plain data ready for a future save.
    /// </summary>
    public sealed class InventoryService
    {
        private readonly InventoryState _state;
        private readonly LocationCapacities _capacities;

        // Reused by MoveAllCards so placing a stack allocates nothing.
        private readonly List<InventoryStack> _moving = new List<InventoryStack>();

        public InventoryService()
            : this(new InventoryState())
        {
        }

        public InventoryService(InventoryState state, LocationCapacities capacities = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _capacities = capacities ?? LocationCapacities.Default;
        }

        /// <summary>
        /// Raised once after any change to what is owned or where it is: one card, a whole pack (after
        /// every card is in), sealed products, an opened sealed pack (after the swap), a move, a removal
        /// or a clear. Not raised when nothing changed, a move is refused, or an operation throws.
        /// </summary>
        public event Action Changed;

        /// <summary>Current single-card stacks, in the order they were created. Read-only for callers.</summary>
        public IReadOnlyList<InventoryStack> Stacks => _state.Stacks;

        /// <summary>Unopened products, in the order they were created. Read-only for callers.</summary>
        public IReadOnlyList<SealedStack> SealedStacks => _state.SealedStacks;

        public LocationCapacities Capacities => _capacities;

        /// <summary>What everything owned cost: singles and sealed products. Opening and moving don't change it.</summary>
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

        /// <summary>Every owned unit: single cards plus sealed units, in all locations.</summary>
        public int TotalItemCount
        {
            get
            {
                int count = 0;
                foreach (InventoryStack stack in _state.Stacks) count += stack.Count;
                foreach (SealedStack stack in _state.SealedStacks) count += stack.Count;
                return count;
            }
        }

        /// <summary>Copies of a card owned, in all locations.</summary>
        public int CountOf(string cardId, RarityTier tier)
        {
            int count = 0;
            foreach (InventoryStack stack in _state.Stacks)
            {
                if (IsCard(stack, cardId, tier)) count += stack.Count;
            }

            return count;
        }

        /// <summary>Copies of a card in one location.</summary>
        public int CountOf(string cardId, RarityTier tier, ItemLocation location)
        {
            InventoryStack stack = Find(cardId, tier, location);
            return stack == null ? 0 : stack.Count;
        }

        /// <summary>Units of a sealed product owned, in all locations.</summary>
        public int CountOfSealed(string productId)
        {
            int count = 0;
            foreach (SealedStack stack in _state.SealedStacks)
            {
                if (string.Equals(stack.ProductId, productId, StringComparison.Ordinal)) count += stack.Count;
            }

            return count;
        }

        /// <summary>Units of a sealed product in one location.</summary>
        public int CountOfSealed(string productId, ItemLocation location)
        {
            SealedStack stack = FindSealed(productId, location);
            return stack == null ? 0 : stack.Count;
        }

        /// <summary>Units in one location: single cards plus sealed units.</summary>
        public int CountIn(ItemLocation location) => CardsIn(location) + SealedIn(location);

        /// <summary>Adds one copy of <paramref name="card"/> that cost <paramref name="costCents"/>, to the binder.</summary>
        public void Add(Card card, long costCents)
        {
            AddCopy(card, costCents);
            Changed?.Invoke();
        }

        /// <summary>
        /// Adds every card from an opened pack to the binder and splits the pack's purchase cost across
        /// them. Rule: an even split in whole cents, with leftover cents going one each to the earliest
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
        /// Removes copies from <paramref name="from"/> and returns the cost basis they carried, using
        /// average cost (rounded down). Removing the last copies takes whatever basis remains, so a
        /// stack's basis reaches exactly zero, and the empty stack is deleted.
        /// </summary>
        /// <exception cref="InvalidOperationException">Fewer than <paramref name="count"/> copies are there.</exception>
        public long Remove(string cardId, RarityTier tier, int count = 1, ItemLocation from = ItemLocation.Binder)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Remove at least one card.");
            }

            InventoryStack stack = Find(cardId, tier, from);
            int ownedCount = stack == null ? 0 : stack.Count;
            if (count > ownedCount)
            {
                throw new InvalidOperationException($"Can't remove {count} × {cardId} ({tier}) from {from}; only {ownedCount} there.");
            }

            long removedCostCents = TakeFrom(stack, count);
            Changed?.Invoke();
            return removedCostCents;
        }

        /// <summary>Adds <paramref name="count"/> unopened units of a product that cost <paramref name="unitCostCents"/> each, to the binder.</summary>
        public void AddSealed(string productId, int count, long unitCostCents)
        {
            if (string.IsNullOrEmpty(productId)) throw new ArgumentException("A sealed product needs an id.", nameof(productId));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Add at least one unit.");
            if (unitCostCents < 0) throw new ArgumentOutOfRangeException(nameof(unitCostCents), unitCostCents, "Cost can't be negative.");

            PutSealed(productId, ItemLocation.Binder, count, checked(unitCostCents * count));
            Changed?.Invoke();
        }

        /// <summary>
        /// Removes unopened units from <paramref name="from"/> and returns the cost basis they carried,
        /// by the same rule as <see cref="Remove"/>: average cost rounded down, the last units take what remains.
        /// </summary>
        /// <exception cref="InvalidOperationException">Fewer than <paramref name="count"/> units are there.</exception>
        public long RemoveSealed(string productId, int count = 1, ItemLocation from = ItemLocation.Binder)
        {
            long removedCostCents = TakeSealed(productId, count, from);
            Changed?.Invoke();
            return removedCostCents;
        }

        /// <summary>
        /// Opens one unit of <paramref name="productId"/> from <paramref name="from"/>: removes it and
        /// adds the pack's cards to the binder, splitting the unit's paid cost across them as
        /// <see cref="AddPack"/> does. One Changed, after the swap, so listeners never see the pack gone
        /// without its cards. Returns the cost split.
        /// </summary>
        /// <exception cref="InvalidOperationException">No unit of the product is there.</exception>
        public long OpenSealed(string productId, OpenedPack pack, ItemLocation from = ItemLocation.Binder)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            ValidatePackCards(pack);

            long paidCents = TakeSealed(productId, 1, from);
            AddPackCards(pack, paidCents);
            Changed?.Invoke();
            return paidCents;
        }

        /// <summary>
        /// Moves <paramref name="count"/> copies of <paramref name="item"/> from one location to another,
        /// carrying their cost basis (average cost, the last copies take the remainder, so totals never
        /// change). All or nothing: if any rule fails, nothing moves, nothing is created or dropped, no
        /// event fires, and the result says why.
        /// Rules: single cards never go to <see cref="ItemLocation.Placed"/> (no loose cards in the
        /// world), sealed product never goes to <see cref="ItemLocation.DisplayCase"/>, the hand holds one
        /// kind at a time within <see cref="LocationCapacities"/>, and the display case has a capacity.
        /// </summary>
        public MoveResult Move(ItemRef item, ItemLocation from, ItemLocation to, int count = 1)
        {
            if (count < 1 || from == to || string.IsNullOrEmpty(item.Id))
            {
                return MoveResult.Refused(MoveFailure.InvalidMove);
            }

            bool isCard = item.Kind == ItemKind.Card;
            int available = isCard ? CountOf(item.Id, item.Tier, from) : CountOfSealed(item.Id, from);
            if (available < count)
            {
                return MoveResult.Refused(MoveFailure.NotOwned);
            }

            MoveFailure failure = CheckDestination(isCard, to, count);
            if (failure != MoveFailure.None)
            {
                return MoveResult.Refused(failure);
            }

            if (isCard)
            {
                InventoryStack source = Find(item.Id, item.Tier, from);
                long costCents = TakeFrom(source, count);
                PutCards(item.Id, item.Tier, to, count, costCents);
            }
            else
            {
                long costCents = TakeSealed(item.Id, count, from);
                PutSealed(item.Id, to, count, costCents);
            }

            Changed?.Invoke();
            return MoveResult.Moved(count);
        }

        /// <summary>
        /// Moves every single card at <paramref name="from"/> to <paramref name="to"/>, as many as there is
        /// room for: placing the held stack in the display case in one go. Stacks go in order, each taking
        /// what still fits, with cost basis moving by average as in <see cref="Move"/>. What doesn't fit
        /// stays at <paramref name="from"/> (<see cref="MoveFailure.CapacityFull"/>); nothing is dropped or
        /// created. Sealed products are never touched. Raises <see cref="Changed"/> once if anything moved.
        /// </summary>
        public CardsMoveResult MoveAllCards(ItemLocation from, ItemLocation to)
        {
            if (from == to)
            {
                return new CardsMoveResult(0, CardsIn(from), MoveFailure.InvalidMove);
            }

            int total = CardsIn(from);
            if (total == 0)
            {
                return new CardsMoveResult(0, 0, MoveFailure.NotOwned);
            }

            int room = CardRoomIn(to, out MoveFailure refusal);
            if (refusal != MoveFailure.None)
            {
                return new CardsMoveResult(0, total, refusal);
            }

            // Snapshot first: taking a whole stack removes it from the list being read.
            _moving.Clear();
            foreach (InventoryStack stack in _state.Stacks)
            {
                if (stack.Location == from)
                {
                    _moving.Add(stack);
                }
            }

            int moved = 0;
            foreach (InventoryStack stack in _moving)
            {
                int count = Math.Min(stack.Count, room - moved);
                if (count <= 0)
                {
                    break;
                }

                string cardId = stack.CardId;
                RarityTier tier = stack.Tier;
                long costCents = TakeFrom(stack, count);
                PutCards(cardId, tier, to, count, costCents);
                moved += count;
            }

            _moving.Clear();
            if (moved > 0)
            {
                Changed?.Invoke();
            }

            int left = total - moved;
            return new CardsMoveResult(moved, left, left > 0 ? MoveFailure.CapacityFull : MoveFailure.None);
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

        private MoveFailure CheckDestination(bool isCard, ItemLocation to, int count)
        {
            switch (to)
            {
                case ItemLocation.Placed:
                    return isCard ? MoveFailure.NotAllowedThere : MoveFailure.None;
                case ItemLocation.DisplayCase:
                    if (!isCard) return MoveFailure.NotAllowedThere;
                    return CardsIn(ItemLocation.DisplayCase) + count > _capacities.DisplayCase ? MoveFailure.CapacityFull : MoveFailure.None;
                case ItemLocation.Held:
                    if (isCard)
                    {
                        if (SealedIn(ItemLocation.Held) > 0) return MoveFailure.HeldMixed;
                        return CardsIn(ItemLocation.Held) + count > _capacities.HeldCards ? MoveFailure.CapacityFull : MoveFailure.None;
                    }

                    // One sealed unit fills the hand: a second, or any while cards are held, is a mix.
                    return CardsIn(ItemLocation.Held) > 0 || SealedIn(ItemLocation.Held) + count > _capacities.HeldSealed
                        ? MoveFailure.HeldMixed
                        : MoveFailure.None;
                default:
                    return MoveFailure.None;
            }
        }

        // How many more single cards a location takes, or why it takes none.
        private int CardRoomIn(ItemLocation location, out MoveFailure refusal)
        {
            refusal = MoveFailure.None;
            switch (location)
            {
                case ItemLocation.Placed:
                    refusal = MoveFailure.NotAllowedThere;
                    return 0;
                case ItemLocation.DisplayCase:
                    return Math.Max(0, _capacities.DisplayCase - CardsIn(ItemLocation.DisplayCase));
                case ItemLocation.Held:
                    if (SealedIn(ItemLocation.Held) > 0)
                    {
                        refusal = MoveFailure.HeldMixed;
                        return 0;
                    }

                    return Math.Max(0, _capacities.HeldCards - CardsIn(ItemLocation.Held));
                default:
                    return int.MaxValue;
            }
        }

        private int CardsIn(ItemLocation location)
        {
            int count = 0;
            foreach (InventoryStack stack in _state.Stacks)
            {
                if (stack.Location == location) count += stack.Count;
            }

            return count;
        }

        private int SealedIn(ItemLocation location)
        {
            int count = 0;
            foreach (SealedStack stack in _state.SealedStacks)
            {
                if (stack.Location == location) count += stack.Count;
            }

            return count;
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

        // Average cost, rounded down; the last copies take what remains. Deletes an emptied stack.
        private long TakeFrom(InventoryStack stack, int count)
        {
            long costCents = count == stack.Count ? stack.CostBasisCents : stack.CostBasisCents * count / stack.Count;
            stack.Count -= count;
            stack.CostBasisCents -= costCents;
            if (stack.Count == 0)
            {
                _state.Stacks.Remove(stack);
            }

            return costCents;
        }

        private long TakeSealed(string productId, int count, ItemLocation from)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Remove at least one unit.");
            }

            SealedStack stack = FindSealed(productId, from);
            int ownedCount = stack == null ? 0 : stack.Count;
            if (count > ownedCount)
            {
                throw new InvalidOperationException($"Can't take {count} × sealed {productId} from {from}; only {ownedCount} there.");
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

        private void PutCards(string cardId, RarityTier tier, ItemLocation location, int count, long costCents)
        {
            InventoryStack stack = Find(cardId, tier, location);
            if (stack == null)
            {
                stack = new InventoryStack { CardId = cardId, Tier = tier, Location = location };
                _state.Stacks.Add(stack);
            }

            stack.Count += count;
            stack.CostBasisCents += costCents;
        }

        private void PutSealed(string productId, ItemLocation location, int count, long costCents)
        {
            SealedStack stack = FindSealed(productId, location);
            if (stack == null)
            {
                stack = new SealedStack { ProductId = productId, Location = location };
                _state.SealedStacks.Add(stack);
            }

            stack.Count += count;
            stack.CostBasisCents += costCents;
        }

        private void AddCopy(Card card, long costCents)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (costCents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(costCents), costCents, "Cost can't be negative.");
            }

            PutCards(card.Id, card.Tier, ItemLocation.Binder, 1, costCents);
        }

        private InventoryStack Find(string cardId, RarityTier tier, ItemLocation location)
        {
            foreach (InventoryStack stack in _state.Stacks)
            {
                if (stack.Location == location && IsCard(stack, cardId, tier))
                {
                    return stack;
                }
            }

            return null;
        }

        private SealedStack FindSealed(string productId, ItemLocation location)
        {
            foreach (SealedStack stack in _state.SealedStacks)
            {
                if (stack.Location == location && string.Equals(stack.ProductId, productId, StringComparison.Ordinal))
                {
                    return stack;
                }
            }

            return null;
        }

        private static bool IsCard(InventoryStack stack, string cardId, RarityTier tier)
        {
            return stack.Tier == tier && string.Equals(stack.CardId, cardId, StringComparison.Ordinal);
        }
    }
}
