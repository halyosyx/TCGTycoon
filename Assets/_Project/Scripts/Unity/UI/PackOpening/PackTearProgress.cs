using System;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// How far one pack's tear has got. The drag moves <see cref="Tear"/> (downward only, so releasing or
    /// moving up pauses it); once the strip is off, time moves the cards' <see cref="Rise"/>, then a short
    /// hand-off before <see cref="IsComplete"/>. Plain C#: the screen feeds it input and time.
    /// </summary>
    public sealed class PackTearProgress
    {
        private readonly PackTearPacing _pacing;
        private float _handOffElapsed;
        private bool _isFlapCued;
        private bool _isCrimpCued;
        private bool _isSlideCued;

        public PackTearProgress(PackTearPacing pacing)
        {
            _pacing = pacing ?? throw new ArgumentNullException(nameof(pacing));
        }

        /// <summary>Raised once per pack for each cue, in order.</summary>
        public event Action<PackTearCue> CueReached;

        /// <summary>0 sealed, 1 the top strip has come away.</summary>
        public float Tear { get; private set; }

        /// <summary>0 cards inside, 1 risen out of the top.</summary>
        public float Rise { get; private set; }

        /// <summary>0 in the hand pose, 1 in the tearing pose.</summary>
        public float PoseBlend { get; private set; }

        public bool IsTorn => Tear >= 1f;

        public bool IsComplete => Rise >= 1f && _handOffElapsed >= _pacing.HandOffSeconds;

        /// <summary>Starts over for the next pack.</summary>
        public void Reset()
        {
            Tear = 0f;
            Rise = 0f;
            PoseBlend = 0f;
            _handOffElapsed = 0f;
            _isFlapCued = false;
            _isCrimpCued = false;
            _isSlideCued = false;
        }

        /// <summary>Moves the tear by a drag of <paramref name="downPixels"/>; upward drags are ignored.</summary>
        public void Drag(float downPixels)
        {
            if (downPixels <= 0f || IsTorn)
            {
                return;
            }

            Tear = Math.Min(1f, Tear + downPixels / _pacing.DragPixels);
            Cue(ref _isFlapCued, PackTearCue.FlapLift);
            if (Tear > _pacing.FlapShare)
            {
                Cue(ref _isCrimpCued, PackTearCue.CrimpTear);
            }
        }

        /// <summary>Advances the pose blend, and once torn the cards' rise and the hand-off.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            PoseBlend = Math.Min(1f, PoseBlend + deltaSeconds / _pacing.PoseSeconds);
            if (!IsTorn)
            {
                return;
            }

            Cue(ref _isSlideCued, PackTearCue.CardsSlide);
            if (Rise < 1f)
            {
                // Time past the end of the rise counts toward the hand-off, so no tick is lost.
                float riseSecondsLeft = (1f - Rise) * _pacing.CardsRiseSeconds;
                if (deltaSeconds < riseSecondsLeft)
                {
                    Rise += deltaSeconds / _pacing.CardsRiseSeconds;
                    return;
                }

                Rise = 1f;
                deltaSeconds -= riseSecondsLeft;
            }

            _handOffElapsed += deltaSeconds;
        }

        private void Cue(ref bool isCued, PackTearCue cue)
        {
            if (isCued)
            {
                return;
            }

            isCued = true;
            CueReached?.Invoke(cue);
        }
    }
}
