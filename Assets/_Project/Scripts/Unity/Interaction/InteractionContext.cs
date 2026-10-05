using System;
using Game.Unity.Hands;

namespace Game.Unity.Interaction
{
    /// <summary>
    /// What an <see cref="IInteractable"/> may use when the player interacts with it: today the hands,
    /// so taking something can put it straight into them. Created once by <c>PlayerController</c>.
    /// </summary>
    public sealed class InteractionContext
    {
        public InteractionContext(PlayerHands hands)
        {
            Hands = hands ?? throw new ArgumentNullException(nameof(hands));
        }

        public PlayerHands Hands { get; }
    }
}
