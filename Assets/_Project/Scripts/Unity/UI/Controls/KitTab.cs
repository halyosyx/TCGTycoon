using System.Globalization;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// A tab with a label and a count, e.g. "Set A 34". Active tabs are wider and filled with the
    /// accent. Named KitTab because UnityEngine.UIElements already has a Tab. The container positions
    /// it so it sticks out of its edge; screens handle clicks and keys.
    /// </summary>
    [UxmlElement]
    public partial class KitTab : VisualElement
    {
        public const string ClassName = "kit-tab";
        public const string ActiveClassName = ClassName + "--active";
        public const string HoverClassName = ClassName + "--hover";
        public const string LabelClassName = ClassName + "__label";
        public const string CountClassName = ClassName + "__count";

        private readonly Label _label;
        private readonly Label _count;
        private int _countValue;
        private bool _isActive;

        public KitTab()
        {
            AddToClassList(ClassName);
            _label = new Label();
            _label.AddToClassList(KitClasses.TextLabel);
            _label.AddToClassList(LabelClassName);
            _count = new Label();
            _count.AddToClassList(KitClasses.NumberSmall);
            _count.AddToClassList(CountClassName);
            Add(_label);
            Add(_count);
            Count = 0;
        }

        [UxmlAttribute]
        public string Title
        {
            get => _label.text;
            set => _label.text = value ?? string.Empty;
        }

        [UxmlAttribute]
        public int Count
        {
            get => _countValue;
            set
            {
                _countValue = value;
                _count.text = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        [UxmlAttribute]
        public bool Active
        {
            get => _isActive;
            set
            {
                _isActive = value;
                EnableInClassList(ActiveClassName, value);
            }
        }

        /// <summary>The count as shown.</summary>
        public string CountText => _count.text;
    }
}
