using Game.Core.Content;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Packs
{
    public sealed class PackAnalysisTests
    {
        private const double Exact = 1e-9;

        [Test]
        public void ExpectedValueCents_StartingPack_Is362Cents()
        {
            double expected = PackAnalysis.ExpectedValueCents(TestContent.StartingPack(), TestContent.SetAPool());

            Assert.That(expected, Is.EqualTo(TestContent.StartingExpectedValueCents).Within(Exact));
        }

        [Test]
        public void TierProbability_StartingPackLastSlot_IsWeightShare()
        {
            PackSlot lastSlot = TestContent.StartingPack().Slots[4];

            Assert.That(PackAnalysis.TierProbability(lastSlot, RarityTier.Rare), Is.EqualTo(0.6).Within(Exact));
            Assert.That(PackAnalysis.TierProbability(lastSlot, RarityTier.SpecialIllustration), Is.EqualTo(0.005).Within(Exact));
            Assert.That(PackAnalysis.TierProbability(lastSlot, RarityTier.Common), Is.EqualTo(0d));
        }

        [Test]
        public void TierProbability_DuplicateTierEntries_AddsTheirWeights()
        {
            PackSlot slot = TestContent.Slot(
                TestContent.Weight(RarityTier.Rare, 1),
                TestContent.Weight(RarityTier.Rare, 1),
                TestContent.Weight(RarityTier.FullArt, 2));

            Assert.That(PackAnalysis.TierProbability(slot, RarityTier.Rare), Is.EqualTo(0.5).Within(Exact));
        }

        [Test]
        public void ChanceOfAtLeastOne_Common_IsCertain()
        {
            Assert.That(PackAnalysis.ChanceOfAtLeastOne(TestContent.StartingPack(), RarityTier.Common), Is.EqualTo(1d).Within(Exact));
        }

        [Test]
        public void ChanceOfAtLeastOne_TwoIndependentSlots_CombinesAsOneMinusProductOfMisses()
        {
            // Each slot hits Holographic-or-better half the time: 1 - 0.5 * 0.5 = 0.75.
            PackConfig config = TestContent.Pack(
                100,
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 1), TestContent.Weight(RarityTier.Holographic, 1)),
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 1), TestContent.Weight(RarityTier.FullArt, 1)));

            Assert.That(PackAnalysis.ChanceOfAtLeastOne(config, RarityTier.Holographic), Is.EqualTo(0.75).Within(Exact));
        }

        [Test]
        public void ChanceOfAtLeastOne_OrBetter_CountsRarerTiersToo()
        {
            // Holographic or better in the starting pack comes only from the last slot: 400 of 1000.
            Assert.That(
                PackAnalysis.ChanceOfAtLeastOne(TestContent.StartingPack(), RarityTier.Holographic),
                Is.EqualTo(0.4).Within(Exact));
        }
    }
}
