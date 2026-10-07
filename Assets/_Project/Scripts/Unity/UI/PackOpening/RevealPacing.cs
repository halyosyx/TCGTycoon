using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Timings of a pack reveal, tuned in the Inspector. Every slot uses the same fast pace: cards arrive
    /// face up (no flip) and there are no slow final slots.
    /// </summary>
    [Serializable]
    public sealed class RevealPacing
    {
        [SerializeField, Min(0.01f), Tooltip("Seconds for a swiped card to curve off the stack.")]
        [FormerlySerializedAs("_slideOutSeconds")]
        private float _swipeSeconds = 0.28f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for the cards to move into the final rows.")]
        private float _rowLayoutSeconds = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds the last card stays on the stack before the rows appear (a click skips the wait).")]
        private float _lastCardHoldSeconds = 1.2f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for a card to move into or out of the showcase.")]
        private float _showcaseSeconds = 0.25f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for a row card to grow or shrink when the pointer enters or leaves it.")]
        private float _hoverSeconds = 0.1f;

        public float SwipeSeconds => _swipeSeconds;

        public float RowLayoutSeconds => _rowLayoutSeconds;

        public float LastCardHoldSeconds => _lastCardHoldSeconds;

        public float ShowcaseSeconds => _showcaseSeconds;

        public float HoverSeconds => _hoverSeconds;
    }
}
