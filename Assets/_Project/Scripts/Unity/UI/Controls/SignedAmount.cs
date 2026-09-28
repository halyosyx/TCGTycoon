using Game.Core.Common;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// A change in money: caret glyph plus signed amount, e.g. "▲ +$14.00" or "▼ −$150.00".
    /// Style guide §5: gains and losses always carry a sign and a caret, never colour alone.
    /// </summary>
    [UxmlElement]
    public partial class SignedAmount : VisualElement
    {
        public const string ClassName = "signed-amount";
        public const string PositiveClassName = ClassName + "--positive";
        public const string NegativeClassName = ClassName + "--negative";
        public const string CaretClassName = ClassName + "__caret";
        public const string ValueClassName = ClassName + "__value";

        private readonly VisualElement _caret;
        private readonly Label _value;
        private long _cents;

        public SignedAmount()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
            _caret = new VisualElement { pickingMode = PickingMode.Ignore };
            _caret.AddToClassList(CaretClassName);
            _value = new Label { pickingMode = PickingMode.Ignore };
            _value.AddToClassList(KitClasses.NumberSmall);
            _value.AddToClassList(ValueClassName);
            Add(_caret);
            Add(_value);
            Cents = 0;
        }

        /// <summary>The change in cents; zero shows as a gain.</summary>
        [UxmlAttribute]
        public long Cents
        {
            get => _cents;
            set
            {
                _cents = value;
                _value.text = Money.FormatDelta(value);
                EnableInClassList(PositiveClassName, value >= 0);
                EnableInClassList(NegativeClassName, value < 0);
            }
        }

        public bool IsPositive => _cents >= 0;

        public string Text => _value.text;
    }
}
