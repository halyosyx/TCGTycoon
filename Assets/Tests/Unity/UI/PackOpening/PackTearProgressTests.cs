using System.Collections.Generic;
using Game.Unity.UI.PackOpening;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.PackOpening
{
    public sealed class PackTearProgressTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void Drag_Down_AdvancesByTheShareOfTheFullDrag()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);

            progress.Drag(pacing.DragPixels * 0.25f);

            Assert.That(progress.Tear, Is.EqualTo(0.25f).Within(Epsilon));
        }

        [Test]
        public void Drag_UpOrNothing_ChangesNothing()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            progress.Drag(pacing.DragPixels * 0.5f);

            progress.Drag(-pacing.DragPixels);
            progress.Drag(0f);

            Assert.That(progress.Tear, Is.EqualTo(0.5f).Within(Epsilon));
        }

        [Test]
        public void Drag_PastTheEnd_ClampsAtTorn()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);

            progress.Drag(pacing.DragPixels * 3f);

            Assert.That(progress.Tear, Is.EqualTo(1f));
            Assert.That(progress.IsTorn, Is.True);
        }

        [Test]
        public void Tick_BeforeTorn_CardsDoNotRise()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            progress.Drag(pacing.DragPixels * 0.9f);

            progress.Tick(pacing.CardsRiseSeconds * 2f);

            Assert.That(progress.Rise, Is.EqualTo(0f));
            Assert.That(progress.IsComplete, Is.False);
        }

        [Test]
        public void Tick_AfterTorn_RisesThenCompletesAfterTheHandOff()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            progress.Drag(pacing.DragPixels);

            progress.Tick(pacing.CardsRiseSeconds * 0.5f);
            Assert.That(progress.Rise, Is.EqualTo(0.5f).Within(Epsilon));

            progress.Tick(pacing.CardsRiseSeconds * 0.5f);
            Assert.That(progress.Rise, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(progress.IsComplete, Is.False, "The hand-off pause still runs.");

            progress.Tick(pacing.HandOffSeconds + Epsilon);
            Assert.That(progress.IsComplete, Is.True);
        }

        [Test]
        public void Cues_SlowDrag_FireOnceEachInOrder()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            var cues = new List<PackTearCue>();
            progress.CueReached += cues.Add;

            for (int step = 0; step < 40; step++)
            {
                progress.Drag(pacing.DragPixels / 20f);
                progress.Tick(0.05f);
            }

            Assert.That(cues, Is.EqualTo(new[] { PackTearCue.FlapLift, PackTearCue.CrimpTear, PackTearCue.CardsSlide }));
        }

        [Test]
        public void Cues_OneBigDrag_StillFireInOrder()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            var cues = new List<PackTearCue>();
            progress.CueReached += cues.Add;

            progress.Drag(pacing.DragPixels * 2f);
            progress.Tick(0.01f);

            Assert.That(cues, Is.EqualTo(new[] { PackTearCue.FlapLift, PackTearCue.CrimpTear, PackTearCue.CardsSlide }));
        }

        [Test]
        public void Tick_PoseBlendsInOverItsDuration()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);

            progress.Tick(pacing.PoseSeconds * 0.5f);
            Assert.That(progress.PoseBlend, Is.EqualTo(0.5f).Within(Epsilon));

            progress.Tick(pacing.PoseSeconds);
            Assert.That(progress.PoseBlend, Is.EqualTo(1f));
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var pacing = new PackTearPacing();
            var progress = new PackTearProgress(pacing);
            var cues = new List<PackTearCue>();
            progress.CueReached += cues.Add;
            progress.Drag(pacing.DragPixels);
            progress.Tick(10f);

            progress.Reset();
            cues.Clear();
            progress.Drag(pacing.DragPixels * 0.1f);

            Assert.That(progress.Tear, Is.EqualTo(0.1f).Within(Epsilon));
            Assert.That(progress.Rise, Is.EqualTo(0f));
            Assert.That(progress.IsComplete, Is.False);
            Assert.That(cues, Is.EqualTo(new[] { PackTearCue.FlapLift }), "Cues fire again for the next pack.");
        }

        // --- Motion: the pose for a moment of the tear ---

        [Test]
        public void Evaluate_Sealed_StripClosedCardsHidden()
        {
            PackTearPose pose = PackTearMotion.Evaluate(0f, 0f, 0f, new PackTearPacing());

            Assert.That(pose.StripAngle, Is.EqualTo(0f));
            Assert.That(pose.StripOffset.sqrMagnitude, Is.EqualTo(0f));
            Assert.That(pose.IsStripVisible, Is.True);
            Assert.That(pose.AreCardsVisible, Is.False);
            Assert.That(pose.CardsRise, Is.EqualTo(0f));
        }

        [Test]
        public void Evaluate_EndOfFlapLift_StripAtTheFlapAngleStillAttached()
        {
            var pacing = new PackTearPacing();

            PackTearPose pose = PackTearMotion.Evaluate(pacing.FlapShare, 0f, 1f, pacing);

            Assert.That(pose.StripAngle, Is.EqualTo(pacing.FlapAngle).Within(Epsilon));
            Assert.That(pose.StripOffset.sqrMagnitude, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void Evaluate_StripAngle_NeverGoesBackAsTheTearAdvances()
        {
            var pacing = new PackTearPacing();
            float previous = -1f;

            for (int step = 0; step <= 50; step++)
            {
                float angle = PackTearMotion.Evaluate(step / 50f, 0f, 1f, pacing).StripAngle;
                Assert.That(angle, Is.GreaterThanOrEqualTo(previous), $"Step {step}");
                previous = angle;
            }
        }

        [Test]
        public void Evaluate_Torn_StripGoneCardsVisible()
        {
            var pacing = new PackTearPacing();

            PackTearPose pose = PackTearMotion.Evaluate(1f, 0f, 1f, pacing);

            Assert.That(pose.IsStripVisible, Is.False);
            Assert.That(pose.AreCardsVisible, Is.True);
        }

        [Test]
        public void Evaluate_Risen_CardsAtTheRiseDistance()
        {
            var pacing = new PackTearPacing();

            Assert.That(PackTearMotion.Evaluate(1f, 0f, 1f, pacing).CardsRise, Is.EqualTo(0f));
            Assert.That(PackTearMotion.Evaluate(1f, 1f, 1f, pacing).CardsRise, Is.EqualTo(pacing.CardsRiseDistance).Within(Epsilon));
        }

        [Test]
        public void Evaluate_PoseBlend_PassesThroughClamped()
        {
            var pacing = new PackTearPacing();

            Assert.That(PackTearMotion.Evaluate(0f, 0f, 0.4f, pacing).PoseBlend, Is.EqualTo(0.4f).Within(Epsilon));
            Assert.That(PackTearMotion.Evaluate(0f, 0f, 3f, pacing).PoseBlend, Is.EqualTo(1f));
        }
    }
}
