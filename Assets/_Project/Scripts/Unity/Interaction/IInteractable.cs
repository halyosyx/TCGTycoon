namespace Game.Unity.Interaction
{
    /// <summary>
    /// Something in the world the player can look at and use with the Interact key (E): take a pack
    /// from a stack, pick up a set-down pack, later open a glass case. <see cref="PlayerInteractor"/>
    /// finds it through any collider on the object or its children, on the Interactable layer. The
    /// object supplies its own prompt and decides what interacting does; the player never switches on
    /// object types.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// False while the object can't be used right now, e.g. an empty stack, or a pack to take while
        /// the hand is already full. Only usable objects are highlighted and prompted.
        /// </summary>
        bool CanInteract(InteractionContext context);

        /// <summary>What interacting does, for the HUD prompt, e.g. "Take".</summary>
        string PromptVerb { get; }

        /// <summary>What the player is looking at, for the HUD prompt, e.g. "Champions pack". May be empty.</summary>
        string PromptObject { get; }

        /// <summary>Called when the player's aim enters (true) or leaves (false) the object.</summary>
        void SetHovered(bool isHovered);

        /// <summary>Called when the player presses Interact while aiming at the object.</summary>
        void Interact(InteractionContext context);
    }
}
