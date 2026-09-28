using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// Centre-of-view marker (style guide §7): an 8px dot, which becomes an accent ring around a
    /// smaller dot while the player aims at something usable.
    /// </summary>
    [UxmlElement]
    public partial class Crosshair : VisualElement
    {
        public const string ClassName = "crosshair";
        public const string UsableClassName = ClassName + "--usable";
        public const string DotClassName = ClassName + "__dot";
        public const string RingClassName = ClassName + "__ring";

        public Crosshair()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
            var ring = new VisualElement { pickingMode = PickingMode.Ignore };
            ring.AddToClassList(RingClassName);
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList(DotClassName);
            Add(ring);
            Add(dot);
        }

        [UxmlAttribute]
        public bool Usable
        {
            get => ClassListContains(UsableClassName);
            set => EnableInClassList(UsableClassName, value);
        }
    }
}
