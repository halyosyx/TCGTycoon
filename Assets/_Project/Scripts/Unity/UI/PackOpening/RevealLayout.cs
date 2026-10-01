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

        [SerializeField, Tooltip("Single row: every card on one line. Two rows: the first half on top, the rest below (4 + 3 for seven cards).")]
        private RowArrangement _rowArrangement = RowArrangement.SingleRow;

        [SerializeField, Min(0f), Tooltip("Horizontal gap between cards in the final row.")]
        private float _rowGap = 24f;

        [SerializeField, Min(0.1f), Tooltip("Scale of the cards in a single row.")]
        private float _rowScale = 0.8f;

        [SerializeField, Min(0.1f), Tooltip("Scale of the cards when they lay out in two rows.")]
        private float _twoRowScale = 0.9f;

        [SerializeField, Min(0f), Tooltip("Vertical gap between the two rows.")]
        private float _rowLineGap = 24f;

        [SerializeField, Min(0.1f), Tooltip("Scale of a row card while the pointer is over it.")]
        private float _hoverScale = 1f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Height of the showcased card as a share of the screen height.")]
        private float _showcaseHeightShare = 0.7f;

        [SerializeField, Range(0f, 1f), Tooltip("Vertical centre of the showcased card as a share of the screen height (0 = top).")]
        private float _showcaseCenterShare = 0.5f;

        [SerializeField, Min(0f), Tooltip("How far a revealed card slides when it leaves the stack.")]
        private float _slideDistance = 900f;

        [SerializeField, Min(1f), Tooltip("Sideways drag distance that counts as a swipe.")]
        private float _dragThreshold = 80f;

        [SerializeField, Min(0f), Tooltip("Pointer movement below this still counts as a click.")]
        private float _clickSlop = 12f;

        public Vector2 StackStep => _stackStep;

        public RowArrangement RowArrangement => _rowArrangement;

        public float RowGap => _rowGap;

        /// <summary>Scale of the cards in the final row, for the current arrangement.</summary>
        public float RowScale => _rowArrangement == RowArrangement.TwoRows ? _twoRowScale : _rowScale;

        public float HoverScale => _hoverScale;

        public float ShowcaseHeightShare => _showcaseHeightShare;

        public float ShowcaseCenterShare => _showcaseCenterShare;

        public float SlideDistance => _slideDistance;

        public float DragThreshold => _dragThreshold;

        public float ClickSlop => _clickSlop;

        /// <summary>Where a card of <paramref name="cardSize"/> (unscaled) sits in the final row, relative to the rest position.</summary>
        public Vector2 RowPosition(int slotIndex, int cardCount, Vector2 cardSize)
        {
            return RowPosition(slotIndex, cardCount, _rowArrangement, cardSize * RowScale, _rowGap, _rowLineGap);
        }

        /// <summary>
        /// Row position of a slot, relative to the rest position (y grows downwards). Pure, so any card
        /// count works: one centred line, or two centred lines with the first holding the larger half.
        /// </summary>
        /// <param name="scaledCardSize">The card's size at row scale.</param>
        public static Vector2 RowPosition(int slotIndex, int cardCount, RowArrangement arrangement, Vector2 scaledCardSize, float gap, float lineGap)
        {
            float stepX = scaledCardSize.x + gap;
            if (arrangement != RowArrangement.TwoRows || cardCount < 2)
            {
                return new Vector2((slotIndex - (cardCount - 1) * 0.5f) * stepX, 0f);
            }

            int topCount = (cardCount + 1) / 2;
            bool isTop = slotIndex < topCount;
            int column = isTop ? slotIndex : slotIndex - topCount;
            int lineCount = isTop ? topCount : cardCount - topCount;
            float stepY = scaledCardSize.y + lineGap;
            return new Vector2((column - (lineCount - 1) * 0.5f) * stepX, (isTop ? -0.5f : 0.5f) * stepY);
        }
    }
}
