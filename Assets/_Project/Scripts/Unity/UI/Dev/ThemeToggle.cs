using Game.Unity.UI.Controls;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Dev
{
    /// <summary>
    /// Development aid for the component gallery: a toggle that adds or removes <c>.theme-light</c> on the
    /// nearest ancestor carrying <see cref="TargetClass"/>, so both themes can be checked side by side.
    /// Not for game screens.
    /// </summary>
    [UxmlElement]
    public partial class ThemeToggle : Toggle
    {
        public const string DefaultTargetClass = "component-gallery";

        public ThemeToggle()
        {
            label = "Light theme";
            this.RegisterValueChangedCallback(OnValueChanged);
        }

        /// <summary>Class of the ancestor that receives <c>.theme-light</c>.</summary>
        [UxmlAttribute]
        public string TargetClass { get; set; } = DefaultTargetClass;

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            for (VisualElement current = parent; current != null; current = current.parent)
            {
                if (current.ClassListContains(TargetClass))
                {
                    current.EnableInClassList(KitClasses.ThemeLight, evt.newValue);
                    return;
                }
            }
        }
    }
}
