namespace Game.Unity.Interaction
{
    /// <summary>
    /// Something in the world the player can point at and click. <see cref="PlayerInteractor"/> finds
    /// it through any collider on the object or its children.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>False while the object can't be used right now (e.g. a pack that is already open).</summary>
        bool CanInteract { get; }

        /// <summary>What interacting does, for the HUD prompt, e.g. "Open".</summary>
        string PromptVerb { get; }

        /// <summary>What the player is looking at, for the HUD prompt, e.g. "Booster pack". May be empty.</summary>
        string PromptObject { get; }

        /// <summary>Called when the player's aim enters (true) or leaves (false) the object.</summary>
        void SetHovered(bool isHovered);

        /// <summary>Called when the player clicks while aiming at the object.</summary>
        void Interact();
    }
}
