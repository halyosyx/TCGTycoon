using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// One notification (style guide §6): a status square with a check icon, a title, a detail line
    /// and an optional signed amount. <see cref="ToastStack"/> creates, stacks and dismisses them.
    /// </summary>
    [UxmlElement]
    public partial class Toast : VisualElement
    {
        public const string ClassName = "toast";
        public const string PositiveClassName = ClassName + "--positive";
        public const string ShownClassName = ClassName + "--shown";
        public const string StatusClassName = ClassName + "__status";
        public const string StatusIconClassName = ClassName + "__status-icon";
        public const string TextClassName = ClassName + "__text";
        public const string TitleClassName = ClassName + "__title";
        public const string DetailClassName = ClassName + "__detail";
        public const string AmountClassName = ClassName + "__amount";

        private readonly Label _title;
        private readonly Label _detail;
        private readonly SignedAmount _amount;

        public Toast()
        {
            AddToClassList(ClassName);

            var status = new VisualElement();
            status.AddToClassList(StatusClassName);
            var icon = new VisualElement();
            icon.AddToClassList(StatusIconClassName);
            status.Add(icon);

            var text = new VisualElement();
            text.AddToClassList(TextClassName);
            _title = new Label();
            _title.AddToClassList(KitClasses.TextLabel);
            _title.AddToClassList(TitleClassName);
            _detail = new Label();
            _detail.AddToClassList(KitClasses.TextCaption);
            _detail.AddToClassList(DetailClassName);
            text.Add(_title);
            text.Add(_detail);

            _amount = new SignedAmount();
            _amount.AddToClassList(AmountClassName);

            Add(status);
            Add(text);
            Add(_amount);
            Set(ToastKind.Neutral, string.Empty, string.Empty, null);
        }

        public ToastKind Kind { get; private set; }

        public string TitleText => _title.text;

        public string DetailText => _detail.text;

        public bool HasAmount => _amount.style.display != DisplayStyle.None;

        public SignedAmount Amount => _amount;

        /// <summary>Fills the toast. A null amount hides the amount; an empty detail hides that line.</summary>
        public void Set(ToastKind kind, string title, string detail, long? amountCents)
        {
            Kind = kind;
            EnableInClassList(PositiveClassName, kind == ToastKind.Positive);
            _title.text = title ?? string.Empty;
            _detail.text = detail ?? string.Empty;
            _detail.style.display = string.IsNullOrEmpty(_detail.text) ? DisplayStyle.None : DisplayStyle.Flex;
            _amount.style.display = amountCents.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            if (amountCents.HasValue)
            {
                _amount.Cents = amountCents.Value;
            }
        }
    }
}
