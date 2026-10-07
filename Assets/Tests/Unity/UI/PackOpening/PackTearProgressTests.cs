using System.Collections.Generic;
using Game.Unity.UI.PackOpening;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.PackOpening
{
    /// <summary>The opening timeline (zoom, then rip: seam, open, hold) and the pose it gives. The cards don't slide in the world: the screen lifts them out in the UI.</summary>
    public sealed class PackTearProgressTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void Tick_ZoomingIn_ReachesTheAnchorAndSettles()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);

            progress.Tick(pacing.ZoomSeconds * 0.5f);
            Assert.That(progress.Zoom, Is.EqualTo(0.5f).Within(Epsilon));
            Assert.That(progress.IsSettled, Is.False);

            progress.Tick(pacing.ZoomSeconds);
            Assert.That(progress.Zoom, Is.EqualTo(1f));
            Assert.That(progress.IsSettled, Is.True);
        }

        [Test]
        public void ZoomOut_MidZoom_ReversesFromWhereItIsAndEndsBackInHand()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            progress.Tick(pacing.ZoomSeconds * 0.6f);

            progress.ZoomOut();
            progress.Tick(pacing.ZoomSeconds * 0.3f);

            Assert.That(progress.Zoom, Is.EqualTo(0.3f).Within(Epsilon));
            Assert.That(progress.IsBackInHand, Is.False);
            progress.Tick(pacing.ZoomSeconds);
            Assert.That(progress.Zoom, Is.EqualTo(0f));
            Assert.That(progress.IsBackInHand, Is.True);
            Assert.That(progress.IsSettled, Is.False);
        }

        [Test]
        public void ZoomOut_AfterTheRip_IsIgnored()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            progress.Tick(pacing.ZoomSeconds);
            progress.StartRip();

            progress.ZoomOut();
            progress.Tick(0.01f);

            Assert.That(progress.IsZoomingOut, Is.False);
            Assert.That(progress.IsRipping, Is.True);
        }

        [Test]
        public void StartRip_MidZoom_SnapsToTheAnchor()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            progress.Tick(pacing.ZoomSeconds * 0.2f);

            progress.StartRip();

            Assert.That(progress.Zoom, Is.EqualTo(1f));
            Assert.That(progress.IsRipping, Is.True);
            Assert.That(progress.IsSettled, Is.False);
        }

        [Test]
        public void Tick_Ripping_SeamThenOpenThenHoldThenComplete()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            progress.Tick(pacing.ZoomSeconds);
            progress.StartRip();

            progress.Tick(pacing.SeamTearSeconds * 0.5f);
            Assert.That(progress.Seam, Is.EqualTo(0.5f).Within(Epsilon));
            Assert.That(progress.Open, Is.EqualTo(0f));

            progress.Tick(pacing.SeamTearSeconds * 0.5f + pacing.OpenSeconds * 0.5f);
            Assert.That(progress.Seam, Is.EqualTo(1f));
            Assert.That(progress.Open, Is.EqualTo(0.5f).Within(Epsilon));

            progress.Tick(pacing.OpenSeconds * 0.5f);
            Assert.That(progress.Open, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(progress.IsComplete, Is.False, "The opened pack holds before the cards lift.");

            progress.Tick(pacing.HandOffSeconds + Epsilon);
            Assert.That(progress.IsComplete, Is.True);
        }

        [Test]
        public void Cues_OneLongTick_FireOnceEachInOrder()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            var cues = new List<PackTearCue>();
            progress.CueReached += cues.Add;
            progress.Tick(pacing.ZoomSeconds);

            progress.StartRip();
            progress.Tick(10f);
            progress.Tick(10f);

            Assert.That(cues, Is.EqualTo(new[] { PackTearCue.SeamTear, PackTearCue.WrapperOpen, PackTearCue.CardsSlide }));
        }

        [Test]
        public void Zooming_FiresNoCue()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            var cues = new List<PackTearCue>();
            progress.CueReached += cues.Add;

            progress.Tick(pacing.ZoomSeconds * 2f);
            progress.ZoomOut();
            progress.Tick(pacing.ZoomSeconds * 2f);

            Assert.That(cues, Is.Empty);
        }

        [Test]
        public void Dim_RisesWhileZoomingInAndFallsWhileBackingOut()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);

            progress.Tick(pacing.DimSeconds * 0.5f);
            Assert.That(progress.Dim, Is.EqualTo(0.5f).Within(Epsilon));
            progress.Tick(pacing.DimSeconds);
            Assert.That(progress.Dim, Is.EqualTo(1f));

            progress.ZoomOut();
            progress.Tick(pacing.DimSeconds * 2f);
            Assert.That(progress.Dim, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_ClearsEverythingForTheNextPack()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            var cues = new List<PackTearCue>();
            progress.CueReached += cues.Add;
            progress.Tick(pacing.ZoomSeconds);
            progress.StartRip();
            progress.Tick(10f);

            progress.Reset();
            cues.Clear();
            progress.Tick(pacing.ZoomSeconds);
            progress.StartRip();

            Assert.That(progress.Seam, Is.EqualTo(0f));
            Assert.That(progress.IsComplete, Is.False);
            Assert.That(cues, Is.EqualTo(new[] { PackTearCue.SeamTear }), "Cues fire again for the next pack.");
        }

        // --- Motion: the pose for a moment of the opening ---

        [Test]
        public void Evaluate_InHand_NothingMovedNothingShown()
        {
            PackTearPose pose = PackTearMotion.Evaluate(0f, 0f, 0f, 0f, new PackTearPacing());

            Assert.That(pose.ZoomBlend, Is.EqualTo(0f));
            Assert.That(pose.SeamTear, Is.EqualTo(0f));
            Assert.That(pose.FlapAngle, Is.EqualTo(0f));
            Assert.That(pose.AreCardsVisible, Is.False);
            Assert.That(pose.DimAlpha, Is.EqualTo(0f));
        }

        [Test]
        public void Evaluate_Settled_AtTheAnchorWithTheWorldDimmed()
        {
            var pacing = new PackTearPacing();

            PackTearPose pose = PackTearMotion.Evaluate(1f, 0f, 0f, 1f, pacing);

            Assert.That(pose.ZoomBlend, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(pose.DimAlpha, Is.EqualTo(pacing.DimAlpha).Within(Epsilon));
            Assert.That(pose.FlapAngle, Is.EqualTo(0f));
        }

        [Test]
        public void Evaluate_Opened_FlapsAtTheOpenAngleCardsShownInPlace()
        {
            var pacing = new PackTearPacing();

            PackTearPose half = PackTearMotion.Evaluate(1f, 1f, 0.5f, 1f, pacing);
            PackTearPose open = PackTearMotion.Evaluate(1f, 1f, 1f, 1f, pacing);

            Assert.That(half.FlapAngle, Is.GreaterThan(0f).And.LessThan(pacing.OpenAngle));
            Assert.That(half.AreCardsVisible, Is.True, "The first card shows as soon as the flaps part.");
            Assert.That(open.FlapAngle, Is.EqualTo(pacing.OpenAngle).Within(Epsilon));
            Assert.That(open.AreCardsVisible, Is.True);
            Assert.That(open.SeamTear, Is.EqualTo(1f));
        }

        [Test]
        public void Evaluate_FlapAngle_NeverGoesBackAsTheWrapperOpens()
        {
            var pacing = new PackTearPacing();
            float previous = -1f;

            for (int step = 0; step <= 40; step++)
            {
                float angle = PackTearMotion.Evaluate(1f, 1f, step / 40f, 1f, pacing).FlapAngle;
                Assert.That(angle, Is.GreaterThanOrEqualTo(previous), $"Step {step}");
                previous = angle;
            }
        }
    }
}
