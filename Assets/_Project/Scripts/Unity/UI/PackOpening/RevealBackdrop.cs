using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// How the dark layers cross over while the cards lift out of the opened pack: the world dimmer (behind
    /// the pack) fades out as the reveal's UI backdrop (over everything) fades in. Two stacked dark layers
    /// of opacity a and b darken the scene by 1 − (1 − a)(1 − b), so the backdrop is solved for each moment
    /// to make the total darkness move straight from the dimmer's level to the backdrop's, with no jump or
    /// dip. Pure, so it is tested without a scene.
    /// </summary>
    public static class RevealBackdrop
    {
        /// <param name="dimStart">The world dimmer's opacity when the lift starts.</param>
        /// <param name="backdropTarget">The reveal backdrop's own opacity (its resting look).</param>
        /// <param name="progress">0 to 1 through the lift.</param>
        public static void Blend(float dimStart, float backdropTarget, float progress, out float dim, out float backdrop)
        {
            float t = Mathf.Clamp01(progress);
            dim = dimStart * (1f - t);
            float total = Mathf.Lerp(dimStart, backdropTarget, t);
            backdrop = dim >= 1f ? 0f : Mathf.Clamp01(1f - (1f - total) / (1f - dim));
        }

        /// <summary>How dark two stacked layers of these opacities make the scene, 0 to 1.</summary>
        public static float TotalDarkness(float dim, float backdrop) => 1f - (1f - dim) * (1f - backdrop);
    }
}
