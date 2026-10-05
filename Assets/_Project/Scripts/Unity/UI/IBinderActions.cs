namespace Game.Unity.UI
{
    /// <summary>
    /// What a binder screen may ask the game to do with an entry, beside reading it
    /// (<see cref="IBinderReadModel"/>). Binder views depend on these two interfaces only, so a view can
    /// be replaced without touching the game side.
    /// </summary>
    public interface IBinderActions
    {
        /// <summary>
        /// Takes one copy of the card with this id from the binder into the player's hand (T). False
        /// when it can't: not a card, none left in the binder, a pack in the hand, or ten cards held.
        /// </summary>
        bool TryTakeToHand(string itemId);
    }
}
