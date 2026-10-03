using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// A keyboard key drawn as a raised cap, e.g. "Tab" or "E". Set the key with <c>text</c>, or use
    /// <see cref="WithIcon"/> for keys whose glyph is an Icons.uss icon (arrows, minus, plus).
    /// </summary>
    [UxmlElement]
    public partial class Keycap : Label
    {
        public const string ClassName = "keycap";
        public const string IconClassName = ClassName + "--icon";
        public const string IconPartClassName = ClassName + "__icon";

        public Keycap()
            : this(string.Empty)
        {
        }

        public Keycap(string key)
            : base(key)
        {
            AddToClassList(ClassName);
            AddToClassList(KitClasses.FontDisplayBold);
        }

        /// <summary>A keycap showing the icon <c>icon--{iconName}</c> (Icons.uss must be loaded by the panel).</summary>
        public static Keycap WithIcon(string iconName)
        {
            var keycap = new Keycap(string.Empty);
            keycap.AddToClassList(IconClassName);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("icon");
            icon.AddToClassList("icon--" + iconName);
            icon.AddToClassList(IconPartClassName);
            keycap.Add(icon);
            return keycap;
        }
    }
}
