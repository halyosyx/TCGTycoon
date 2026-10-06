using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Turns a tear's progress into a pose: the back flap lifts (the strip hinges back off the back seam),
    /// then the crimp splits (the strip keeps hinging, lifts and drifts away), then the cards rise out of
    /// the top. Pure, so the motion is tested without a scene.
    /// </summary>
    public static class PackTearMotion
    {
        /// <param name="tear">0 sealed to 1 strip away (<see cref="PackTearProgress.Tear"/>).</param>
        /// <param name="rise">0 to 1 (<see cref="PackTearProgress.Rise"/>).</param>
        /// <param name="poseBlend">0 to 1 (<see cref="PackTearProgress.PoseBlend"/>).</param>
        public static PackTearPose Evaluate(float tear, float rise, float poseBlend, PackTearPacing pacing)
        {
            tear = Mathf.Clamp01(tear);
            float flapShare = pacing.FlapShare;

            float stripAngle;
            Vector3 stripOffset;
            if (tear <= flapShare)
            {
                stripAngle = pacing.FlapAngle * Smooth(tear / flapShare);
                stripOffset = Vector3.zero;
            }
            else
            {
                float split = Smooth((tear - flapShare) / (1f - flapShare));
                stripAngle = Mathf.Lerp(pacing.FlapAngle, Mathf.Max(pacing.FlapAngle, pacing.PeelAngle), split);
                stripOffset = new Vector3(pacing.StripDrift * split, pacing.StripLift * split, 0f);
            }

            bool isTorn = tear >= 1f;
            bool areCardsVisible = tear > flapShare;
            float cardsRise = pacing.CardsRiseDistance * Smooth(Mathf.Clamp01(rise));
            return new PackTearPose(stripAngle, stripOffset, !isTorn, areCardsVisible, cardsRise, Mathf.Clamp01(poseBlend));
        }

        private static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }
}
