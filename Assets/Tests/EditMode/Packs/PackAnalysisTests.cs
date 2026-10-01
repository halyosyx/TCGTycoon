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
        public void ExpectedValueCents_StartingPack_Is418Point4Cents()
        {
            double expected = PackAnalysis.ExpectedValueCents(TestContent.StartingPack(), TestContent.SetAPool());

            Assert.That(expected, Is.EqualTo(TestContent.StartingExpectedValueCents).Within(Exact));
        }

        [Test]
        public void TierProbability_StartingPackLastSlot_IsWeightShare()
        {
            PackSlot lastSlot = TestContent.StartingPack().Slots[6];

            Assert.That(PackAnalysis.TierProbability(lastSlot, RarityTier.HoloFullArt), Is.EqualTo(0.985).Within(Exact));
            Assert.That(PackAnalysis.TierProbability(lastSlot, RarityTier.SpecialFullArtHolo), Is.EqualTo(0.015).Within(Exact));
            Assert.That(PackAnalysis.TierProbability(lastSlot, RarityTier.Common), Is.EqualTo(0d));
        }

        [Test]
        public void TierProbability_DuplicateTierEntries_AddsTheirWeights()
        {
            PackSlot slot = TestContent.Slot(
                TestContent.Weight(RarityTier.HoloFullArt, 1),
                TestContent.Weight(RarityTier.HoloFullArt, 1),
                TestContent.Weight(RarityTier.SpecialFullArtHolo, 2));

            Assert.That(PackAnalysis.TierProbability(slot, RarityTier.HoloFullArt), Is.EqualTo(0.5).Within(Exact));
        }

        [Test]
        public void ChanceOfAtLeastOne_Common_IsCertain()
        {
            Assert.That(PackAnalysis.ChanceOfAtLeastOne(TestContent.StartingPack(), RarityTier.Common), Is.EqualTo(1d).Within(Exact));
        }

        [Test]
        public void ChanceOfAtLeastOne_TwoIndependentSlots_CombinesAsOneMinusProductOfMisses()
        {
            // Each slot hits Holo Full Art or better half the time: 1 - 0.5 * 0.5 = 0.75.
            PackConfig config = TestContent.Pack(
                100,
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 1), TestContent.Weight(RarityTier.HoloFullArt, 1)),
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 1), TestContent.Weight(RarityTier.SpecialFullArtHolo, 1)));

            Assert.That(PackAnalysis.ChanceOfAtLeastOne(config, RarityTier.HoloFullArt), Is.EqualTo(0.75).Within(Exact));
        }

        [Test]
        public void ChanceOfAtLeastOne_HoloFullArtOrBetter_IsGuaranteedBySlotSeven()
        {
            // GDD v1.7: slot 7 rolls only Holo Full Art or Special, so every pack holds at least one hit.
            Assert.That(
                PackAnalysis.ChanceOfAtLeastOne(TestContent.StartingPack(), RarityTier.HoloFullArt),
                Is.EqualTo(1d).Within(Exact));
        }

        [Test]
        public void ChanceOfAtLeastOne_Special_ComesOnlyFromSlotSeven()
        {
            // 15 of 1000 in slot 7; no other slot can roll it.
            Assert.That(
                PackAnalysis.ChanceOfAtLeastOne(TestContent.StartingPack(), RarityTier.SpecialFullArtHolo),
                Is.EqualTo(0.015).Within(Exact));
        }
    }
}
