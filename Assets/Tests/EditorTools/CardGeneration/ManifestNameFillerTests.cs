using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using Game.EditorTools.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.EditorTools.Tests.CardGeneration
{
    public sealed class ManifestNameFillerTests
    {
        [Test]
        public void Fill_OnlyEmptyNames_HandEditedNamesStay()
        {
            Dictionary<string, IReadOnlyList<CardManifestEntry>> cards = Unnamed("TA", 3);
            cards[ManifestFixtures.InPrintSetId] = new List<CardManifestEntry>
            {
                new CardManifestEntry("TA_C_001", "Typed By Hand", RarityTier.Common),
                new CardManifestEntry("TA_C_002", string.Empty, RarityTier.Common),
            };

            Dictionary<string, List<CardManifestEntry>> filled = ManifestNameFiller.Fill(ManifestFixtures.Sets(), cards, out int filledCount);

            Assert.That(filled[ManifestFixtures.InPrintSetId][0].Name, Is.EqualTo("Typed By Hand"));
            Assert.That(filled[ManifestFixtures.InPrintSetId][1].Name, Is.Not.Empty);
            Assert.That(filledCount, Is.EqualTo(1 + 3));
        }

        [Test]
        public void Fill_NothingEmpty_FillsNothing()
        {
            ManifestNameFiller.Fill(ManifestFixtures.Sets(), ManifestFixtures.CardsBySet(), out int filledCount);

            Assert.That(filledCount, Is.EqualTo(0));
        }

        [Test]
        public void Fill_SameInputs_SameNames()
        {
            Dictionary<string, IReadOnlyList<CardManifestEntry>> cards = Unnamed("TA", 40);

            Dictionary<string, List<CardManifestEntry>> first = ManifestNameFiller.Fill(ManifestFixtures.Sets(), cards, out _);
            Dictionary<string, List<CardManifestEntry>> second = ManifestNameFiller.Fill(ManifestFixtures.Sets(), cards, out _);

            Assert.That(Names(second[ManifestFixtures.InPrintSetId]), Is.EqualTo(Names(first[ManifestFixtures.InPrintSetId])));
        }

        [Test]
        public void Fill_ManyCardsInTwoSets_NamesUniqueAcrossBothIgnoringCase()
        {
            var cards = new Dictionary<string, IReadOnlyList<CardManifestEntry>>
            {
                { ManifestFixtures.InPrintSetId, UnnamedCards("TA", 66) },
                { ManifestFixtures.OutOfPrintSetId, UnnamedCards("TB", 66) },
            };

            Dictionary<string, List<CardManifestEntry>> filled = ManifestNameFiller.Fill(ManifestFixtures.Sets(), cards, out _);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (List<CardManifestEntry> set in filled.Values)
            {
                foreach (CardManifestEntry card in set)
                {
                    Assert.That(seen.Add(card.Name), Is.True, $"Duplicate name {card.Name}");
                }
            }
        }

        [Test]
        public void SeedOf_DependsOnlyOnTheSetId()
        {
            Assert.That(ManifestNameFiller.SeedOf("SetA"), Is.EqualTo(ManifestNameFiller.SeedOf("SetA")));
            Assert.That(ManifestNameFiller.SeedOf("SetA"), Is.Not.EqualTo(ManifestNameFiller.SeedOf("SetB")));
            Assert.That(ManifestNameFiller.SeedOf("SetA"), Is.GreaterThanOrEqualTo(0));
        }

        private static Dictionary<string, IReadOnlyList<CardManifestEntry>> Unnamed(string prefix, int count)
        {
            return new Dictionary<string, IReadOnlyList<CardManifestEntry>>
            {
                { ManifestFixtures.InPrintSetId, UnnamedCards(prefix, count) },
                { ManifestFixtures.OutOfPrintSetId, UnnamedCards("TB", 3) },
            };
        }

        // Commons, the tier whose two-syllable names have the fewest combinations.
        private static List<CardManifestEntry> UnnamedCards(string prefix, int count)
        {
            var cards = new List<CardManifestEntry>(count);
            for (int index = 1; index <= count; index++)
            {
                cards.Add(new CardManifestEntry($"{prefix}_C_{index:000}", string.Empty, RarityTier.Common));
            }

            return cards;
        }

        private static List<string> Names(List<CardManifestEntry> cards)
        {
            var names = new List<string>();
            foreach (CardManifestEntry card in cards) names.Add(card.Name);
            return names;
        }
    }
}
