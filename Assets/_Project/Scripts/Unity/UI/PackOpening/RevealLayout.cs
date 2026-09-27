using System;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>Positions and pointer thresholds of a pack reveal, in panel pixels (1920 × 1080 reference).</summary>
    [Serializable]
    public sealed class RevealLayout
    {
        [SerializeField, Tooltip("Offset of each card below the top of the stack, so the stack reads as a pile.")]
        private Vector2 _stackStep = new Vector2(4f, 5f);

        [SerializeField, Min(0f), Tooltip("Horizontal gap between cards in the final row.")]
        private float _rowGap = 28f;

        [SerializeField, Min(0.1f), Tooltip("Scale of the cards in the final row.")]
        private float _rowScale = 0.9f;

        [SerializeField, Min(0f), Tooltip("How far a revealed card slides when it leaves the stack.")]
        private float _slideDistance = 900f;

        [SerializeField, Min(1f), Tooltip("Sideways drag distance that counts as a swipe.")]
        private float _dragThreshold = 80f;

        [SerializeField, Min(0f), Tooltip("Pointer movement below this still counts as a click.")]
        private float _clickSlop = 12f;

        public Vector2 StackStep => _stackStep;

        public float RowGap => _rowGap;

        public float RowScale => _rowScale;

        public float SlideDistance => _slideDistance;

        public float DragThreshold => _dragThreshold;

        public float ClickSlop => _clickSlop;
    }
}
