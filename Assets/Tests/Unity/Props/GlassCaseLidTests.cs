using Game.Unity.Props;
using NUnit.Framework;

namespace Game.Unity.Tests.Props
{
    public sealed class GlassCaseLidTests
    {
        private const float Seconds = 0.4f;
        private const float OpenAngle = 90f;
        private const float Epsilon = 0.0001f;

        [Test]
        public void New_IsClosedAndFlat()
        {
            var lid = new GlassCaseLid(Seconds, OpenAngle);

            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Closed));
            Assert.That(lid.Angle, Is.EqualTo(0f));
            Assert.That(lid.IsOpen, Is.False);
            Assert.That(lid.IsMoving, Is.False);
        }

        [Test]
        public void Toggle_FromClosed_StartsOpening()
        {
            var lid = new GlassCaseLid(Seconds, OpenAngle);

            lid.Toggle();

            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Opening));
            Assert.That(lid.IsMoving, Is.True);
            Assert.That(lid.IsOpen, Is.False, "Not open until the lid is all the way up.");
        }

        [Test]
        public void Tick_Opening_HalfwayThenOpenAtNinetyDegrees()
        {
            var lid = new GlassCaseLid(Seconds, OpenAngle);
            lid.Toggle();

            lid.Tick(Seconds * 0.5f);
            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Opening));
            Assert.That(lid.Angle, Is.EqualTo(OpenAngle * 0.5f).Within(Epsilon));

            lid.Tick(Seconds);
            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Open));
            Assert.That(lid.Angle, Is.EqualTo(OpenAngle));
            Assert.That(lid.IsOpen, Is.True);
            Assert.That(lid.IsMoving, Is.False);
        }

        [Test]
        public void Toggle_FromOpen_ClosesBackToClosed()
        {
            GlassCaseLid lid = CreateOpen();

            lid.Toggle();
            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Closing));
            Assert.That(lid.IsOpen, Is.False);

            lid.Tick(Seconds * 2f);
            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Closed));
            Assert.That(lid.Angle, Is.EqualTo(0f));
        }

        [Test]
        public void Toggle_WhileOpening_ReversesFromWhereItIs()
        {
            var lid = new GlassCaseLid(Seconds, OpenAngle);
            lid.Toggle();
            lid.Tick(Seconds * 0.25f);

            lid.Toggle();

            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Closing));
            Assert.That(lid.Angle, Is.EqualTo(OpenAngle * 0.25f).Within(Epsilon));
            lid.Tick(Seconds * 0.25f + Epsilon);
            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Closed));
        }

        [Test]
        public void Toggle_WhileClosing_ReversesToOpening()
        {
            GlassCaseLid lid = CreateOpen();
            lid.Toggle();
            lid.Tick(Seconds * 0.5f);

            lid.Toggle();

            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Opening));
            lid.Tick(Seconds * 0.5f + Epsilon);
            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Open));
        }

        [Test]
        public void Tick_AtRest_ChangesNothing()
        {
            var closed = new GlassCaseLid(Seconds, OpenAngle);
            GlassCaseLid open = CreateOpen();

            closed.Tick(1f);
            open.Tick(1f);

            Assert.That(closed.State, Is.EqualTo(GlassCaseLidState.Closed));
            Assert.That(closed.Angle, Is.EqualTo(0f));
            Assert.That(open.State, Is.EqualTo(GlassCaseLidState.Open));
            Assert.That(open.Angle, Is.EqualTo(OpenAngle));
        }

        [Test]
        public void ZeroDuration_ToggleSnapsOnTheNextTick()
        {
            var lid = new GlassCaseLid(0f, OpenAngle);

            lid.Toggle();
            lid.Tick(0.001f);

            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Open));
        }

        private static GlassCaseLid CreateOpen()
        {
            var lid = new GlassCaseLid(Seconds, OpenAngle);
            lid.Toggle();
            lid.Tick(Seconds * 2f);
            return lid;
        }
    }
}
