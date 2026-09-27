using System;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>Timings of a pack reveal, tuned in the Inspector.</summary>
    [Serializable]
    public sealed class RevealPacing
    {
        [SerializeField, Min(0.01f), Tooltip("Seconds to flip a card in a fast slot.")]
        private float _fastFlipSeconds = 0.18f;

        [SerializeField, Min(0f), Tooltip("Seconds the rarity glow builds on a face-down card in a slow slot before it flips.")]
        private float _slowTellSeconds = 0.9f;

        [SerializeField, Min(0.01f), Tooltip("Seconds to flip a card in a slow slot.")]
        private float _slowFlipSeconds = 0.45f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for a revealed card to slide off the stack.")]
        private float _slideOutSeconds = 0.2f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for the cards to move into the final row.")]
        private float _rowLayoutSeconds = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds the last card stays on the stack before the row appears (a click skips the wait).")]
        private float _lastCardHoldSeconds = 1.2f;

        [SerializeField, Min(0), Tooltip("How many of the pack's last slots reveal slowly with a rarity tell (2 = slots 4 and 5 of 5).")]
        private int _slowSlotCount = 2;

        public float FastFlipSeconds => _fastFlipSeconds;

        public float SlowTellSeconds => _slowTellSeconds;

        public float SlowFlipSeconds => _slowFlipSeconds;

        public float SlideOutSeconds => _slideOutSeconds;

        public float RowLayoutSeconds => _rowLayoutSeconds;

        public float LastCardHoldSeconds => _lastCardHoldSeconds;

        public int SlowSlotCount => _slowSlotCount;

        /// <summary>True for the pack's last <see cref="SlowSlotCount"/> slots, whatever the pack size.</summary>
        public bool IsSlowSlot(int slotIndex, int cardCount)
        {
            return slotIndex >= 0 && slotIndex < cardCount && slotIndex >= cardCount - _slowSlotCount;
        }
    }
}
