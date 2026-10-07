using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Sizes, positions and pointer thresholds of a pack reveal, in panel pixels (1920 × 1080 reference).
    /// The swipe-sequence card size and the row card scale are separate values on purpose. The final
    /// layout is always two rows (4 + 3 for seven cards).
    /// </summary>
    [Serializable]
    public sealed class RevealLayout
    {
        [Header("Swipe sequence")]
        [SerializeField, Range(0.3f, 0.95f), Tooltip("Height of the card being swiped, as a share of the screen height. Keep it a little below the zoomed pack.")]
        private float _stackCardHeightShare = 0.72f;

        [SerializeField, Tooltip("Offset of each card below the top of the stack, in card pixels, so the stack reads as a pile.")]
        private Vector2 _stackStep = new Vector2(2f, 2.5f);

        [SerializeField, Min(0f), Tooltip("How far a swiped card travels sideways, in panel pixels.")]
        [FormerlySerializedAs("_slideDistance")]
        private float _swipeDistance = 1100f;

        [SerializeField, Min(0f), Tooltip("How far a swiped card drops as it leaves, in panel pixels.")]
        private float _swipeDrop = 380f;

        [SerializeField, Range(1f, 5f), Tooltip("Shape of the swipe's curve: 1 is a straight line; higher throws it sideways first and drops it later.")]
        private float _swipeCurve = 2.2f;

        [SerializeField, Range(0f, 45f), Tooltip("Degrees a swiped card leans into its swipe by the time it leaves.")]
        private float _swipeTiltDegrees = 16f;

        [SerializeField, Min(1f), Tooltip("Sideways drag distance that counts as a swipe.")]
        private float _dragThreshold = 80f;

        [SerializeField, Min(0f), Tooltip("Pointer movement below this still counts as a click.")]
        private float _clickSlop = 12f;

        [Header("Rows")]
        [SerializeField, Min(0.1f), Tooltip("Scale of the cards in the final rows (1.375 times the old single row's 0.8).")]
        private float _rowScale = 1.1f;

        [SerializeField, Min(0f), Tooltip("Horizontal gap between cards in a row.")]
        private float _rowGap = 24f;

        [SerializeField, Min(0f), Tooltip("Vertical gap between the two rows.")]
        private float _rowLineGap = 24f;

        [SerializeField, Min(0.1f), Tooltip("Scale of a row card while the pointer is over it (above Row Scale).")]
        private float _hoverScale = 1.2f;

        [Header("Showcase")]
        [SerializeField, Range(0.1f, 1f), Tooltip("Height of the showcased card as a share of the screen height.")]
        private float _showcaseHeightShare = 0.7f;

        [SerializeField, Range(0f, 1f), Tooltip("Vertical centre of the showcased card as a share of the screen height (0 = top).")]
        private float _showcaseCenterShare = 0.5f;

        public float StackCardHeightShare => _stackCardHeightShare;

        public Vector2 StackStep => _stackStep;

        public float SwipeDistance => _swipeDistance;

        public float SwipeDrop => _swipeDrop;

        public float SwipeCurve => _swipeCurve;

        public float SwipeTiltDegrees => _swipeTiltDegrees;

        public float DragThreshold => _dragThreshold;

        public float ClickSlop => _clickSlop;

        public float RowScale => _rowScale;

        public float RowGap => _rowGap;

        public float HoverScale => _hoverScale;

        public float ShowcaseHeightShare => _showcaseHeightShare;

        public float ShowcaseCenterShare => _showcaseCenterShare;

        /// <summary>Scale of a swipe-sequence card for a panel <paramref name="panelHeight"/> pixels tall.</summary>
        public float StackScale(float panelHeight) => _stackCardHeightShare * panelHeight / CardView.Height;

        /// <summary>Where a card of <paramref name="cardSize"/> (unscaled) sits in the final rows, relative to the rest position.</summary>
        public Vector2 RowPosition(int slotIndex, int cardCount, Vector2 cardSize)
        {
            return RowPosition(slotIndex, cardCount, RowArrangement.TwoRows, cardSize * _rowScale, _rowGap, _rowLineGap);
        }

        /// <summary>Width and height of the whole two-row block for <paramref name="cardCount"/> cards of <paramref name="cardSize"/> (unscaled).</summary>
        public Vector2 RowBlockSize(int cardCount, Vector2 cardSize)
        {
            Vector2 scaled = cardSize * _rowScale;
            int topCount = cardCount < 2 ? cardCount : (cardCount + 1) / 2;
            int lines = cardCount < 2 ? 1 : 2;
            float width = topCount * scaled.x + Mathf.Max(0, topCount - 1) * _rowGap;
            float height = lines * scaled.y + (lines - 1) * _rowLineGap;
            return new Vector2(width, height);
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

        /// <summary>
        /// The panel's logical size for a screen, as UI Toolkit's "scale with screen size" works it out:
        /// the scale blends the width and height ratios (in log space) by <paramref name="match"/>.
        /// </summary>
        public static Vector2 PanelSize(Vector2 screen, Vector2 reference, float match)
        {
            float widthLog = Mathf.Log(screen.x / reference.x, 2f);
            float heightLog = Mathf.Log(screen.y / reference.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(widthLog, heightLog, match));
            return screen / scale;
        }
    }
}
