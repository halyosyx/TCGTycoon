using System;
using Game.Core.Content;
using Game.Unity.Cards;
using Game.Unity.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// One card on the pack opening screen: the generated card template as the face, a flat back and
    /// a rarity glow behind it. Pooled and re-bound for every pack. Animation is stepped by
    /// <see cref="Tick"/> and only writes struct style values, so it allocates nothing per frame.
    /// </summary>
    public sealed class CardView
    {
        public const string ClassName = "card-view";

        /// <summary>Card width in panel pixels; matches `.card-view` in PackOpening.uss and `.card` in the generated template.</summary>
        public const float Width = 250f;

        private const string FaceClassName = "card-view__face";
        private const string BackClassName = "card-view__back";
        private const string BackLabelClassName = "card-view__back-label";
        private const string GlowClassName = "card-view__glow";
        private const string BackLabelText = "Mythbound";

        // Glow breathes between this share of its peak and the peak.
        private const float GlowPulseFloor = 0.7f;
        // The card swells slightly while a tell builds, as if it's about to burst.
        private const float TellSwell = 0.04f;
        private const float GlowCornerRadius = 16f;

        private readonly VisualElement _root;
        private readonly VisualElement _glow;
        private readonly VisualElement _face;
        private readonly VisualElement _back;
        private readonly Label _backLabel;

        private AnimationPhase _phase;
        private float _elapsed;
        private float _duration;
        private float _flipHalfSeconds;

        private Vector2 _position;
        private Vector2 _moveFrom;
        private Vector2 _moveTo;
        private float _scale;
        private float _scaleFrom;
        private float _scaleTo;
        private float _opacity;
        private float _opacityFrom;
        private float _opacityTo;
        private bool _isHiddenWhenMoved;
        private float _flipScaleX;
        private float _swell;
        private float _dragOffset;

        private float _glowIntensity;
        private float _glowPulsesPerSecond;
        private float _glowLevel;
        private float _glowClock;

        public CardView(VisualTreeAsset cardTemplate)
        {
            if (cardTemplate == null) throw new ArgumentNullException(nameof(cardTemplate));

            _root = new VisualElement { name = ClassName };
            _root.AddToClassList(ClassName);

            _glow = new VisualElement { pickingMode = PickingMode.Ignore };
            _glow.AddToClassList(GlowClassName);

            _face = cardTemplate.Instantiate();
            _face.AddToClassList(FaceClassName);

            _back = new VisualElement();
            _back.AddToClassList(BackClassName);
            _backLabel = new Label(BackLabelText);
            _backLabel.AddToClassList(BackLabelClassName);
            _back.Add(_backLabel);

            _root.Add(_glow);
            _root.Add(_face);
            _root.Add(_back);
        }

        private enum AnimationPhase
        {
            None,
            Tell,
            FlipToEdge,
            FlipFromEdge,
            Move,
        }

        public VisualElement Root => _root;

        public bool IsAnimating => _phase != AnimationPhase.None;

        public bool IsFaceUp { get; private set; }

        public Vector2 Position => _position;

        /// <summary>Shows a new card face down at the given position, with every animation reset.</summary>
        public void Bind(Card card, RarityPaletteDefinition palette, TierTell tell, Vector2 position)
        {
            CardTemplate.Bind(_face, card, palette);

            _back.style.backgroundColor = palette.PackColor;
            Color backBorder = palette.PackLabelColor;
            _back.style.borderTopColor = backBorder;
            _back.style.borderRightColor = backBorder;
            _back.style.borderBottomColor = backBorder;
            _back.style.borderLeftColor = backBorder;
            _backLabel.style.color = palette.PackLabelColor;

            float spread = tell == null ? 0f : tell.Spread;
            _glow.style.backgroundColor = palette.ColorOf(card.Tier);
            _glow.style.left = -spread;
            _glow.style.top = -spread;
            _glow.style.right = -spread;
            _glow.style.bottom = -spread;
            float radius = GlowCornerRadius + spread;
            _glow.style.borderTopLeftRadius = radius;
            _glow.style.borderTopRightRadius = radius;
            _glow.style.borderBottomLeftRadius = radius;
            _glow.style.borderBottomRightRadius = radius;
            _glowIntensity = tell == null ? 0f : tell.Intensity;
            _glowPulsesPerSecond = tell == null ? 0f : tell.PulsesPerSecond;
            _glowLevel = 0f;
            _glowClock = 0f;

            _phase = AnimationPhase.None;
            _position = position;
            _scale = 1f;
            _opacity = 1f;
            _flipScaleX = 1f;
            _swell = 0f;
            _dragOffset = 0f;
            _isHiddenWhenMoved = false;
            _root.style.display = DisplayStyle.Flex;
            SetFaceUp(false);
            Apply();
        }

        /// <summary>Flips the card over quickly.</summary>
        public void RevealFast(float flipSeconds)
        {
            _dragOffset = 0f;
            StartFlip(flipSeconds);
        }

        /// <summary>Builds the rarity glow on the face-down card, then flips it slowly.</summary>
        public void RevealSlow(float tellSeconds, float flipSeconds)
        {
            _dragOffset = 0f;
            _flipHalfSeconds = Mathf.Max(0.005f, flipSeconds * 0.5f);
            StartPhase(AnimationPhase.Tell, tellSeconds);
        }

        /// <summary>Turns the card face up at once, with its glow at full strength.</summary>
        public void ShowFaceUp()
        {
            SetFaceUp(true);
            _flipScaleX = 1f;
            _swell = 0f;
            _glowLevel = 1f;
            if (_phase != AnimationPhase.Move)
            {
                _phase = AnimationPhase.None;
            }

            Apply();
        }

        /// <summary>Moves the card, optionally fading it and hiding it when it arrives.</summary>
        public void MoveTo(Vector2 target, float scale, float opacity, float seconds, bool isHiddenWhenMoved)
        {
            _moveFrom = new Vector2(_position.x + _dragOffset, _position.y);
            _dragOffset = 0f;
            _moveTo = target;
            _scaleFrom = _scale;
            _scaleTo = scale;
            _opacityFrom = _opacity;
            _opacityTo = opacity;
            _isHiddenWhenMoved = isHiddenWhenMoved;
            _root.style.display = DisplayStyle.Flex;
            StartPhase(AnimationPhase.Move, seconds);
        }

        /// <summary>Places the card at once, without animating.</summary>
        public void PlaceAt(Vector2 position, float scale, float opacity)
        {
            _phase = AnimationPhase.None;
            _position = position;
            _scale = scale;
            _opacity = opacity;
            _dragOffset = 0f;
            _isHiddenWhenMoved = false;
            _root.style.display = DisplayStyle.Flex;
            Apply();
        }

        /// <summary>Shifts the card sideways while the player drags it; ignored during an animation.</summary>
        public void SetDragOffset(float offset)
        {
            if (IsAnimating)
            {
                return;
            }

            _dragOffset = offset;
            Apply();
        }

        /// <summary>Jumps every running animation step to its end state.</summary>
        public void Finish()
        {
            // Each step hands over to the next when it ends; a pack has at most four steps in a row.
            while (_phase != AnimationPhase.None)
            {
                _elapsed = _duration;
                Step();
            }

            Apply();
        }

        /// <summary>Advances the animation and the glow pulse by one frame.</summary>
        public void Tick(float deltaSeconds)
        {
            _glowClock += deltaSeconds;
            if (_phase != AnimationPhase.None)
            {
                _elapsed += deltaSeconds;
                Step();
            }

            Apply();
        }

        private void StartFlip(float flipSeconds)
        {
            _flipHalfSeconds = Mathf.Max(0.005f, flipSeconds * 0.5f);
            StartPhase(AnimationPhase.FlipToEdge, _flipHalfSeconds);
        }

        private void StartPhase(AnimationPhase phase, float seconds)
        {
            _phase = phase;
            _elapsed = 0f;
            _duration = Mathf.Max(0f, seconds);
            Step();
        }

        private void Step()
        {
            float progress = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
            switch (_phase)
            {
                case AnimationPhase.Tell:
                    _glowLevel = progress;
                    _swell = TellSwell * progress;
                    if (progress >= 1f) StartPhase(AnimationPhase.FlipToEdge, _flipHalfSeconds);
                    break;

                case AnimationPhase.FlipToEdge:
                    _flipScaleX = 1f - EaseIn(progress);
                    if (progress >= 1f)
                    {
                        SetFaceUp(true);
                        _glowLevel = 1f;
                        _swell = 0f;
                        StartPhase(AnimationPhase.FlipFromEdge, _flipHalfSeconds);
                    }

                    break;

                case AnimationPhase.FlipFromEdge:
                    _flipScaleX = EaseOut(progress);
                    if (progress >= 1f) _phase = AnimationPhase.None;
                    break;

                case AnimationPhase.Move:
                    float eased = EaseOut(progress);
                    _position = Vector2.LerpUnclamped(_moveFrom, _moveTo, eased);
                    _scale = Mathf.LerpUnclamped(_scaleFrom, _scaleTo, eased);
                    _opacity = Mathf.LerpUnclamped(_opacityFrom, _opacityTo, eased);
                    if (progress >= 1f)
                    {
                        _phase = AnimationPhase.None;
                        if (_isHiddenWhenMoved) _root.style.display = DisplayStyle.None;
                    }

                    break;
            }
        }

        private void Apply()
        {
            float scale = _scale * (1f + _swell);
            _root.style.translate = new Translate(_position.x + _dragOffset, _position.y);
            _root.style.scale = new Scale(new Vector2(scale * _flipScaleX, scale));
            _root.style.opacity = _opacity;

            float pulse = _glowPulsesPerSecond <= 0f
                ? 1f
                : Mathf.Lerp(GlowPulseFloor, 1f, 0.5f + 0.5f * Mathf.Sin(_glowClock * _glowPulsesPerSecond * 2f * Mathf.PI));
            _glow.style.opacity = _glowIntensity * _glowLevel * pulse;
        }

        private void SetFaceUp(bool isFaceUp)
        {
            IsFaceUp = isFaceUp;
            _face.style.display = isFaceUp ? DisplayStyle.Flex : DisplayStyle.None;
            _back.style.display = isFaceUp ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static float EaseIn(float progress) => progress * progress;

        private static float EaseOut(float progress) => 1f - (1f - progress) * (1f - progress);
    }
}
