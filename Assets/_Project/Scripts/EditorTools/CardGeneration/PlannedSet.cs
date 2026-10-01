using System.Collections.Generic;
using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>A card set as the generator will write it: its Sets.csv row and its cards in manifest order.</summary>
    public sealed class PlannedSet
    {
        public PlannedSet(SetManifestEntry entry, IReadOnlyList<PlannedCard> cards)
        {
            Entry = entry;
            Cards = cards;
        }

        public SetManifestEntry Entry { get; }

        public IReadOnlyList<PlannedCard> Cards { get; }

        public string SetId => Entry.SetId;

        /// <summary>The set's cards as a Core pool, for validating packs against it before anything is written.</summary>
        public CardPool ToCardPool()
        {
            var cards = new List<Card>(Cards.Count);
            foreach (PlannedCard card in Cards)
            {
                cards.Add(card.ToCard());
            }

            return new CardPool(cards);
        }
    }
}
