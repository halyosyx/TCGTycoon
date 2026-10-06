namespace Game.Unity.UI
{
    /// <summary>
    /// What a binder screen may ask the game to do with an entry, beside reading it
    /// (<see cref="IBinderReadModel"/>). Binder views depend on these two interfaces only, so a view can
    /// be replaced without touching the game side.
    /// </summary>
    public interface IBinderActions
    {
        /// <summary>Single cards in the player's hand now.</summary>
        int HeldCardCount { get; }

        /// <summary>How many single cards the hand can hold.</summary>
        int HeldCardCapacity { get; }

        /// <summary>
        /// Takes one copy of the card with this id from the binder into the player's hand (RMB, T). False
        /// when it can't: not a card, none left in the binder, a pack in the hand, or the hand is full.
        /// </summary>
        bool TryTakeToHand(string itemId);

        /// <summary>Puts the most recently taken card back from the hand into the binder (Shift+RMB).</summary>
        bool TryPutBackToBinder();
    }
}
