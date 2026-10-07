using Game.Unity.UI.PackOpening;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests.UI.PackOpening
{
    /// <summary>
    /// The swipe-away curve. Every slot uses the same path and pacing: there are no slow final slots
    /// any more (these replace the old slow-slot pacing tests).
    /// </summary>
    public sealed class SwipePathTests
    {
        private const float Distance = 1100f;
        private const float Drop = 380f;
        private const float Curve = 2.2f;
        private const float Epsilon = 0.01f;

        private static readonly Vector2 s_start = new Vector2(10f, -20f);

        [TestCase(1f)]
        [TestCase(-1f)]
        public void Evaluate_StartsAtTheCardAndEndsOffToTheSideAndDown(float direction)
        {
            Vector2 start = SwipePath.Evaluate(s_start, direction, Distance, Drop, Curve, 0f);
            Vector2 end = SwipePath.Evaluate(s_start, direction, Distance, Drop, Curve, 1f);

            Assert.That(Vector2.Distance(start, s_start), Is.LessThan(Epsilon));
            Assert.That(end.x, Is.EqualTo(s_start.x + direction * Distance).Within(Epsilon));
            Assert.That(end.y, Is.EqualTo(s_start.y + Drop).Within(Epsilon), "Panel y grows downwards.");
        }

        [Test]
        public void Evaluate_MovesSidewaysAndDownwardsOnlyNeverBack()
        {
            Vector2 previous = SwipePath.Evaluate(s_start, 1f, Distance, Drop, Curve, 0f);
            for (int step = 1; step <= 50; step++)
            {
                Vector2 point = SwipePath.Evaluate(s_start, 1f, Distance, Drop, Curve, step / 50f);
                Assert.That(point.x, Is.GreaterThanOrEqualTo(previous.x), $"Step {step} x");
                Assert.That(point.y, Is.GreaterThanOrEqualTo(previous.y), $"Step {step} y");
                previous = point;
            }
        }

        [Test]
        public void Evaluate_CurvesDownward_ThrownSidewaysThenFalling()
        {
            // Halfway along the path in x, the card is still well above the straight line to its end.
            for (int step = 1; step < 50; step++)
            {
                Vector2 point = SwipePath.Evaluate(s_start, 1f, Distance, Drop, Curve, step / 50f);
                float along = (point.x - s_start.x) / Distance;
                float chordY = s_start.y + Drop * along;
                Assert.That(point.y, Is.LessThanOrEqualTo(chordY + Epsilon), $"Step {step}");
            }
        }

        [Test]
        public void Evaluate_ProgressOutsideRange_IsClamped()
        {
            Assert.That(SwipePath.Evaluate(s_start, 1f, Distance, Drop, Curve, -1f), Is.EqualTo(SwipePath.Evaluate(s_start, 1f, Distance, Drop, Curve, 0f)));
            Assert.That(SwipePath.Evaluate(s_start, 1f, Distance, Drop, Curve, 2f), Is.EqualTo(SwipePath.Evaluate(s_start, 1f, Distance, Drop, Curve, 1f)));
        }

        [Test]
        public void Tilt_LeansWithTheSwipeAndGrowsToFull()
        {
            Assert.That(SwipePath.Tilt(1f, 16f, 0f), Is.EqualTo(0f));
            Assert.That(SwipePath.Tilt(1f, 16f, 1f), Is.EqualTo(16f).Within(Epsilon));
            Assert.That(SwipePath.Tilt(-1f, 16f, 1f), Is.EqualTo(-16f).Within(Epsilon));
        }
    }
}
