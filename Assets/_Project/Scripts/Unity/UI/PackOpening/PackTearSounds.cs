using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>The rip's sound per cue. Clips are placeholders until real foley exists; a missing clip is silent.</summary>
    [Serializable]
    public sealed class PackTearSounds
    {
        [SerializeField, Tooltip("The back seam starts to tear.")]
        [FormerlySerializedAs("_crimpTear")]
        private AudioClip _seamTear;

        [SerializeField, Tooltip("The back flaps swing open.")]
        [FormerlySerializedAs("_flapLift")]
        private AudioClip _wrapperOpen;

        [SerializeField, Tooltip("The cards slide out.")]
        private AudioClip _cardsSlide;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 0.8f;

        public float Volume => _volume;

        public AudioClip ClipFor(PackTearCue cue)
        {
            switch (cue)
            {
                case PackTearCue.SeamTear:
                    return _seamTear;
                case PackTearCue.WrapperOpen:
                    return _wrapperOpen;
                case PackTearCue.CardsSlide:
                    return _cardsSlide;
                default:
                    return null;
            }
        }
    }
}
