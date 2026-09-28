using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// Placeholder card face: tier-coloured border, glyph top-left, card id top-right, name and tier
    /// name bottom-left. The parent sets the size (cards are 63:88); the border scales with the width.
    /// Display only: a presenter or view passes the tier number, name and id in.
    /// </summary>
    [UxmlElement]
    public partial class CardFace : VisualElement
    {
        public const string ClassName = "card-face";
        public const string TopClassName = ClassName + "__top";
        public const string GlyphClassName = ClassName + "__glyph";
        public const string IdClassName = ClassName + "__id";
        public const string BottomClassName = ClassName + "__bottom";
        public const string NameClassName = ClassName + "__name";
        public const string TierClassName = ClassName + "__tier";

        // Style guide §6: border width is about a fortieth of the card's width, never under 3 px.
        private const float BorderWidthShare = 1f / 40f;
        private const float MinimumBorderWidth = 3f;

        private readonly TierGlyph _glyph;
        private readonly Label _id;
        private readonly Label _name;
        private readonly Label _tierName;
        private int _tier = TierDisplay.Lowest;
        private string _tierClass;
        private float _borderWidth;

        public CardFace()
        {
            AddToClassList(ClassName);

            var top = new VisualElement();
            top.AddToClassList(TopClassName);
            _glyph = new TierGlyph();
            _glyph.AddToClassList(GlyphClassName);
            _id = new Label();
            _id.AddToClassList(KitClasses.NumberSmall);
            _id.AddToClassList(IdClassName);
            top.Add(_glyph);
            top.Add(_id);

            var bottom = new VisualElement();
            bottom.AddToClassList(BottomClassName);
            _name = new Label();
            _name.AddToClassList(KitClasses.FontDisplayBold);
            _name.AddToClassList(NameClassName);
            _tierName = new Label();
            _tierName.AddToClassList(KitClasses.TextCaption);
            _tierName.AddToClassList(TierClassName);
            bottom.Add(_name);
            bottom.Add(_tierName);

            Add(top);
            Add(bottom);
            SetBorderWidth(MinimumBorderWidth);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyTier();
        }

        /// <summary>Tier number, 1 (Common) to 7 (Special Illustration).</summary>
        [UxmlAttribute]
        public int Tier
        {
            get => _tier;
            set
            {
                _tier = TierDisplay.Clamp(value);
                ApplyTier();
            }
        }

        [UxmlAttribute]
        public string CardName
        {
            get => _name.text;
            set => _name.text = value ?? string.Empty;
        }

        [UxmlAttribute]
        public string CardId
        {
            get => _id.text;
            set => _id.text = value ?? string.Empty;
        }

        /// <summary>The tier name shown under the card name.</summary>
        public string TierName => _tierName.text;

        /// <summary>Current border width in panel pixels.</summary>
        public float BorderWidth => _borderWidth;

        /// <summary>Shows a card in one call.</summary>
        public void SetCard(int tier, string cardName, string cardId)
        {
            Tier = tier;
            CardName = cardName;
            CardId = cardId;
        }

        /// <summary>Border width for a card of the given width (style guide §6).</summary>
        public static float BorderWidthFor(float cardWidth)
        {
            return Mathf.Max(MinimumBorderWidth, Mathf.Round(cardWidth * BorderWidthShare));
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > 0f)
            {
                SetBorderWidth(BorderWidthFor(evt.newRect.width));
            }
        }

        private void SetBorderWidth(float width)
        {
            // The border sits inside the width, so resizing it doesn't change the width that set it.
            if (Mathf.Approximately(width, _borderWidth))
            {
                return;
            }

            _borderWidth = width;
            style.borderTopWidth = width;
            style.borderRightWidth = width;
            style.borderBottomWidth = width;
            style.borderLeftWidth = width;
        }

        private void ApplyTier()
        {
            _glyph.Tier = _tier;
            _tierName.text = TierDisplay.NameOf(_tier);
            if (_tierClass != null)
            {
                RemoveFromClassList(_tierClass);
            }

            _tierClass = TierDisplay.ModifierClass(ClassName, _tier);
            AddToClassList(_tierClass);
        }
    }
}
