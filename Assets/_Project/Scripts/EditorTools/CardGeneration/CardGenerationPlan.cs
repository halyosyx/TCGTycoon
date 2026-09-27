using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Packs;
using static System.FormattableString;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Turns generator settings into the exact cards to create (ids, names, tiers, prices). Pure, so the
    /// plan can be checked against a pack before any asset is written, and tested without the editor.
    /// </summary>
    public static class CardGenerationPlan
    {
        /// <summary>Re-roll limit for a duplicate name; the tables allow thousands of distinct names.</summary>
        public const int MaxNameAttempts = 1_000;

        // Index = RarityTier. These codes are part of every card id, which inventory and saves depend
        // on: add codes for new tiers, but never change an existing one.
        private static readonly string[] s_tierCodes = { "Common", "Uncommon", "Rare", "Holo", "FullArt", "AltArt", "SpecialArt" };

        public static string TierCode(RarityTier tier)
        {
            int index = (int)tier;
            if (index < 0 || index >= s_tierCodes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "This tier has no id code. Add one (and never change existing codes).");
            }

            return s_tierCodes[index];
        }

        /// <summary>Card id in the form <c>{setId}_{tierCode}_{number:00}</c>, e.g. <c>SetA_Holo_02</c>.</summary>
        public static string CardId(string setId, RarityTier tier, int number) => Invariant($"{setId}_{TierCode(tier)}_{number:00}");

        /// <summary>
        /// The cards to generate, lowest tier first. Names are unique within the set: a duplicate is
        /// re-rolled with the next attempt number, which stays deterministic.
        /// </summary>
        public static IReadOnlyList<Card> Build(CardGenerationSettings settings, Func<RarityTier, long> priceOf)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (priceOf == null) throw new ArgumentNullException(nameof(priceOf));
            ValidateSetId(settings.SetId);

            var cards = new List<Card>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RarityTier tier in RarityTiers.All)
            {
                int count = settings.CountOf(tier);
                if (count < 0)
                {
                    throw new ArgumentException($"The card count for {tier} can't be negative ({count}).", nameof(settings));
                }

                long priceCents = priceOf(tier);
                for (int number = 1; number <= count; number++)
                {
                    string name = UniqueName(settings.Seed, tier, number, usedNames);
                    cards.Add(new Card(CardId(settings.SetId, tier, number), name, settings.SetId, tier, priceCents));
                }
            }

            return cards.AsReadOnly();
        }

        /// <summary>
        /// Reasons <paramref name="pack"/> couldn't be opened with the planned cards, from the same
        /// validator the game uses. The key case: a tier the pack can roll that has no cards. Empty when
        /// the pack is openable.
        /// </summary>
        public static IReadOnlyList<string> FindPackProblems(IReadOnlyList<Card> plannedCards, PackConfig pack)
        {
            if (plannedCards == null) throw new ArgumentNullException(nameof(plannedCards));
            if (pack == null) throw new ArgumentNullException(nameof(pack));

            var problems = new List<string>();
            foreach (ValidationIssue issue in PackConfigValidator.Validate(pack, new CardPool(plannedCards)))
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    problems.Add($"{pack.DisplayName}: {issue}");
                }
            }

            return problems;
        }

        private static void ValidateSetId(string setId)
        {
            if (string.IsNullOrWhiteSpace(setId))
            {
                throw new ArgumentException("The set id can't be blank.", nameof(setId));
            }

            foreach (char character in setId)
            {
                if (char.IsWhiteSpace(character))
                {
                    throw new ArgumentException($"The set id '{setId}' can't contain spaces; it becomes part of every card id.", nameof(setId));
                }
            }
        }

        private static string UniqueName(int seed, RarityTier tier, int number, HashSet<string> usedNames)
        {
            for (int attempt = 0; attempt < MaxNameAttempts; attempt++)
            {
                string name = CardNameGenerator.Generate(seed, tier, number, attempt);
                if (usedNames.Add(name))
                {
                    return name;
                }
            }

            throw new InvalidOperationException($"Couldn't find a unique {tier} name in {MaxNameAttempts} attempts; the name tables are too small for this many cards.");
        }
    }
}
