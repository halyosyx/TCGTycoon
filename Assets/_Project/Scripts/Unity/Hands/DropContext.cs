using UnityEngine;

namespace Game.Unity.Hands
{
    /// <summary>Where the player is when they put something down: their view and their body.</summary>
    public readonly struct DropContext
    {
        public DropContext(Transform view, Transform body)
        {
            View = view;
            Body = body;
        }

        /// <summary>The camera: where the player looks.</summary>
        public Transform View { get; }

        /// <summary>The player's root: where they stand.</summary>
        public Transform Body { get; }
    }
}
