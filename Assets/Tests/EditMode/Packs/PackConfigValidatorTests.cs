using System.Collections.Generic;
using System.Linq;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Packs
{
    public sealed class PackConfigValidatorTests
    {
        [Test]
        public void Validate_StartingPack_ReportsNoIssues()
        {
            IReadOnlyList<ValidationIssue> issues = PackConfigValidator.Validate(TestContent.StartingPack(), TestContent.SetAPool());

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void Validate_NoSlots_ReportsError()
        {
            AssertSingleIssue(TestContent.Pack(100), ValidationSeverity.Error, ValidationIssue.PackLevel);
        }

        [Test]
        public void Validate_EmptySlot_ReportsError()
        {
            AssertSingleIssue(TestContent.Pack(100, TestContent.Slot()), ValidationSeverity.Error, slotIndex: 0);
        }

        [Test]
        public void Validate_AllZeroWeights_ReportsError()
        {
            PackConfig config = TestContent.Pack(100, TestContent.Slot(TestContent.Weight(RarityTier.Common, 0), TestContent.Weight(RarityTier.Rare, 0)));

            AssertSingleIssue(config, ValidationSeverity.Error, slotIndex: 0);
        }

        [Test]
        public void Validate_NegativeWeight_ReportsError()
        {
            PackConfig config = TestContent.Pack(
                100,
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 1)),
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 5), TestContent.Weight(RarityTier.Rare, -1)));

            AssertSingleIssue(config, ValidationSeverity.Error, slotIndex: 1);
        }

        [Test]
        public void Validate_TierWithoutCardsInPool_ReportsError()
        {
            var pool = new CardPool(new[] { TestContent.CreateCard("C1", RarityTier.Common) });
            PackConfig config = TestContent.Pack(100, TestContent.Slot(TestContent.Weight(RarityTier.Common, 9), TestContent.Weight(RarityTier.Rare, 1)));

            AssertSingleIssue(config, pool, ValidationSeverity.Error, slotIndex: 0);
        }

        [Test]
        public void Validate_ZeroWeightTierWithoutCards_ReportsNothing()
        {
            var pool = new CardPool(new[] { TestContent.CreateCard("C1", RarityTier.Common) });
            PackConfig config = TestContent.Pack(100, TestContent.Slot(TestContent.Weight(RarityTier.Common, 9), TestContent.Weight(RarityTier.Rare, 0)));

            Assert.That(PackConfigValidator.Validate(config, pool), Is.Empty);
        }

        [Test]
        public void Validate_UnknownTierValue_ReportsError()
        {
            PackConfig config = TestContent.Pack(100, TestContent.Slot(TestContent.Weight(RarityTier.Common, 1), TestContent.Weight((RarityTier)99, 1)));

            AssertSingleIssue(config, ValidationSeverity.Error, slotIndex: 0);
        }

        [Test]
        public void Validate_DuplicateTierInSlot_ReportsWarning()
        {
            PackConfig config = TestContent.Pack(100, TestContent.Slot(TestContent.Weight(RarityTier.Common, 1), TestContent.Weight(RarityTier.Common, 2)));

            AssertSingleIssue(config, ValidationSeverity.Warning, slotIndex: 0);
        }

        [Test]
        public void Validate_NonPositivePrice_ReportsWarning()
        {
            PackConfig config = TestContent.Pack(0, TestContent.Slot(TestContent.Weight(RarityTier.Common, 1)));

            AssertSingleIssue(config, ValidationSeverity.Warning, ValidationIssue.PackLevel);
        }

        [Test]
        public void Validate_DuplicateCardIds_ReportsWarning()
        {
            var pool = new CardPool(new[] { TestContent.CreateCard("Same", RarityTier.Common), TestContent.CreateCard("Same", RarityTier.Common) });
            PackConfig config = TestContent.Pack(100, TestContent.Slot(TestContent.Weight(RarityTier.Common, 1)));

            AssertSingleIssue(config, pool, ValidationSeverity.Warning, ValidationIssue.PackLevel);
        }

        [Test]
        public void HasErrors_OnlyWarnings_ReturnsFalse()
        {
            var issues = new[] { new ValidationIssue(ValidationSeverity.Warning, 0, "just a warning") };

            Assert.That(PackConfigValidator.HasErrors(issues), Is.False);
        }

        private static void AssertSingleIssue(PackConfig config, ValidationSeverity severity, int slotIndex)
        {
            AssertSingleIssue(config, TestContent.SetAPool(), severity, slotIndex);
        }

        private static void AssertSingleIssue(PackConfig config, CardPool pool, ValidationSeverity severity, int slotIndex)
        {
            IReadOnlyList<ValidationIssue> issues = PackConfigValidator.Validate(config, pool);

            Assert.That(issues.Count, Is.EqualTo(1), "Issues: " + string.Join(" | ", issues.Select(issue => issue.ToString())));
            Assert.That(issues[0].Severity, Is.EqualTo(severity));
            Assert.That(issues[0].SlotIndex, Is.EqualTo(slotIndex));
            Assert.That(PackConfigValidator.HasErrors(issues), Is.EqualTo(severity == ValidationSeverity.Error));
        }
    }
}
