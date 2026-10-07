using System;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// How opening a pack feels, tuned in the Inspector. The pack zooms from the hand to the centre of the
    /// view with its back turned to the player (uncommitted), then the rip: the back seam tears top to
    /// bottom, the two back flaps open outward like a book and the cards slide out. Plain durations: there
    /// is no drag.
    /// </summary>
    [Serializable]
    public sealed class PackTearPacing
    {
        [Header("Zoom (uncommitted)")]
        [SerializeField, Min(0.01f), Tooltip("Seconds for the pack to travel from the hand to the centre anchor (and back, when backing out).")]
        private float _zoomSeconds = 0.45f;

        [SerializeField, Min(0.05f), Tooltip("Distance of the centre anchor in front of the camera, in metres. The pack is scaled up rather than pulled close, so its corners don't distort.")]
        private float _zoomDistance = 0.32f;

        [SerializeField, Range(0.3f, 1f), Tooltip("How much of the screen height the zoomed pack fills.")]
        private float _zoomScreenHeightShare = 0.9f;

        [SerializeField, Range(-30f, 30f), Tooltip("Degrees the zoomed pack tips about its horizontal axis (positive: its top leans away), so the back catches the light.")]
        private float _zoomTiltDegrees = 6f;

        [SerializeField, Range(0f, 1f), Tooltip("Opacity of the dark layer over the world while the pack is zoomed.")]
        private float _dimAlpha = 0.6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for the world to dim in or out.")]
        private float _dimSeconds = 0.25f;

        [Header("Rip (after the commit)")]
        [SerializeField, Min(0.01f), Tooltip("Seconds for the back seam to tear from top to bottom.")]
        private float _seamTearSeconds = 0.3f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for the back flaps to swing open.")]
        private float _openSeconds = 0.35f;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees each back flap opens to, about its side edge.")]
        private float _openAngle = 150f;

        [SerializeField, Min(0f), Tooltip("How far the card stack slides out of the opened wrapper, in metres.")]
        private float _cardsSlideDistance = 0.08f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for the cards to slide out.")]
        private float _cardsSlideSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Seconds the slid-out cards stay before the reveal takes over.")]
        private float _handOffSeconds = 0.1f;

        public float ZoomSeconds => _zoomSeconds;

        public float ZoomDistance => _zoomDistance;

        public float ZoomScreenHeightShare => _zoomScreenHeightShare;

        public float ZoomTiltDegrees => _zoomTiltDegrees;

        public float DimAlpha => _dimAlpha;

        public float DimSeconds => _dimSeconds;

        public float SeamTearSeconds => _seamTearSeconds;

        public float OpenSeconds => _openSeconds;

        public float OpenAngle => _openAngle;

        public float CardsSlideDistance => _cardsSlideDistance;

        public float CardsSlideSeconds => _cardsSlideSeconds;

        public float HandOffSeconds => _handOffSeconds;

        /// <summary>The anchor's rotation relative to the camera: turned to show the back, tipped by the tilt.</summary>
        public Quaternion AnchorRotation => Quaternion.Euler(_zoomTiltDegrees, 180f, 0f);

        /// <summary>
        /// Scale that makes a pack of <paramref name="packHeight"/> metres fill the configured share of the
        /// screen height at the anchor distance, for a camera of <paramref name="fieldOfView"/> degrees.
        /// </summary>
        public float AnchorScale(float packHeight, float fieldOfView)
        {
            float visibleHeight = 2f * _zoomDistance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            return packHeight <= 0f ? 1f : _zoomScreenHeightShare * visibleHeight / packHeight;
        }
    }
}
