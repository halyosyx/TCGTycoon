using Game.Unity.UI.PackOpening;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.PackOpening
{
    /// <summary>
    /// The hand-off from the dimmed world to the reveal's backdrop: the world dimmer fades out while the
    /// UI backdrop fades in, so the screen darkens smoothly from one to the other with no jump.
    /// </summary>
    public sealed class RevealBackdropTests
    {
        private const float Dim = 0.6f;
        private const float Backdrop = 0.82f;
        private const float Epsilon = 0.0001f;

        [Test]
        public void Blend_AtStart_WorldDimmedNoBackdrop()
        {
            RevealBackdrop.Blend(Dim, Backdrop, 0f, out float dim, out float backdrop);

            Assert.That(dim, Is.EqualTo(Dim).Within(Epsilon));
            Assert.That(backdrop, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void Blend_AtEnd_BackdropOnlyAtItsOwnStrength()
        {
            RevealBackdrop.Blend(Dim, Backdrop, 1f, out float dim, out float backdrop);

            Assert.That(dim, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(backdrop, Is.EqualTo(Backdrop).Within(Epsilon));
        }

        [Test]
        public void Blend_TotalDarkness_RisesSteadilyNeverDips()
        {
            float previous = -1f;
            for (int step = 0; step <= 50; step++)
            {
                RevealBackdrop.Blend(Dim, Backdrop, step / 50f, out float dim, out float backdrop);
                float total = RevealBackdrop.TotalDarkness(dim, backdrop);
                Assert.That(total, Is.GreaterThanOrEqualTo(previous - Epsilon), $"Step {step}");
                Assert.That(dim, Is.InRange(0f, 1f));
                Assert.That(backdrop, Is.InRange(0f, 1f));
                previous = total;
            }

            Assert.That(previous, Is.EqualTo(Backdrop).Within(Epsilon));
        }

        [Test]
        public void Blend_WorldAlreadyDarkerThanTheBackdrop_NeverOvershoots()
        {
            RevealBackdrop.Blend(0.9f, Backdrop, 0.5f, out float dim, out float backdrop);

            Assert.That(backdrop, Is.InRange(0f, 1f));
            Assert.That(RevealBackdrop.TotalDarkness(dim, backdrop), Is.InRange(Backdrop - Epsilon, 0.9f + Epsilon));
        }

        [Test]
        public void Blend_ProgressOutsideRange_IsClamped()
        {
            RevealBackdrop.Blend(Dim, Backdrop, -1f, out float startDim, out float startBackdrop);
            RevealBackdrop.Blend(Dim, Backdrop, 2f, out float endDim, out float endBackdrop);

            Assert.That(startDim, Is.EqualTo(Dim).Within(Epsilon));
            Assert.That(startBackdrop, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(endDim, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(endBackdrop, Is.EqualTo(Backdrop).Within(Epsilon));
        }
    }
}
