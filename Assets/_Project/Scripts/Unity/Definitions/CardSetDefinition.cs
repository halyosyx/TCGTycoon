using System.Collections.Generic;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>Authoring asset for a card set. Converted into a Core <see cref="CardPool"/> at load.</summary>
    [CreateAssetMenu(menuName = "TCG/Card Set", fileName = "NewCardSet")]
    public sealed class CardSetDefinition : ScriptableObject
    {
        public const string IdField = nameof(_id);
        public const string DisplayNameField = nameof(_displayName);
        public const string ShortNameField = nameof(_shortName);
        public const string IdPrefixField = nameof(_idPrefix);
        public const string LifecycleField = nameof(_lifecycle);
        public const string PriceScalePercentField = nameof(_priceScalePercent);
        public const string ColourField = nameof(_colour);
        public const string CardsField = nameof(_cards);

        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField, Tooltip("Short label for tight spots such as the binder's tabs (e.g. \"Set A\"). Empty: the display name is used.")]
        private string _shortName;

        [SerializeField, Tooltip("Prefix of this set's card ids, e.g. \"RC\" in RC_C_014.")]
        private string _idPrefix;

        [SerializeField, Tooltip("In print (cheap, always in stock) or out of print (scarce, appreciating).")]
        private SetLifecycle _lifecycle;

        [SerializeField, Min(0), Tooltip("Card values are the tier base price times this percentage (100 in print, 180 out of print).")]
        private int _priceScalePercent = 100;

        [SerializeField, Tooltip("The set's colour: store thumbnails and pack stacks are tinted with it.")]
        private Color _colour = Color.gray;

        [SerializeField]
        private List<CardDefinition> _cards = new List<CardDefinition>();

        public string Id => _id;

        public string DisplayName => _displayName;

        /// <summary>The short name, or the display name (then the id) when it is empty.</summary>
        public string ShortName => !string.IsNullOrEmpty(_shortName) ? _shortName : !string.IsNullOrEmpty(_displayName) ? _displayName : _id;

        public string IdPrefix => _idPrefix;

        public SetLifecycle Lifecycle => _lifecycle;

        public int PriceScalePercent => _priceScalePercent;

        public Color Colour => _colour;

        public IReadOnlyList<CardDefinition> Cards => _cards;

        /// <summary>Empty entries in the card list; they're skipped when converting.</summary>
        public int MissingCardCount
        {
            get
            {
                int missing = 0;
                foreach (CardDefinition card in _cards)
                {
                    if (card == null) missing++;
                }

                return missing;
            }
        }

        public CardPool ToCardPool()
        {
            var cards = new List<Card>(_cards.Count);
            foreach (CardDefinition definition in _cards)
            {
                if (definition == null)
                {
                    continue;
                }

                cards.Add(definition.ToCard(_id));
            }

            return new CardPool(cards);
        }
    }
}
