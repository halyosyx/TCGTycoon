using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>One row of <c>Sets.csv</c>: a card set's identity, lifecycle, price scale and the file listing its cards.</summary>
    public sealed class SetManifestEntry
    {
        public SetManifestEntry(
            string setId,
            string displayName,
            string shortName,
            string idPrefix,
            SetLifecycle lifecycle,
            int priceScalePercent,
            string cardManifest,
            int lineNumber = 0)
        {
            SetId = setId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ShortName = shortName ?? string.Empty;
            IdPrefix = idPrefix ?? string.Empty;
            Lifecycle = lifecycle;
            PriceScalePercent = priceScalePercent;
            CardManifest = cardManifest ?? string.Empty;
            LineNumber = lineNumber;
        }

        /// <summary>Stable set id (Card.SetId), e.g. "SetA". Never change it once cards can be owned.</summary>
        public string SetId { get; }

        public string DisplayName { get; }

        /// <summary>Binder tab label, e.g. "Champions".</summary>
        public string ShortName { get; }

        /// <summary>Prefix of every card id in the set, e.g. "RC".</summary>
        public string IdPrefix { get; }

        public SetLifecycle Lifecycle { get; }

        /// <summary>Card value = tier base price x this / 100, exactly (100 in print, 180 out of print).</summary>
        public int PriceScalePercent { get; }

        /// <summary>File name of the set's card manifest, next to Sets.csv, e.g. "Champions.csv".</summary>
        public string CardManifest { get; }

        /// <summary>Line in Sets.csv, for error messages; 0 when built in code.</summary>
        public int LineNumber { get; }
    }
}
