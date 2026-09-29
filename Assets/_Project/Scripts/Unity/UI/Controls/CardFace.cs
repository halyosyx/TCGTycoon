using System;
using System.Globalization;
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

        /// <summary>Sealed-product face: neutral frame, no glyph, no tier colour (see <see cref="SetProduct"/>).</summary>
        public const string NeutralClassName = ClassName + "--neutral";

        // Style guide §6: border width is about a fortieth of the card's width, never under the
        // --border-width-card-min token (read through the custom property .card-face sets).
        private const float BorderWidthShare = 1f / 40f;

        // A px value only reads back as a string ("3px"): Unity reads custom floats from unitless numbers only.
        private static readonly CustomStyleProperty<string> s_minimumBorderWidthProperty = new CustomStyleProperty<string>("--card-face-border-min");
        private const string PixelUnit = "px";

        private readonly TierGlyph _glyph;
        private readonly Label _id;
        private readonly Label _name;
        private readonly Label _tierName;
        private int _tier = TierDisplay.Lowest;
        private string _tierClass;
        private float _borderWidth;
        private float _minimumBorderWidth;
        private float _width;

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
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
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

        /// <summary>
        /// Shows a sealed product instead of a card: set in UXML to preview it (e.g. "Set A · 36 packs").
        /// Setting it to empty leaves the face as it is; <see cref="SetCard"/> returns to a card.
        /// </summary>
        [UxmlAttribute]
        public string ProductDetail
        {
            get => IsProduct ? _tierName.text : string.Empty;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SetProduct(_name.text, value);
                }
            }
        }

        /// <summary>The tier name shown under the card name (or a product's detail line).</summary>
        public string TierName => _tierName.text;

        /// <summary>True while showing a sealed product rather than a card.</summary>
        public bool IsProduct => ClassListContains(NeutralClassName);

        /// <summary>
        /// Placeholder face for a sealed product (pack, bundle, box): neutral frame with no tier colour
        /// or glyph, the product name bottom-left and a detail line such as "Set A · 36 packs" beneath it.
        /// </summary>
        public void SetProduct(string productName, string detail)
        {
            if (_tierClass != null)
            {
                RemoveFromClassList(_tierClass);
                _tierClass = null;
            }

            AddToClassList(NeutralClassName);
            _glyph.style.display = DisplayStyle.None;
            _name.text = productName ?? string.Empty;
            _tierName.text = detail ?? string.Empty;
            _id.text = string.Empty;
        }

        /// <summary>Current border width in panel pixels.</summary>
        public float BorderWidth => _borderWidth;

        /// <summary>The border floor from --border-width-card-min; 0 until the face's styles resolve.</summary>
        public float MinimumBorderWidth => _minimumBorderWidth;

        /// <summary>Shows a card in one call.</summary>
        public void SetCard(int tier, string cardName, string cardId)
        {
            Tier = tier;
            CardName = cardName;
            CardId = cardId;
        }

        /// <summary>Border width for a card of the given width, never under the floor (style guide §6).</summary>
        public static float BorderWidthFor(float cardWidth, float minimumWidth)
        {
            return Mathf.Max(minimumWidth, Mathf.Round(cardWidth * BorderWidthShare));
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_minimumBorderWidthProperty, out string value)
                && TryParsePixels(value, out float minimumWidth)
                && !Mathf.Approximately(minimumWidth, _minimumBorderWidth))
            {
                _minimumBorderWidth = minimumWidth;
                SetBorderWidth(BorderWidthFor(_width, _minimumBorderWidth));
            }
        }

        /// <summary>Reads a USS length such as "3px" or "3" into panel pixels.</summary>
        public static bool TryParsePixels(string value, out float pixels)
        {
            string number = value == null ? string.Empty : value.Trim();
            if (number.EndsWith(PixelUnit, StringComparison.Ordinal))
            {
                number = number.Substring(0, number.Length - PixelUnit.Length);
            }

            return float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out pixels);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > 0f)
            {
                _width = evt.newRect.width;
                SetBorderWidth(BorderWidthFor(_width, _minimumBorderWidth));
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

        // Any tier change turns a product face back into a card face.
        private void ApplyTier()
        {
            RemoveFromClassList(NeutralClassName);
            _glyph.style.display = DisplayStyle.Flex;
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
