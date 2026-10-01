using System;
using System.Collections.Generic;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Fills empty <c>name</c> cells in card manifests with <see cref="CardNameGenerator"/> names, so a
    /// new row needs only an id and a tier. Names already in a manifest are never touched, which is what
    /// lets hand edits stick: the CSV is the source of truth, not the generator. A generated name depends
    /// on the set id, the tier and the id's index, and is unique (ignoring case) across every set.
    /// </summary>
    public static class ManifestNameFiller
    {
        /// <summary>Re-roll limit for a duplicate name; the tables allow far more distinct names than a set needs.</summary>
        public const int MaxNameAttempts = 1_000;

        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        /// <summary>
        /// Every set's cards with empty names filled, in the same order. <paramref name="filledCount"/>
        /// says how many names were added (0 means nothing to write back).
        /// </summary>
        /// <exception cref="InvalidOperationException">No unique name was found within <see cref="MaxNameAttempts"/>.</exception>
        public static Dictionary<string, List<CardManifestEntry>> Fill(
            IReadOnlyList<SetManifestEntry> sets,
            IReadOnlyDictionary<string, IReadOnlyList<CardManifestEntry>> cardsBySetId,
            out int filledCount)
        {
            if (sets == null) throw new ArgumentNullException(nameof(sets));
            if (cardsBySetId == null) throw new ArgumentNullException(nameof(cardsBySetId));

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (IReadOnlyList<CardManifestEntry> cards in cardsBySetId.Values)
            {
                foreach (CardManifestEntry card in cards)
                {
                    if (card.Name.Length > 0) usedNames.Add(card.Name);
                }
            }

            filledCount = 0;
            var result = new Dictionary<string, List<CardManifestEntry>>(StringComparer.Ordinal);
            foreach (SetManifestEntry set in sets)
            {
                if (!cardsBySetId.TryGetValue(set.SetId, out IReadOnlyList<CardManifestEntry> cards) || result.ContainsKey(set.SetId))
                {
                    continue;
                }

                int seed = SeedOf(set.SetId);
                var filled = new List<CardManifestEntry>(cards.Count);
                for (int row = 0; row < cards.Count; row++)
                {
                    CardManifestEntry card = cards[row];
                    if (card.Name.Length > 0)
                    {
                        filled.Add(card);
                        continue;
                    }

                    int index = CardDataPlan.TryParseCardId(card.Id, out _, out _, out int idIndex) ? idIndex : row + 1;
                    filled.Add(card.WithName(UniqueName(seed, card, index, usedNames)));
                    filledCount++;
                }

                result.Add(set.SetId, filled);
            }

            return result;
        }

        /// <summary>A stable seed for a set id (FNV-1a over its characters), so names don't depend on file order.</summary>
        public static int SeedOf(string setId)
        {
            uint hash = FnvOffsetBasis;
            foreach (char character in setId ?? string.Empty)
            {
                hash ^= character;
                hash *= FnvPrime;
            }

            return (int)(hash & int.MaxValue);
        }

        private static string UniqueName(int seed, CardManifestEntry card, int index, HashSet<string> usedNames)
        {
            for (int attempt = 0; attempt < MaxNameAttempts; attempt++)
            {
                string name = CardNameGenerator.Generate(seed, card.Tier, index, attempt);
                if (usedNames.Add(name))
                {
                    return name;
                }
            }

            throw new InvalidOperationException($"Couldn't find a unique name for {card.Id} in {MaxNameAttempts} attempts.");
        }
    }
}
