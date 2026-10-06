using System;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>The tear's sound per cue. Clips are placeholders until real foley exists; a missing clip is silent.</summary>
    [Serializable]
    public sealed class PackTearSounds
    {
        [SerializeField, Tooltip("The back flap starts to lift.")]
        private AudioClip _flapLift;

        [SerializeField, Tooltip("The top crimp starts to split.")]
        private AudioClip _crimpTear;

        [SerializeField, Tooltip("The cards start sliding out of the top.")]
        private AudioClip _cardsSlide;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 0.8f;

        public float Volume => _volume;

        public AudioClip ClipFor(PackTearCue cue)
        {
            switch (cue)
            {
                case PackTearCue.FlapLift:
                    return _flapLift;
                case PackTearCue.CrimpTear:
                    return _crimpTear;
                case PackTearCue.CardsSlide:
                    return _cardsSlide;
                default:
                    return null;
            }
        }
    }
}
