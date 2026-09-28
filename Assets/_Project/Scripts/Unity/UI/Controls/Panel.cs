using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// A surface with a 1px border and large radius, and an optional header bar shown when
    /// <see cref="Title"/> is set. Children added in UXML or code go into the padded body.
    /// </summary>
    [UxmlElement]
    public partial class Panel : VisualElement
    {
        public const string ClassName = "panel";
        public const string HeaderClassName = ClassName + "__header";
        public const string BodyClassName = ClassName + "__body";

        private readonly VisualElement _header;
        private readonly Label _title;
        private readonly VisualElement _body;
        private PanelPadding _padding = PanelPadding.Medium;

        public Panel()
        {
            AddToClassList(ClassName);

            _header = new VisualElement();
            _header.AddToClassList(HeaderClassName);
            _title = new Label();
            _title.AddToClassList(KitClasses.TextSubheading);
            _header.Add(_title);

            _body = new VisualElement();
            _body.AddToClassList(BodyClassName);

            hierarchy.Add(_header);
            hierarchy.Add(_body);
            Title = string.Empty;
            ApplyPadding();
        }

        /// <summary>Padding inside the body: 16, 24 or 32 px (style guide §6).</summary>
        public enum PanelPadding
        {
            Small,
            Medium,
            Large,
        }

        public override VisualElement contentContainer => _body;

        /// <summary>Header text; the header bar is hidden while this is empty.</summary>
        [UxmlAttribute]
        public string Title
        {
            get => _title.text;
            set
            {
                _title.text = value ?? string.Empty;
                _header.style.display = string.IsNullOrEmpty(_title.text) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute]
        public PanelPadding Padding
        {
            get => _padding;
            set
            {
                _padding = value;
                ApplyPadding();
            }
        }

        private void ApplyPadding()
        {
            EnableInClassList(ClassName + "--small", _padding == PanelPadding.Small);
            EnableInClassList(ClassName + "--medium", _padding == PanelPadding.Medium);
            EnableInClassList(ClassName + "--large", _padding == PanelPadding.Large);
        }
    }
}
