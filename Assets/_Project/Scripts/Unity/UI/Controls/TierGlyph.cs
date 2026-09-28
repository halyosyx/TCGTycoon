using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>A tier's Phosphor glyph in the tier colour (circle for Common up to crown for Special Illustration).</summary>
    [UxmlElement]
    public partial class TierGlyph : VisualElement
    {
        public const string ClassName = "tier-glyph";

        private int _tier = TierDisplay.Lowest;
        private string _tierClass;

        public TierGlyph()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
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

        private void ApplyTier()
        {
            if (_tierClass != null)
            {
                RemoveFromClassList(_tierClass);
            }

            _tierClass = ClassName + "--" + _tier;
            AddToClassList(_tierClass);
        }
    }
}
