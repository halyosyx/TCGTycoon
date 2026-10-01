using System.Collections.Generic;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using Game.EditorTools.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.EditorTools.Tests.CardGeneration
{
    public sealed class CardDataPlanTests
    {
        [Test]
        public void Build_ValidManifests_PlansEverySetWithScaledValues()
        {
            CardDataPlan plan = ManifestFixtures.ValidPlan();

            Assert.That(plan.Errors, Is.Empty);
            Assert.That(plan.Sets.Count, Is.EqualTo(2));
            Assert.That(Values(plan.FindSet(ManifestFixtures.InPrintSetId)), Is.EqualTo(new long[] { 5, 5, 15, 240, 7000 }));
            Assert.That(Values(plan.FindSet(ManifestFixtures.OutOfPrintSetId)), Is.EqualTo(new long[] { 9, 9, 27, 432, 12600 }));
        }

        [Test]
        public void Build_OutOfPrintValues_AreInPrintTimesOnePointEightExactly()
        {
            CardDataPlan plan = ManifestFixtures.ValidPlan();
            IReadOnlyList<PlannedCard> inPrint = plan.FindSet(ManifestFixtures.InPrintSetId).Cards;
            IReadOnlyList<PlannedCard> outOfPrint = plan.FindSet(ManifestFixtures.OutOfPrintSetId).Cards;

            for (int i = 0; i < inPrint.Count; i++)
            {
                Assert.That(outOfPrint[i].ValueCents * 10, Is.EqualTo(inPrint[i].ValueCents * 18), inPrint[i].Id);
            }
        }

        [TestCase(5, 150, false, 7)]
        [TestCase(5, 180, true, 9)]
        [TestCase(7000, 180, true, 12600)]
        public void TryScale_WholeCentsOnly(long basePrice, int scale, bool expectedExact, long expectedValue)
        {
            bool isExact = CardDataPlan.TryScale(basePrice, scale, out long value);

            Assert.That(isExact, Is.EqualTo(expectedExact));
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Build_ScaleThatLeavesFractionalCents_Fails()
        {
            List<SetManifestEntry> sets = ManifestFixtures.Sets();
            sets[1] = new SetManifestEntry(ManifestFixtures.OutOfPrintSetId, "B", "B", "TB", SetLifecycle.OutOfPrint, 150, "TestB.csv", 3);

            CardDataPlan plan = CardDataPlan.Build(sets, ManifestFixtures.CardsBySet(), ManifestFixtures.Prices());

            Assert.That(plan.IsValid, Is.False);
            Assert.That(plan.Errors, Has.Some.Contains("not a whole number of cents"));
        }

        [TestCase(RarityTier.Common, "C")]
        [TestCase(RarityTier.Uncommon, "U")]
        [TestCase(RarityTier.HoloFullArt, "HFA")]
        [TestCase(RarityTier.SpecialFullArtHolo, "SFAH")]
        public void TierCode_IsTheIdCode(RarityTier tier, string expected)
        {
            Assert.That(CardDataPlan.TierCode(tier), Is.EqualTo(expected));
        }

        [TestCase("RC_C_014", true, "RC", "C", 14)]
        [TestCase("MO_SFAH_02", true, "MO", "SFAH", 2)]
        [TestCase("rc_C_014", false, "", "", 0)]
        [TestCase("RC_C_", false, "", "", 0)]
        [TestCase("RC_C_1a", false, "", "", 0)]
        [TestCase("RC_C", false, "", "", 0)]
        public void TryParseCardId_SplitsPrefixCodeAndIndex(string id, bool expected, string prefix, string code, int index)
        {
            bool isParsed = CardDataPlan.TryParseCardId(id, out string actualPrefix, out string actualCode, out int actualIndex);

            Assert.That(isParsed, Is.EqualTo(expected));
            if (expected)
            {
                Assert.That(actualPrefix, Is.EqualTo(prefix));
                Assert.That(actualCode, Is.EqualTo(code));
                Assert.That(actualIndex, Is.EqualTo(index));
            }
        }

        [Test]
        public void Build_IdWithWrongPrefixOrTierCode_FailsWithFileAndLine()
        {
            Dictionary<string, IReadOnlyList<CardManifestEntry>> cards = ManifestFixtures.CardsBySet();
            cards[ManifestFixtures.InPrintSetId] = new List<CardManifestEntry>
            {
                new CardManifestEntry("TB_C_001", "Wrong prefix", RarityTier.Common, lineNumber: 2),
                new CardManifestEntry("TA_U_009", "Wrong code", RarityTier.Common, lineNumber: 3),
            };

            CardDataPlan plan = CardDataPlan.Build(ManifestFixtures.Sets(), cards, ManifestFixtures.Prices());

            Assert.That(plan.Errors, Has.Some.StartsWith("TestA.csv:2:").And.Contains("prefix TA"));
            Assert.That(plan.Errors, Has.Some.StartsWith("TestA.csv:3:").And.Contains("tier code U"));
        }

        [Test]
        public void Build_SameIdInTwoSets_Fails()
        {
            Dictionary<string, IReadOnlyList<CardManifestEntry>> cards = ManifestFixtures.CardsBySet();
            List<SetManifestEntry> sets = ManifestFixtures.Sets();
            sets[1] = new SetManifestEntry(ManifestFixtures.OutOfPrintSetId, "B", "B", "TA", SetLifecycle.OutOfPrint, 180, "TestB.csv", 3);
            cards[ManifestFixtures.OutOfPrintSetId] = ManifestFixtures.Cards("TA", "B ");

            CardDataPlan plan = CardDataPlan.Build(sets, cards, ManifestFixtures.Prices());

            Assert.That(plan.Errors, Has.Some.Contains("is already used at TestA.csv"));
            Assert.That(plan.Errors, Has.Some.Contains("idPrefix TA is used by another set"));
        }

        [Test]
        public void Build_EmptyName_FailsUnlessNamesAreOptional()
        {
            Dictionary<string, IReadOnlyList<CardManifestEntry>> cards = ManifestFixtures.CardsBySet();
            cards[ManifestFixtures.InPrintSetId] = new List<CardManifestEntry> { new CardManifestEntry("TA_C_001", string.Empty, RarityTier.Common, lineNumber: 2) };

            CardDataPlan strict = CardDataPlan.Build(ManifestFixtures.Sets(), cards, ManifestFixtures.Prices());
            CardDataPlan lenient = CardDataPlan.Build(ManifestFixtures.Sets(), cards, ManifestFixtures.Prices(), requireNames: false);

            Assert.That(strict.Errors, Has.Some.Contains("has no name"));
            Assert.That(lenient.Errors, Is.Empty);
        }

        [Test]
        public void Build_TierWithoutPrice_Fails()
        {
            List<TierPriceEntry> prices = ManifestFixtures.Prices();
            prices.RemoveAt(3);

            CardDataPlan plan = CardDataPlan.Build(ManifestFixtures.Sets(), ManifestFixtures.CardsBySet(), prices);

            Assert.That(plan.Errors, Has.Some.Contains("no price for SpecialFullArtHolo"));
        }

        [Test]
        public void Build_DuplicateSetId_Fails()
        {
            List<SetManifestEntry> sets = ManifestFixtures.Sets();
            sets[1] = new SetManifestEntry(ManifestFixtures.InPrintSetId, "B", "B", "TB", SetLifecycle.OutOfPrint, 180, "TestB.csv", 3);

            CardDataPlan plan = CardDataPlan.Build(sets, ManifestFixtures.CardsBySet(), ManifestFixtures.Prices());

            Assert.That(plan.Errors, Has.Some.Contains("setId TestA is listed more than once"));
        }

        private static List<long> Values(PlannedSet set)
        {
            var values = new List<long>();
            foreach (PlannedCard card in set.Cards)
            {
                values.Add(card.ValueCents);
            }

            return values;
        }
    }
}
