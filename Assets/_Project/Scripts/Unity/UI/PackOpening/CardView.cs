using System;
using Game.Core.Content;
using Game.Unity.Cards;
using Game.Unity.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// One card on the pack opening screen: the generated card template as the face, with a one-shot
    /// flash for rare tiers (a light pass over the face and a tier-coloured halo behind the card, local to
    /// the card, never repeating). A card waiting lower in the stack is covered by a plain back, so the
    /// pile's edges never give its tier away; it is face up the moment it reaches the top (no flip).
    /// Pooled and re-bound for every pack. Animation is stepped by <see cref="Tick"/> and only writes
    /// struct style values, so it allocates nothing per frame. The flash never counts as animating and
    /// never takes the pointer, so swiping goes straight through it.
    /// </summary>
    public sealed class CardView
    {
        public const string ClassName = "card-view";

        /// <summary>Card width in panel pixels; matches `.card-view` in PackOpening.uss and `.card` in the generated template.</summary>
        public const float Width = 250f;

        /// <summary>Card height in panel pixels; the fallback when the element hasn't been laid out yet.</summary>
        public const float Height = 350f;

        private const string FaceClassName = "card-view__face";
        private const float HaloCornerRadius = 16f;
        private const float FaceCornerRadius = 12f;
        private const float CoverBorderWidth = 6f;

        // The flash rises over this share of its duration, then fades over the rest, so it reads as a glow
        // rather than a blink.
        private const float FlashRiseShare = 0.2f;

        // The light pass over the face is gentler than the halo, so the card stays readable mid-flash.
        private const float FaceFlashShare = 0.6f;

        private readonly VisualElement _root;
        private readonly VisualElement _halo;
        private readonly VisualElement _face;
        private readonly VisualElement _flash;
        private readonly VisualElement _cover;

        private AnimationPhase _phase;
        private float _elapsed;
        private float _duration;

        private Vector2 _position;
        private Vector2 _moveFrom;
        private Vector2 _moveTo;
        private float _scale;
        private float _scaleFrom;
        private float _scaleTo;
        private float _opacity;
        private float _opacityFrom;
        private float _opacityTo;
        private float _rotation;
        private bool _isHiddenWhenMoved;
        private float _dragOffset;
        private float _hoverFactor;
        private float _hoverTarget;
        private float _hoverRate;

        private float _swipeDirection;
        private float _swipeDistance;
        private float _swipeDrop;
        private float _swipeCurve;
        private float _swipeTilt;

        private float _flashPeak;
        private float _flashSeconds;
        private float _flashElapsed;

        public CardView(VisualTreeAsset cardTemplate)
        {
            if (cardTemplate == null) throw new ArgumentNullException(nameof(cardTemplate));

            _root = new VisualElement { name = ClassName };
            _root.AddToClassList(ClassName);

            _halo = CreateOverlay(HaloCornerRadius);
            _face = cardTemplate.Instantiate();
            _face.AddToClassList(FaceClassName);
            _flash = CreateOverlay(FaceCornerRadius);
            _cover = CreateOverlay(FaceCornerRadius);
            _cover.style.opacity = 1f;
            _cover.style.borderTopWidth = CoverBorderWidth;
            _cover.style.borderRightWidth = CoverBorderWidth;
            _cover.style.borderBottomWidth = CoverBorderWidth;
            _cover.style.borderLeftWidth = CoverBorderWidth;

            _root.Add(_halo);
            _root.Add(_face);
            _root.Add(_flash);
            _root.Add(_cover);
        }

        private enum AnimationPhase
        {
            None,
            Move,
            Swipe,
        }

        public VisualElement Root => _root;

        public bool IsAnimating => _phase != AnimationPhase.None;

        /// <summary>True while a rarity flash is still fading (it doesn't count as animating).</summary>
        public bool IsFlashing => _flashElapsed < _flashSeconds && _flashPeak > 0f;

        public Vector2 Position => _position;

        /// <summary>The card's rest-layout rectangle in its parent, before translate and scale.</summary>
        public Rect LayoutRect => _root.layout;

        /// <summary>
        /// Shows a new card at the given position and scale, with every animation reset: face up, or
        /// covered by a plain back while it waits lower in the stack (<see cref="Uncover"/>).
        /// </summary>
        public void Bind(Card card, RarityPaletteDefinition palette, Vector2 position, float scale, bool isCovered)
        {
            CardTemplate.Bind(_face, card, palette);
            _halo.style.backgroundColor = palette.ColorOf(card.Tier);
            _flash.style.backgroundColor = palette.CardTextColor;
            _cover.style.backgroundColor = palette.PackColor;
            Color coverBorder = palette.PackLabelColor;
            _cover.style.borderTopColor = coverBorder;
            _cover.style.borderRightColor = coverBorder;
            _cover.style.borderBottomColor = coverBorder;
            _cover.style.borderLeftColor = coverBorder;
            _cover.style.display = isCovered ? DisplayStyle.Flex : DisplayStyle.None;
            SetHaloSpread(0f);

            _phase = AnimationPhase.None;
            _position = position;
            _scale = scale;
            _opacity = 1f;
            _rotation = 0f;
            _dragOffset = 0f;
            _hoverFactor = 1f;
            _hoverTarget = 1f;
            _hoverRate = 0f;
            _isHiddenWhenMoved = false;
            _flashPeak = 0f;
            _flashSeconds = 0f;
            _flashElapsed = 0f;
            _root.style.display = DisplayStyle.Flex;
            Apply();
        }

        /// <summary>The card has reached the top of the stack (or is being peeked at under it): face up at once, no flip.</summary>
        public void Uncover() => _cover.style.display = DisplayStyle.None;

        /// <summary>Back to a plain back, waiting lower in the stack.</summary>
        public void Cover() => _cover.style.display = DisplayStyle.Flex;

        /// <summary>
        /// The card's rarity reaction, once: a quick flash over the face and a halo in the tier colour that
        /// both fade over the tell's duration. No tell, or a zero flash, does nothing.
        /// </summary>
        public void PlayTell(TierTell tell)
        {
            if (TierTell.KindOf(tell) == TierTellKind.None)
            {
                return;
            }

            _flashPeak = tell.FlashIntensity;
            _flashSeconds = tell.FlashSeconds;
            _flashElapsed = 0f;
            SetHaloSpread(tell.HaloSpread);
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
            _rotation = 0f;
            _isHiddenWhenMoved = isHiddenWhenMoved;
            _root.style.display = DisplayStyle.Flex;
            StartPhase(AnimationPhase.Move, seconds);
        }

        /// <summary>
        /// Sends the card off the stack along a downward curve (<see cref="SwipePath"/>), leaning into the
        /// swipe and fading, then hides it.
        /// </summary>
        /// <param name="direction">+1 to the right, −1 to the left.</param>
        public void SwipeAway(float direction, float distance, float drop, float curve, float tiltDegrees, float seconds)
        {
            _moveFrom = new Vector2(_position.x + _dragOffset, _position.y);
            _dragOffset = 0f;
            _swipeDirection = direction < 0f ? -1f : 1f;
            _swipeDistance = distance;
            _swipeDrop = drop;
            _swipeCurve = curve;
            _swipeTilt = tiltDegrees;
            _opacityFrom = _opacity;
            _isHiddenWhenMoved = true;
            StartPhase(AnimationPhase.Swipe, seconds);
        }

        /// <summary>Places the card at once, without animating.</summary>
        public void PlaceAt(Vector2 position, float scale, float opacity)
        {
            _phase = AnimationPhase.None;
            _position = position;
            _scale = scale;
            _opacity = opacity;
            _rotation = 0f;
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

        /// <summary>
        /// Eases an extra scale factor on top of the card's own scale (1 = none), reaching it in
        /// <paramref name="seconds"/>. Used for the row hover; safe to call every frame.
        /// </summary>
        public void SetHoverTarget(float factor, float seconds)
        {
            if (Mathf.Approximately(factor, _hoverTarget))
            {
                return;
            }

            _hoverTarget = factor;
            _hoverRate = Mathf.Abs(factor - _hoverFactor) / Mathf.Max(seconds, 0.001f);
        }

        /// <summary>Jumps the running move or swipe to its end state. A flash keeps fading on its own.</summary>
        public void Finish()
        {
            if (_phase != AnimationPhase.None)
            {
                _elapsed = _duration;
                Step();
            }

            Apply();
        }

        /// <summary>Advances the animation and the flash by one frame.</summary>
        public void Tick(float deltaSeconds)
        {
            _hoverFactor = Mathf.MoveTowards(_hoverFactor, _hoverTarget, _hoverRate * deltaSeconds);
            if (_flashElapsed < _flashSeconds)
            {
                _flashElapsed += deltaSeconds;
            }

            if (_phase != AnimationPhase.None)
            {
                _elapsed += deltaSeconds;
                Step();
            }

            Apply();
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
                case AnimationPhase.Move:
                    float eased = EaseOut(progress);
                    _position = Vector2.LerpUnclamped(_moveFrom, _moveTo, eased);
                    _scale = Mathf.LerpUnclamped(_scaleFrom, _scaleTo, eased);
                    _opacity = Mathf.LerpUnclamped(_opacityFrom, _opacityTo, eased);
                    if (progress >= 1f) EndPhase();
                    break;

                case AnimationPhase.Swipe:
                    _position = SwipePath.Evaluate(_moveFrom, _swipeDirection, _swipeDistance, _swipeDrop, _swipeCurve, progress);
                    _rotation = SwipePath.Tilt(_swipeDirection, _swipeTilt, progress);
                    _opacity = Mathf.Lerp(_opacityFrom, 0f, progress * progress);
                    if (progress >= 1f) EndPhase();
                    break;
            }
        }

        private void EndPhase()
        {
            _phase = AnimationPhase.None;
            if (_isHiddenWhenMoved)
            {
                _root.style.display = DisplayStyle.None;
            }
        }

        private void Apply()
        {
            float scale = _scale * _hoverFactor;
            _root.style.translate = new Translate(_position.x + _dragOffset, _position.y);
            _root.style.scale = new Scale(new Vector2(scale, scale));
            _root.style.rotate = new Rotate(_rotation);
            _root.style.opacity = _opacity;

            // One shot: a short smooth rise to the peak, a slow smooth fade, then it stays at zero.
            float flash = 0f;
            if (_flashPeak > 0f && _flashSeconds > 0f && _flashElapsed < _flashSeconds)
            {
                float t = _flashElapsed / _flashSeconds;
                float envelope = t < FlashRiseShare
                    ? Mathf.SmoothStep(0f, 1f, t / FlashRiseShare)
                    : Mathf.SmoothStep(1f, 0f, (t - FlashRiseShare) / (1f - FlashRiseShare));
                flash = _flashPeak * envelope;
            }

            _flash.style.opacity = flash * FaceFlashShare;
            _halo.style.opacity = flash;
        }

        private void SetHaloSpread(float spread)
        {
            _halo.style.left = -spread;
            _halo.style.top = -spread;
            _halo.style.right = -spread;
            _halo.style.bottom = -spread;
            float radius = HaloCornerRadius + spread;
            _halo.style.borderTopLeftRadius = radius;
            _halo.style.borderTopRightRadius = radius;
            _halo.style.borderBottomLeftRadius = radius;
            _halo.style.borderBottomRightRadius = radius;
        }

        // An absolute layer over the whole card that never takes the pointer and starts invisible.
        private static VisualElement CreateOverlay(float cornerRadius)
        {
            var overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            overlay.style.position = UnityEngine.UIElements.Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.right = 0f;
            overlay.style.bottom = 0f;
            overlay.style.borderTopLeftRadius = cornerRadius;
            overlay.style.borderTopRightRadius = cornerRadius;
            overlay.style.borderBottomLeftRadius = cornerRadius;
            overlay.style.borderBottomRightRadius = cornerRadius;
            overlay.style.opacity = 0f;
            return overlay;
        }

        private static float EaseOut(float progress) => 1f - (1f - progress) * (1f - progress);
    }
}
