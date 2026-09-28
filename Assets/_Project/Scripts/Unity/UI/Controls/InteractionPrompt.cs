using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// "What happens if I press this" under the crosshair (style guide §6): keycap, verb and object,
    /// e.g. [LMB] Open  Booster pack. Hidden until <see cref="Show"/>.
    /// </summary>
    [UxmlElement]
    public partial class InteractionPrompt : VisualElement
    {
        public const string ClassName = "interaction-prompt";
        public const string VerbClassName = ClassName + "__verb";
        public const string ObjectClassName = ClassName + "__object";

        private readonly Keycap _key;
        private readonly Label _verb;
        private readonly Label _object;

        public InteractionPrompt()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
            _key = new Keycap();
            _verb = new Label();
            _verb.AddToClassList(KitClasses.TextSubheading);
            _verb.AddToClassList(VerbClassName);
            _object = new Label();
            _object.AddToClassList(KitClasses.TextSubheading);
            _object.AddToClassList(KitClasses.TextMuted);
            _object.AddToClassList(ObjectClassName);
            Add(_key);
            Add(_verb);
            Add(_object);
            Hide();
        }

        public bool IsShown => style.display != DisplayStyle.None;

        public string KeyText => _key.text;

        public string VerbText => _verb.text;

        public string ObjectText => _object.text;

        public void Show(string key, string verb, string objectName)
        {
            _key.text = key ?? string.Empty;
            _verb.text = verb ?? string.Empty;
            _object.text = objectName ?? string.Empty;
            _object.style.display = string.IsNullOrEmpty(_object.text) ? DisplayStyle.None : DisplayStyle.Flex;
            style.display = DisplayStyle.Flex;
        }

        public void Hide() => style.display = DisplayStyle.None;
    }
}
