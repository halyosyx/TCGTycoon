using System;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// The opening's timeline, advanced by time only. First the zoom: the pack travels to the centre
    /// anchor (<see cref="Zoom"/> 0 to 1) and settles; backing out runs it in reverse
    /// (<see cref="ZoomOut"/>). Nothing here commits: the screen commits on the rip click, then calls
    /// <see cref="StartRip"/>, which plays the seam tearing and the wrapper opening (the first card shows in
    /// place), then a short hold before <see cref="IsComplete"/>, when the screen lifts the cards out. Plain
    /// C#: the screen feeds it time.
    /// </summary>
    public sealed class PackTearProgress
    {
        private readonly PackTearPacing _pacing;
        private float _handOffElapsed;
        private bool _isOpenCued;
        private bool _isLiftCued;

        public PackTearProgress(PackTearPacing pacing)
        {
            _pacing = pacing ?? throw new ArgumentNullException(nameof(pacing));
        }

        /// <summary>Raised once per pack for each cue, in order.</summary>
        public event Action<PackTearCue> CueReached;

        /// <summary>0 in the hand, 1 at the centre anchor.</summary>
        public float Zoom { get; private set; }

        /// <summary>0 world as normal, 1 fully dimmed.</summary>
        public float Dim { get; private set; }

        public bool IsZoomingOut { get; private set; }

        public bool IsRipping { get; private set; }

        /// <summary>At the anchor and waiting for the rip click.</summary>
        public bool IsSettled => !IsRipping && !IsZoomingOut && Zoom >= 1f;

        /// <summary>Backing out has brought the pack all the way back to the hand.</summary>
        public bool IsBackInHand => IsZoomingOut && Zoom <= 0f;

        /// <summary>How far down the back seam has torn, 0 to 1.</summary>
        public float Seam { get; private set; }

        /// <summary>How far the back flaps have opened, 0 to 1.</summary>
        public float Open { get; private set; }

        public bool IsComplete => IsRipping && Open >= 1f && _handOffElapsed >= _pacing.HandOffSeconds;

        /// <summary>Starts over for the next pack: in the hand, zooming in.</summary>
        public void Reset()
        {
            Zoom = 0f;
            Dim = 0f;
            IsZoomingOut = false;
            IsRipping = false;
            Seam = 0f;
            Open = 0f;
            _handOffElapsed = 0f;
            _isOpenCued = false;
            _isLiftCued = false;
        }

        /// <summary>Backs out: the zoom runs back to the hand from where it is. Ignored once ripping.</summary>
        public void ZoomOut()
        {
            if (!IsRipping)
            {
                IsZoomingOut = true;
            }
        }

        /// <summary>The rip has been committed: the pack snaps to the anchor and the seam starts to tear.</summary>
        public void StartRip()
        {
            if (IsRipping)
            {
                return;
            }

            Zoom = 1f;
            IsZoomingOut = false;
            IsRipping = true;
            CueReached?.Invoke(PackTearCue.SeamTear);
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (!IsRipping)
            {
                float zoomStep = deltaSeconds / _pacing.ZoomSeconds;
                float dimStep = deltaSeconds / _pacing.DimSeconds;
                Zoom = IsZoomingOut ? Math.Max(0f, Zoom - zoomStep) : Math.Min(1f, Zoom + zoomStep);
                Dim = IsZoomingOut ? Math.Max(0f, Dim - dimStep) : Math.Min(1f, Dim + dimStep);
                return;
            }

            // Each phase hands its leftover time to the next, so no tick is lost.
            float left = deltaSeconds;
            Seam = Advance(Seam, _pacing.SeamTearSeconds, ref left);
            if (Seam < 1f)
            {
                return;
            }

            Cue(ref _isOpenCued, PackTearCue.WrapperOpen);
            Open = Advance(Open, _pacing.OpenSeconds, ref left);
            if (Open < 1f)
            {
                return;
            }

            // The wrapper is open and the first card shows: the cards are about to lift out.
            Cue(ref _isLiftCued, PackTearCue.CardsSlide);
            _handOffElapsed += left;
        }

        private static float Advance(float value, float seconds, ref float left)
        {
            if (value >= 1f || left <= 0f)
            {
                return value;
            }

            float secondsLeft = (1f - value) * seconds;
            if (left < secondsLeft)
            {
                value += left / seconds;
                left = 0f;
                return value;
            }

            left -= secondsLeft;
            return 1f;
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
