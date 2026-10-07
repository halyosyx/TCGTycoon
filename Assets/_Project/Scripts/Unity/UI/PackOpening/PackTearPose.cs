namespace Game.Unity.UI.PackOpening
{
    /// <summary>Where the pack, its flaps and its cards are at one moment of the opening (<see cref="PackTearMotion"/>).</summary>
    public readonly struct PackTearPose
    {
        public PackTearPose(float zoomBlend, float seamTear, float flapAngle, bool areCardsVisible, float dimAlpha)
        {
            ZoomBlend = zoomBlend;
            SeamTear = seamTear;
            FlapAngle = flapAngle;
            AreCardsVisible = areCardsVisible;
            DimAlpha = dimAlpha;
        }

        /// <summary>0 in the hand, 1 at the centre anchor (eased).</summary>
        public float ZoomBlend { get; }

        /// <summary>How far down the back seam has torn, 0 to 1.</summary>
        public float SeamTear { get; }

        /// <summary>Degrees each back flap has opened about its side edge.</summary>
        public float FlapAngle { get; }

        /// <summary>The cards (the first one face up) show inside the pack once the flaps part.</summary>
        public bool AreCardsVisible { get; }

        /// <summary>Opacity of the dark layer over the world.</summary>
        public float DimAlpha { get; }
    }
}
