using UnityEngine;

namespace Game.Unity.Hands
{
    /// <summary>
    /// Something the player can hold in their one hand slot (<see cref="PlayerHands"/>): a sealed
    /// booster pack, or a stack of single cards. The holdable owns what Use (LMB) and Put down (F) mean
    /// for it, including the Core move that goes with them, and supplies its own prompt words. The
    /// player never switches on holdable types.
    /// </summary>
    public interface IHoldable
    {
        /// <summary>What is held, for prompts and debugging, e.g. "Champions pack" or "3 cards".</summary>
        string Noun { get; }

        /// <summary>Units held: 1 for a pack, the card count for a stack.</summary>
        int Count { get; }

        /// <summary>The Use (LMB) prompt verb, e.g. "Open"; empty when <see cref="CanUse"/> is false.</summary>
        string UseVerb { get; }

        bool CanUse { get; }

        /// <summary>The Put down (F) prompt verb, e.g. "Put down" or "Return to binder".</summary>
        string DropVerb { get; }

        /// <summary>Called when the hand takes it: parent to the socket, switch to the held layer, stop colliding.</summary>
        void OnHeld(Transform socket, int heldLayer);

        /// <summary>Uses it. True when it has left the hand (a pack that was opened).</summary>
        bool TryUse();

        /// <summary>Puts it down, wherever this holdable puts things. True when it has left the hand.</summary>
        bool TryDrop(DropContext context);
    }
}
