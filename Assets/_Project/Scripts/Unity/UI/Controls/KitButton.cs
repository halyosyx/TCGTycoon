using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// The kit's button: primary, secondary or ghost. Named KitButton because UnityEngine.UIElements
    /// already has a Button, which this extends for clicks, focus and the optional left icon
    /// (<c>iconImage</c>). Hover, pressed, focus and disabled come from pseudo-classes; the matching
    /// modifier classes force a state (used by the component gallery).
    /// </summary>
    [UxmlElement]
    public partial class KitButton : Button
    {
        public const string KitClassName = "kit-button";
        public const string HoverClassName = KitClassName + "--hover";
        public const string PressedClassName = KitClassName + "--pressed";
        public const string FocusClassName = KitClassName + "--focus";
        public const string DisabledClassName = KitClassName + "--disabled";

        private ButtonVariant _variant = ButtonVariant.Primary;

        public KitButton()
        {
            AddToClassList(KitClassName);
            AddToClassList(KitClasses.TextLabel);
            ApplyVariant();
        }

        public enum ButtonVariant
        {
            Primary,
            Secondary,
            Ghost,
        }

        [UxmlAttribute]
        public ButtonVariant Variant
        {
            get => _variant;
            set
            {
                _variant = value;
                ApplyVariant();
            }
        }

        private void ApplyVariant()
        {
            EnableInClassList(KitClassName + "--primary", _variant == ButtonVariant.Primary);
            EnableInClassList(KitClassName + "--secondary", _variant == ButtonVariant.Secondary);
            EnableInClassList(KitClassName + "--ghost", _variant == ButtonVariant.Ghost);
        }
    }
}
