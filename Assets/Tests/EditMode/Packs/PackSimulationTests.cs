using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Packs
{
    public sealed class PackSimulationTests
    {
        private const int Seed = 424242;
        private const int ManyPacks = 100_000;
        private const double MinimumRipEvShare = 0.80;
        private const double MaximumRipEvShare = 0.95;

        [Test]
        public void Run_OneHundredThousandStartingPacks_RipEvWithinEightyToNinetyFivePercentOfPrice()
        {
            PackTally tally = SimulateStartingPack();

            double share = tally.AverageValueCents / TestContent.StartingPackPriceCents;

            Assert.That(share, Is.InRange(MinimumRipEvShare, MaximumRipEvShare));
        }

        [Test]
        public void Run_OneHundredThousandStartingPacks_AverageValueWithinFiveStandardErrorsOfExpected()
        {
            PackTally tally = SimulateStartingPack();

            Assert.That(
                tally.AverageValueCents,
                Is.EqualTo(TestContent.StartingExpectedValueCents).Within(Tolerance.ForMean(tally.StandardErrorCents)));
        }

        [Test]
        public void Run_OneHundredThousandStartingPacks_ChanceOfAtLeastOneMatchesAnalysis()
        {
            PackConfig config = TestContent.StartingPack();
            PackTally tally = SimulateStartingPack();

            foreach (RarityTier tier in RarityTiers.All)
            {
                double expected = PackAnalysis.ChanceOfAtLeastOne(config, tier);
                Assert.That(
                    tally.ObservedChanceOfAtLeastOne(tier),
                    Is.EqualTo(expected).Within(Tolerance.ForRate(expected, ManyPacks)),
                    tier.ToString());
            }
        }

        [Test]
        public void Run_GivenCount_TalliesThatManyPacks()
        {
            PackTally tally = PackSimulation.Run(CreateStartingOpener(), 250);

            Assert.That(tally.PackCount, Is.EqualTo(250));
            Assert.That(tally.SlotCount, Is.EqualTo(5));
        }

        private static PackTally SimulateStartingPack() => PackSimulation.Run(CreateStartingOpener(), ManyPacks);

        private static PackOpener CreateStartingOpener()
        {
            return new PackOpener(TestContent.StartingPack(), TestContent.SetAPool(), new SeededRng(Seed));
        }
    }
}
