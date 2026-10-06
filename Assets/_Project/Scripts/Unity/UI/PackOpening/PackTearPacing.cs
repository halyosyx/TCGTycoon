using System;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// How tearing a pack open feels: the drag, the timings and the motion of the two pack pieces, tuned
    /// in the Inspector. The tear is lift the back flap, split the top crimp, then the cards rise out of
    /// the top.
    /// </summary>
    [Serializable]
    public sealed class PackTearPacing
    {
        [Header("Drag")]
        [SerializeField, Min(1f), Tooltip("Downward mouse travel, in pixels, from sealed to the top strip coming away.")]
        private float _dragPixels = 360f;

        [SerializeField, Range(0.05f, 0.95f), Tooltip("Share of the drag spent lifting the back flap before the crimp starts to split.")]
        private float _flapShare = 0.3f;

        [Header("Top strip")]
        [SerializeField, Range(0f, 90f), Tooltip("Degrees the strip hinges back off the back seam by the end of the flap lift.")]
        private float _flapAngle = 25f;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees the strip has hinged back when it comes away.")]
        private float _peelAngle = 130f;

        [SerializeField, Min(0f), Tooltip("Metres the strip lifts as the crimp splits.")]
        private float _stripLift = 0.03f;

        [SerializeField, Tooltip("Metres the strip drifts sideways as it comes away (negative: to the left).")]
        private float _stripDrift = 0.02f;

        [Header("Cards")]
        [SerializeField, Min(0f), Tooltip("Metres the card stack rises out of the top.")]
        private float _cardsRiseDistance = 0.06f;

        [SerializeField, Min(0.01f), Tooltip("Seconds for the cards to rise out of the top.")]
        private float _cardsRiseSeconds = 0.45f;

        [SerializeField, Min(0f), Tooltip("Seconds the risen cards stay in view before the reveal takes over.")]
        private float _handOffSeconds = 0.15f;

        [Header("Pose")]
        [SerializeField, Min(0.01f), Tooltip("Seconds for the pack to move from the hand into the tearing pose.")]
        private float _poseSeconds = 0.2f;

        [SerializeField, Tooltip("Where the pack is held while tearing, in metres from the camera (x right, y up, z forward).")]
        private Vector3 _tearPosePosition = new Vector3(0f, -0.035f, 0.3f);

        [SerializeField, Tooltip("The pack's rotation while tearing, relative to the camera, in degrees (0 = front facing the player; positive X tips the face up toward the ceiling light).")]
        private Vector3 _tearPoseRotation = new Vector3(12f, 0f, 0f);

        public float DragPixels => _dragPixels;

        public float FlapShare => _flapShare;

        public float FlapAngle => _flapAngle;

        public float PeelAngle => _peelAngle;

        public float StripLift => _stripLift;

        public float StripDrift => _stripDrift;

        public float CardsRiseDistance => _cardsRiseDistance;

        public float CardsRiseSeconds => _cardsRiseSeconds;

        public float HandOffSeconds => _handOffSeconds;

        public float PoseSeconds => _poseSeconds;

        public Vector3 TearPosePosition => _tearPosePosition;

        public Quaternion TearPoseRotation => Quaternion.Euler(_tearPoseRotation);
    }
}
