using System.Collections.Generic;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>Authoring asset for a card set. Converted into a Core <see cref="CardPool"/> at load.</summary>
    [CreateAssetMenu(menuName = "TCG/Card Set", fileName = "NewCardSet")]
    public sealed class CardSetDefinition : ScriptableObject
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private List<CardDefinition> _cards = new List<CardDefinition>();

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
