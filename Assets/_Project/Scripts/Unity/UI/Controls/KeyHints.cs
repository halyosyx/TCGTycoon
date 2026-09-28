using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// A row of keycaps with labels, e.g. "Tab Inventory · Esc Pause". Fill it with
    /// <see cref="AddHint"/>, or in UXML with <see cref="Hints"/> as "Tab:Inventory;Esc:Pause".
    /// <see cref="OnWorld"/> switches to the HUD's translucent plate and world-text shadow.
    /// </summary>
    [UxmlElement]
    public partial class KeyHints : VisualElement
    {
        public const string ClassName = "key-hints";
        public const string OnWorldClassName = ClassName + "--on-world";
        public const string HintClassName = ClassName + "__hint";
        public const string SpacedHintClassName = HintClassName + "--spaced";
        public const string LabelClassName = ClassName + "__label";

        private const char HintSeparator = ';';
        private const char KeySeparator = ':';

        private string _hints = string.Empty;
        private bool _isOnWorld;

        public KeyHints()
        {
            AddToClassList(ClassName);
        }

        /// <summary>Hints as "Key:Label" pairs separated by semicolons. Setting it replaces every hint.</summary>
        [UxmlAttribute]
        public string Hints
        {
            get => _hints;
            set
            {
                _hints = value ?? string.Empty;
                ClearHints();
                foreach (string pair in _hints.Split(HintSeparator))
                {
                    int split = pair.IndexOf(KeySeparator);
                    if (split > 0)
                    {
                        AddHint(pair.Substring(0, split).Trim(), pair.Substring(split + 1).Trim());
                    }
                }
            }
        }

        /// <summary>True over the 3D world (HUD): translucent plate and text shadow.</summary>
        [UxmlAttribute]
        public bool OnWorld
        {
            get => _isOnWorld;
            set
            {
                _isOnWorld = value;
                EnableInClassList(OnWorldClassName, value);
                EnableInClassList(KitClasses.TextOnWorld, value);
            }
        }

        public int HintCount => childCount;

        public void AddHint(string key, string label)
        {
            var hint = new VisualElement();
            hint.AddToClassList(HintClassName);
            if (childCount > 0)
            {
                // USS has no gap, so every hint after the first carries the spacing.
                hint.AddToClassList(SpacedHintClassName);
            }

            hint.Add(new Keycap(key));
            var text = new Label(label);
            text.AddToClassList(KitClasses.TextCaption);
            text.AddToClassList(LabelClassName);
            hint.Add(text);
            Add(hint);
        }

        public void ClearHints() => Clear();
    }
}
