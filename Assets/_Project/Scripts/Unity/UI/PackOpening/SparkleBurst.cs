using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// One burst of sparkles on a revealed Special Full Art Holo card: small diamonds that fly out from the
    /// card edge and fade, once. Lives on the card's own element, so it stays local to the card and moves
    /// with it. Pooled by the screen; its elements are made once, and playing writes struct styles only, so
    /// a burst allocates nothing per frame. Never takes the pointer. Angles come from a fixed pattern, not
    /// a random source.
    /// </summary>
    public sealed class SparkleBurst
    {
        // Offsets that break up the even spacing, in turns of the per-sparkle angle.
        private static readonly float[] s_angleJitter = { 0f, 0.31f, -0.22f, 0.12f, -0.36f, 0.27f, -0.08f, 0.19f };
        private static readonly float[] s_travelJitter = { 1f, 0.72f, 0.9f, 0.6f, 0.95f, 0.8f, 0.68f, 0.86f };

        private readonly VisualElement _root;
        private readonly VisualElement[] _sparkles;
        private readonly Vector2[] _directions;
        private readonly float[] _travel;
        private SparkleSettings _settings;
        private int _count;
        private float _elapsed;
        private Vector2 _centre;
        private Vector2 _halfSize;

        public SparkleBurst(int maxSparkles)
        {
            _root = new VisualElement { pickingMode = PickingMode.Ignore, name = "sparkle-burst" };
            _root.style.position = Position.Absolute;
            _root.style.left = 0f;
            _root.style.top = 0f;
            _root.style.right = 0f;
            _root.style.bottom = 0f;
            _sparkles = new VisualElement[maxSparkles];
            _directions = new Vector2[maxSparkles];
            _travel = new float[maxSparkles];
            for (int i = 0; i < maxSparkles; i++)
            {
                var sparkle = new VisualElement { pickingMode = PickingMode.Ignore };
                sparkle.style.position = Position.Absolute;
                _sparkles[i] = sparkle;
                _root.Add(sparkle);
            }
        }

        public bool IsPlaying { get; private set; }

        /// <summary>Starts the burst on <paramref name="card"/> (a card element of <paramref name="cardSize"/> pixels).</summary>
        public void Play(VisualElement card, Vector2 cardSize, int count, Color colour, SparkleSettings settings)
        {
            _settings = settings;
            _count = Mathf.Clamp(count, 0, _sparkles.Length);
            _elapsed = 0f;
            _halfSize = cardSize * 0.5f;
            _centre = _halfSize;
            card.Add(_root);
            float size = settings.Size;
            for (int i = 0; i < _sparkles.Length; i++)
            {
                VisualElement sparkle = _sparkles[i];
                bool isUsed = i < _count;
                sparkle.style.display = isUsed ? DisplayStyle.Flex : DisplayStyle.None;
                if (!isUsed)
                {
                    continue;
                }

                float turn = (i + s_angleJitter[i % s_angleJitter.Length]) / _count;
                float angle = turn * 2f * Mathf.PI;
                _directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _travel[i] = s_travelJitter[i % s_travelJitter.Length];
                sparkle.style.width = size;
                sparkle.style.height = size;
                sparkle.style.marginLeft = -size * 0.5f;
                sparkle.style.marginTop = -size * 0.5f;
                sparkle.style.backgroundColor = colour;
                sparkle.style.rotate = new Rotate(45f);
            }

            IsPlaying = _count > 0;
            Apply();
        }

        /// <summary>Advances the burst; returns false once it has finished (and left its card).</summary>
        public bool Tick(float deltaSeconds)
        {
            if (!IsPlaying)
            {
                return false;
            }

            _elapsed += deltaSeconds;
            if (_elapsed >= _settings.Seconds)
            {
                Stop();
                return false;
            }

            Apply();
            return true;
        }

        public void Stop()
        {
            IsPlaying = false;
            _root.RemoveFromHierarchy();
        }

        private void Apply()
        {
            float t = Mathf.Clamp01(_elapsed / _settings.Seconds);
            float out1 = 1f - (1f - t) * (1f - t);
            float opacity = _settings.Opacity * (1f - t * t);
            float scale = 1f - 0.5f * t;
            for (int i = 0; i < _count; i++)
            {
                // Start on the card's edge in this direction, then fly outward.
                Vector2 direction = _directions[i];
                float edge = Mathf.Min(_halfSize.x / Mathf.Max(Mathf.Abs(direction.x), 0.001f), _halfSize.y / Mathf.Max(Mathf.Abs(direction.y), 0.001f));
                Vector2 position = _centre + direction * (edge + _settings.Travel * _travel[i] * out1);
                VisualElement sparkle = _sparkles[i];
                sparkle.style.left = position.x;
                sparkle.style.top = position.y;
                sparkle.style.opacity = opacity;
                sparkle.style.scale = new Scale(new Vector2(scale, scale));
            }
        }
    }
}
