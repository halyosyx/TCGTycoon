using System.Globalization;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// Accent badge with a copy count, e.g. "x2". With <see cref="Corner"/> it overhangs the bottom-right
    /// corner of its parent (a card face or pocket), which should not clip overflow.
    /// </summary>
    [UxmlElement]
    public partial class CopyBadge : Label
    {
        public const string ClassName = "copy-badge";
        public const string CornerClassName = ClassName + "--corner";
        private const string CountPrefix = "x";

        private int _count = 1;
        private bool _isCorner;

        public CopyBadge()
        {
            AddToClassList(ClassName);
            AddToClassList(KitClasses.FontBodyExtraBold);
            ApplyCount();
        }

        [UxmlAttribute]
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                ApplyCount();
            }
        }

        [UxmlAttribute]
        public bool Corner
        {
            get => _isCorner;
            set
            {
                _isCorner = value;
                EnableInClassList(CornerClassName, value);
            }
        }

        private void ApplyCount()
        {
            text = CountPrefix + _count.ToString(CultureInfo.InvariantCulture);
        }
    }
}
