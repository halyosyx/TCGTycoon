using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using NUnit.Framework;

namespace Game.EditorTools.Tests.CardGeneration
{
    public sealed class CardGenerationPlanTests
    {
        private static readonly Func<RarityTier, long> s_priceOf = tier => ((int)tier + 1) * 100L;

        [Test]
        public void Build_DefaultSettings_CreatesConfiguredCountPerTier()
        {
            IReadOnlyList<Card> plan = CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf);

            int[] expected = { 12, 9, 5, 3, 2, 2, 1 };
            foreach (RarityTier tier in RarityTiers.All)
            {
                Assert.That(plan.Count(card => card.Tier == tier), Is.EqualTo(expected[(int)tier]), tier.ToString());
            }

            Assert.That(plan.Count, Is.EqualTo(34));
        }

        [Test]
        public void Build_DefaultSettings_IdsFollowSetTierIndexFormat()
        {
            List<string> ids = CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf).Select(card => card.Id).ToList();

            Assert.That(ids, Does.Contain("SetA_Common_01"));
            Assert.That(ids, Does.Contain("SetA_Common_12"));
            Assert.That(ids, Does.Contain("SetA_Holo_02"));
            Assert.That(ids, Does.Contain("SetA_SpecialArt_01"));
            Assert.That(ids, Is.Unique);
        }

        [Test]
        public void Build_DefaultSettings_NamesAreUniqueAndSetIdIsApplied()
        {
            IReadOnlyList<Card> plan = CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf);

            Assert.That(plan.Select(card => card.DisplayName), Is.Unique);
            Assert.That(plan.Select(card => card.SetId), Is.All.EqualTo("SetA"));
        }

        [Test]
        public void Build_SameSeed_ProducesIdenticalPlan()
        {
            List<string> first = Describe(CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf));
            List<string> second = Describe(CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Build_ChangingOneTierCount_KeepsOtherTiersNames()
        {
            var changed = new CardGenerationSettings();
            changed.CardsPerTier[(int)RarityTier.Common] = 20;

            IReadOnlyList<Card> original = CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf);
            IReadOnlyList<Card> modified = CardGenerationPlan.Build(changed, s_priceOf);

            foreach (Card card in original.Where(card => card.Tier != RarityTier.Common))
            {
                Card match = modified.Single(other => other.Id == card.Id);
                Assert.That(match.DisplayName, Is.EqualTo(card.DisplayName), card.Id);
            }
        }

        [Test]
        public void Build_EachCard_TakesItsTierPrice()
        {
            IReadOnlyList<Card> plan = CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf);

            foreach (Card card in plan)
            {
                Assert.That(card.ValueCents, Is.EqualTo(s_priceOf(card.Tier)), card.Id);
            }
        }

        [Test]
        public void TierCode_EveryTier_HasDistinctCode()
        {
            List<string> codes = RarityTiers.All.Select(CardGenerationPlan.TierCode).ToList();

            Assert.That(codes, Is.Unique);
            Assert.That(codes, Has.None.Empty);
        }

        [Test]
        public void Build_NegativeCount_Throws()
        {
            var settings = new CardGenerationSettings();
            settings.CardsPerTier[(int)RarityTier.Rare] = -1;

            Assert.Throws<ArgumentException>(() => CardGenerationPlan.Build(settings, s_priceOf));
        }

        [Test]
        public void Build_BlankSetId_Throws()
        {
            var settings = new CardGenerationSettings { SetId = " " };

            Assert.Throws<ArgumentException>(() => CardGenerationPlan.Build(settings, s_priceOf));
        }

        [Test]
        public void FindPackProblems_WeightedTierWithoutCards_ReportsTheTier()
        {
            var settings = new CardGenerationSettings();
            settings.CardsPerTier[(int)RarityTier.FullArt] = 0;
            IReadOnlyList<Card> plan = CardGenerationPlan.Build(settings, s_priceOf);

            IReadOnlyList<string> problems = CardGenerationPlan.FindPackProblems(plan, StartingPack());

            Assert.That(problems, Has.Some.Contains("FullArt"));
        }

        [Test]
        public void FindPackProblems_CompletePlan_ReportsNothing()
        {
            IReadOnlyList<Card> plan = CardGenerationPlan.Build(new CardGenerationSettings(), s_priceOf);

            Assert.That(CardGenerationPlan.FindPackProblems(plan, StartingPack()), Is.Empty);
        }

        private static List<string> Describe(IReadOnlyList<Card> plan)
        {
            return plan.Select(card => $"{card.Id}|{card.DisplayName}|{card.Tier}|{card.ValueCents}").ToList();
        }

        private static PackConfig StartingPack()
        {
            PackSlot Slot(params TierWeight[] entries) => new PackSlot(entries);
            return new PackConfig("pack", "Pack", 425, new[]
            {
                Slot(new TierWeight(RarityTier.Common, 100)),
                Slot(new TierWeight(RarityTier.Common, 100)),
                Slot(new TierWeight(RarityTier.Common, 100)),
                Slot(new TierWeight(RarityTier.Uncommon, 90), new TierWeight(RarityTier.Rare, 10)),
                Slot(
                    new TierWeight(RarityTier.Rare, 600),
                    new TierWeight(RarityTier.Holographic, 250),
                    new TierWeight(RarityTier.FullArt, 105),
                    new TierWeight(RarityTier.AlternateIllustration, 40),
                    new TierWeight(RarityTier.SpecialIllustration, 5)),
            });
        }
    }
}
