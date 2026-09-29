// PROTOTYPE: temporary Binder UI. Replace, don't extend. See Docs/UI/UI_STYLE_GUIDE.md §8.

using Game.Unity.UI.Controls;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Prototype
{
    /// <summary>One binder pocket: a card face with a copy badge, or an empty pocket. Pooled.</summary>
    internal sealed class BinderPocket
    {
        public const string ClassName = "binder-pocket";
        public const string SpacedClassName = ClassName + "--spaced";
        public const string EmptyClassName = ClassName + "--empty";
        public const string SelectedClassName = ClassName + "--selected";

        private readonly CardFace _card;
        private readonly CopyBadge _badge;
        private readonly Label _empty;

        public BinderPocket()
        {
            Root = new VisualElement { userData = this };
            Root.AddToClassList(ClassName);

            _card = new CardFace();
            _card.AddToClassList(ClassName + "__card");
            _badge = new CopyBadge { Corner = true };
            _empty = new Label("Empty pocket");
            _empty.AddToClassList(KitClasses.TextCaption);
            _empty.AddToClassList(ClassName + "__empty-label");

            Root.Add(_card);
            Root.Add(_badge);
            Root.Add(_empty);
        }

        public VisualElement Root { get; }

        /// <summary>The item in this pocket, or null when it is empty.</summary>
        public BinderEntry Entry { get; private set; }

        public void Bind(BinderEntry entry)
        {
            Entry = entry;
            bool isEmpty = entry == null;
            Root.EnableInClassList(EmptyClassName, isEmpty);
            _card.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;
            _empty.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;

            // One copy needs no badge: the card in the pocket is that copy.
            _badge.style.display = !isEmpty && entry.Copies > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            if (isEmpty)
            {
                return;
            }

            if (entry.Tier.HasValue)
            {
                _card.SetCard(entry.Tier.Value, entry.DisplayName, entry.ItemId);
            }
            else
            {
                _card.SetProduct(entry.DisplayName, entry.Detail);
            }

            _badge.Count = entry.Copies;
        }

        public void SetSelected(bool isSelected) => Root.EnableInClassList(SelectedClassName, isSelected);
    }
}
