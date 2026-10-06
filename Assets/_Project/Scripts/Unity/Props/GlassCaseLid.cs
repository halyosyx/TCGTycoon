using System;

namespace Game.Unity.Props
{
    /// <summary>
    /// The glass case lid: Closed → Opening → Open → Closing → Closed. <see cref="Toggle"/> starts the
    /// next motion (mid-motion it reverses from where the lid is), and <see cref="Tick"/> moves it over
    /// a fixed duration. Plain C#, so the case's MonoBehaviour only shows <see cref="Angle"/>. Cards may be
    /// placed only while <see cref="IsOpen"/>; closing with cards inside is fine.
    /// </summary>
    public sealed class GlassCaseLid
    {
        private readonly float _seconds;
        private readonly float _openAngle;

        /// <param name="seconds">Time for a full open or close; 0 snaps on the next tick.</param>
        /// <param name="openAngle">Degrees the lid stands at when open.</param>
        public GlassCaseLid(float seconds, float openAngle)
        {
            if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            _seconds = seconds;
            _openAngle = openAngle;
        }

        public GlassCaseLidState State { get; private set; } = GlassCaseLidState.Closed;

        /// <summary>0 closed to 1 open.</summary>
        public float Progress { get; private set; }

        public float Angle => Progress * _openAngle;

        /// <summary>All the way up: the only state cards can be placed or taken in.</summary>
        public bool IsOpen => State == GlassCaseLidState.Open;

        public bool IsMoving => State == GlassCaseLidState.Opening || State == GlassCaseLidState.Closing;

        public void Toggle()
        {
            switch (State)
            {
                case GlassCaseLidState.Closed:
                case GlassCaseLidState.Closing:
                    State = GlassCaseLidState.Opening;
                    break;
                case GlassCaseLidState.Open:
                case GlassCaseLidState.Opening:
                    State = GlassCaseLidState.Closing;
                    break;
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsMoving || deltaSeconds <= 0f)
            {
                return;
            }

            float step = _seconds <= 0f ? 1f : deltaSeconds / _seconds;
            if (State == GlassCaseLidState.Opening)
            {
                Progress = Math.Min(1f, Progress + step);
                if (Progress >= 1f)
                {
                    State = GlassCaseLidState.Open;
                }
            }
            else
            {
                Progress = Math.Max(0f, Progress - step);
                if (Progress <= 0f)
                {
                    State = GlassCaseLidState.Closed;
                }
            }
        }
    }
}
