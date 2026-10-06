using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>Where the two pack pieces and the cards are at one moment of a tear (<see cref="PackTearMotion"/>).</summary>
    public readonly struct PackTearPose
    {
        public PackTearPose(float stripAngle, Vector3 stripOffset, bool isStripVisible, bool areCardsVisible, float cardsRise, float poseBlend)
        {
            StripAngle = stripAngle;
            StripOffset = stripOffset;
            IsStripVisible = isStripVisible;
            AreCardsVisible = areCardsVisible;
            CardsRise = cardsRise;
            PoseBlend = poseBlend;
        }

        /// <summary>Degrees the top strip has hinged back off the back seam.</summary>
        public float StripAngle { get; }

        /// <summary>The strip's offset from its sealed place, in pack space (metres).</summary>
        public Vector3 StripOffset { get; }

        public bool IsStripVisible { get; }

        public bool AreCardsVisible { get; }

        /// <summary>Metres the card stack has risen out of the top.</summary>
        public float CardsRise { get; }

        /// <summary>0 in the hand pose, 1 in the tearing pose.</summary>
        public float PoseBlend { get; }
    }
}
