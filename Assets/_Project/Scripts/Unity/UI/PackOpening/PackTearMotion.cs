using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Turns the opening's progress into a pose: the zoom to the anchor, the back seam tearing top to
    /// bottom, the flaps opening like a book, the cards sliding out, and the world dimming. Pure, so the
    /// motion is tested without a scene.
    /// </summary>
    public static class PackTearMotion
    {
        /// <param name="zoom">0 hand to 1 anchor (<see cref="PackTearProgress.Zoom"/>).</param>
        /// <param name="seam">0 to 1 (<see cref="PackTearProgress.Seam"/>).</param>
        /// <param name="open">0 to 1 (<see cref="PackTearProgress.Open"/>).</param>
        /// <param name="slide">0 to 1 (<see cref="PackTearProgress.Slide"/>).</param>
        /// <param name="dim">0 to 1 (<see cref="PackTearProgress.Dim"/>).</param>
        public static PackTearPose Evaluate(float zoom, float seam, float open, float slide, float dim, PackTearPacing pacing)
        {
            float zoomBlend = Smooth(zoom);
            float flapAngle = pacing.OpenAngle * Smooth(open);
            float cardsSlide = pacing.CardsSlideDistance * Smooth(slide);
            return new PackTearPose(zoomBlend, Mathf.Clamp01(seam), flapAngle, open > 0f, cardsSlide, pacing.DimAlpha * Mathf.Clamp01(dim));
        }

        private static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }
}
