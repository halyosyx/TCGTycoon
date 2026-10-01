using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>A card as the generator will write it: the manifest row plus its set and computed value.</summary>
    public sealed class PlannedCard
    {
        public PlannedCard(CardManifestEntry entry, string setId, long valueCents)
        {
            Entry = entry;
            SetId = setId;
            ValueCents = valueCents;
        }

        public CardManifestEntry Entry { get; }

        public string SetId { get; }

        /// <summary>Tier base price x the set's price scale / 100, in integer cents.</summary>
        public long ValueCents { get; }

        public string Id => Entry.Id;

        public RarityTier Tier => Entry.Tier;

        public Card ToCard() => new Card(Entry.Id, Entry.Name, SetId, Entry.Tier, ValueCents);
    }
}
