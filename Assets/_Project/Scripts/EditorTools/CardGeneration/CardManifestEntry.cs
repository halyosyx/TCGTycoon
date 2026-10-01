using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>One row of a set's card manifest (e.g. <c>Champions.csv</c>): a card as authored.</summary>
    public sealed class CardManifestEntry
    {
        public CardManifestEntry(string id, string name, RarityTier tier, string flavour = "", string artHint = "", int lineNumber = 0)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Tier = tier;
            Flavour = flavour ?? string.Empty;
            ArtHint = artHint ?? string.Empty;
            LineNumber = lineNumber;
        }

        /// <summary>Authored, stable id: <c>&lt;prefix&gt;_&lt;tierCode&gt;_&lt;index&gt;</c>, e.g. RC_C_014.</summary>
        public string Id { get; }

        /// <summary>Display name. Empty until filled (TCG > Generate Card Data > Fill missing names) or typed in.</summary>
        public string Name { get; }

        public RarityTier Tier { get; }

        public string Flavour { get; }

        public string ArtHint { get; }

        /// <summary>Line in the manifest, for error messages; 0 when built in code.</summary>
        public int LineNumber { get; }

        public CardManifestEntry WithName(string name) => new CardManifestEntry(Id, name, Tier, Flavour, ArtHint, LineNumber);
    }
}
