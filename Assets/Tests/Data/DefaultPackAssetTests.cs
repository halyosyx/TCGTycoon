using System.Collections.Generic;
using System.Linq;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Unity.Definitions;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    /// <summary>
    /// Checks the real pack assets, not code copies: the in-print default (500 cents, Champions) and
    /// the out-of-print pack (900 cents, Origins), both 7 slots on the same table. Fails if a pack stops
    /// validating, or if tuning pushes Rip EV outside the GDD's 80–95% band.
    /// </summary>
    [TestFixture(DefaultPackPath, 500L)]
    [TestFixture(OutOfPrintPackPath, 900L)]
    public sealed class DefaultPackAssetTests
    {
        private const string DefaultPackPath = "Assets/_Project/Data/Products/SetA_BoosterPack.asset";
        private const string OutOfPrintPackPath = "Assets/_Project/Data/Products/SetB_BoosterPack.asset";
        private const int Seed = 777;
        private const int ManyPacks = 100_000;
        private const int ExpectedSlotCount = 7;
        private const double MinimumRipEvShare = 0.80;
        private const double MaximumRipEvShare = 0.95;

        private readonly string _packPath;
        private readonly long _expectedPriceCents;
        private PackConfig _config;
        private CardPool _pool;

        public DefaultPackAssetTests(string packPath, long expectedPriceCents)
        {
            _packPath = packPath;
            _expectedPriceCents = expectedPriceCents;
        }

        [SetUp]
        public void LoadDefaultPack()
        {
            var pack = AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(_packPath);
            Assert.That(pack != null, $"Pack asset not found at {_packPath}.");
            Assert.That(pack.CardSet != null, "Default pack has no card set assigned.");
            Assert.That(pack.CardSet.MissingCardCount, Is.EqualTo(0), "The card set has empty entries.");

            _config = pack.ToPackConfig();
            _pool = pack.CardSet.ToCardPool();
        }

        [Test]
        public void Pack_PriceAndSlotCount_MatchTheDoc()
        {
            Assert.That(_config.PriceCents, Is.EqualTo(_expectedPriceCents));
            Assert.That(_config.Slots.Count, Is.EqualTo(ExpectedSlotCount));
        }

        [Test]
        public void DefaultPack_Converted_HasNoValidationErrors()
        {
            IReadOnlyList<ValidationIssue> issues = PackConfigValidator.Validate(_config, _pool);

            Assert.That(PackConfigValidator.HasErrors(issues), Is.False, string.Join("\n", issues.Select(issue => issue.ToString())));
        }

        [Test]
        public void DefaultPack_GeneratedPool_CoversEveryWeightedTier()
        {
            for (int slotIndex = 0; slotIndex < _config.Slots.Count; slotIndex++)
            {
                foreach (TierWeight entry in _config.Slots[slotIndex].Entries)
                {
                    if (entry.Weight > 0)
                    {
                        Assert.That(_pool.HasCards(entry.Tier), $"Slot {slotIndex + 1} can roll {entry.Tier}, but the pack's card set has no {entry.Tier} cards.");
                    }
                }
            }
        }

        [Test]
        public void DefaultPack_ExpectedValue_WithinEightyToNinetyFivePercentOfPrice()
        {
            double share = PackAnalysis.ExpectedValueCents(_config, _pool) / _config.PriceCents;

            Assert.That(share, Is.InRange(MinimumRipEvShare, MaximumRipEvShare));
        }

        [Test]
        public void DefaultPack_OneHundredThousandSimulatedPacks_RipEvWithinEightyToNinetyFivePercentOfPrice()
        {
            PackTally tally = PackSimulation.Run(new PackOpener(_config, _pool, new SeededRng(Seed)), ManyPacks);

            Assert.That(tally.AverageValueCents / _config.PriceCents, Is.InRange(MinimumRipEvShare, MaximumRipEvShare));
        }
    }
}
