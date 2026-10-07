using System;
using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>The look of a sparkle burst on a revealed card, in card pixels (the burst scales with the card).</summary>
    [Serializable]
    public sealed class SparkleSettings
    {
        [SerializeField, Min(0.05f), Tooltip("Seconds a burst lasts: the sparkles fly out and fade once.")]
        private float _seconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("How far sparkles travel from the card edge, in card pixels.")]
        private float _travel = 60f;

        [SerializeField, Min(1f), Tooltip("Size of one sparkle, in card pixels.")]
        private float _size = 7f;

        [SerializeField, Range(0f, 1f), Tooltip("Peak opacity of a sparkle.")]
        private float _opacity = 0.9f;

        [SerializeField, Min(1), Tooltip("Bursts kept ready in the pool. More than this at once reuses the oldest.")]
        private int _pooledBursts = 4;

        [SerializeField, Min(1), Tooltip("Most sparkles one burst can show (elements are made once per burst).")]
        private int _maxSparklesPerBurst = 24;

        public float Seconds => _seconds;

        public float Travel => _travel;

        public float Size => _size;

        public float Opacity => _opacity;

        public int PooledBursts => _pooledBursts;

        public int MaxSparklesPerBurst => _maxSparklesPerBurst;
    }
}
