using System;
using Game.Core.Content;
using Game.Unity.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Element names of the UI Toolkit card template (Generated/UI/CardTemplate.uxml) and the code that
    /// fills it from a card and the palette. The names are defined here once; the generator writes the
    /// UXML from these constants, so code and template can't drift apart.
    /// </summary>
    public static class CardTemplate
    {
        public const string RootName = "card";
        public const string FaceName = "card-face";
        public const string NameLabelName = "card-name";
        public const string TierLabelName = "card-tier";
        public const string IdLabelName = "card-id";

        private const float IdLabelAlpha = 0.6f;

        /// <summary>Fills an instantiated template. Colours come from the palette, overriding the stylesheet's preview defaults.</summary>
        public static void Bind(VisualElement template, Card card, RarityPaletteDefinition palette)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (palette == null) throw new ArgumentNullException(nameof(palette));

            Color tierColor = palette.ColorOf(card.Tier);
            VisualElement root = Require<VisualElement>(template, RootName);
            root.style.borderTopColor = tierColor;
            root.style.borderRightColor = tierColor;
            root.style.borderBottomColor = tierColor;
            root.style.borderLeftColor = tierColor;
            root.style.backgroundColor = palette.CardFaceColor;

            Label nameLabel = Require<Label>(template, NameLabelName);
            nameLabel.text = card.DisplayName;
            nameLabel.style.color = palette.CardTextColor;

            Label tierLabel = Require<Label>(template, TierLabelName);
            tierLabel.text = palette.DisplayNameOf(card.Tier);
            tierLabel.style.color = tierColor;

            Label idLabel = Require<Label>(template, IdLabelName);
            Color idColor = palette.CardTextColor;
            idColor.a = IdLabelAlpha;
            idLabel.text = card.Id;
            idLabel.style.color = idColor;
        }

        private static TElement Require<TElement>(VisualElement template, string elementName)
            where TElement : VisualElement
        {
            TElement element = template.Q<TElement>(elementName);
            if (element == null)
            {
                throw new InvalidOperationException($"Card template has no {typeof(TElement).Name} named '{elementName}'.");
            }

            return element;
        }
    }
}
