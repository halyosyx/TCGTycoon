using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// An outlined chip naming a rarity tier: glyph plus tier name, or the short code (C, U, R, H, FA,
    /// AI, SI) when <see cref="Compact"/>.
    /// </summary>
    [UxmlElement]
    public partial class TierChip : VisualElement
    {
        public const string ClassName = "tier-chip";
        public const string NameClassName = ClassName + "__name";

        private readonly TierGlyph _glyph;
        private readonly Label _name;
        private int _tier = TierDisplay.Lowest;
        private bool _isCompact;
        private string _tierClass;

        public TierChip()
        {
            AddToClassList(ClassName);
            _glyph = new TierGlyph();
            _name = new Label();
            _name.AddToClassList(KitClasses.TextLabel);
            _name.AddToClassList(NameClassName);
            Add(_glyph);
            Add(_name);
            Refresh();
        }

        /// <summary>Tier number, 1 (Common) to 7 (Special Illustration).</summary>
        [UxmlAttribute]
        public int Tier
        {
            get => _tier;
            set
            {
                _tier = TierDisplay.Clamp(value);
                Refresh();
            }
        }

        [UxmlAttribute]
        public bool Compact
        {
            get => _isCompact;
            set
            {
                _isCompact = value;
                Refresh();
            }
        }

        /// <summary>The text the chip shows: the tier name, or its code when compact.</summary>
        public string Text => _name.text;

        private void Refresh()
        {
            _glyph.Tier = _tier;
            _name.text = _isCompact ? TierDisplay.CodeOf(_tier) : TierDisplay.NameOf(_tier);
            if (_tierClass != null)
            {
                RemoveFromClassList(_tierClass);
            }

            _tierClass = TierDisplay.ModifierClass(ClassName, _tier);
            AddToClassList(_tierClass);
        }
    }
}
