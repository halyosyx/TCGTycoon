using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>A keyboard key drawn as a raised cap, e.g. "Tab" or "E". Set the key with <c>text</c>.</summary>
    [UxmlElement]
    public partial class Keycap : Label
    {
        public const string ClassName = "keycap";

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
    }
}
