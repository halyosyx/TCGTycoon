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
    /// Checks the real default pack asset, not a code copy. Fails if the asset stops validating, or
    /// if tuning pushes Rip EV outside the GDD's 80–95% band.
    /// </summary>
    public sealed class DefaultPackAssetTests
    {
        private const string DefaultPackPath = "Assets/_Project/Data/Products/SetA_BoosterPack.asset";
        private const int Seed = 777;
        private const int ManyPacks = 100_000;
        private const double MinimumRipEvShare = 0.80;
        private const double MaximumRipEvShare = 0.95;

        private PackConfig _config;
        private CardPool _pool;

        [SetUp]
        public void LoadDefaultPack()
        {
            var pack = AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(DefaultPackPath);
            Assert.That(pack != null, $"Default pack asset not found at {DefaultPackPath}.");
            Assert.That(pack.CardSet != null, "Default pack has no card set assigned.");
            Assert.That(pack.CardSet.MissingCardCount, Is.EqualTo(0), "The card set has empty entries.");

            _config = pack.ToPackConfig();
            _pool = pack.CardSet.ToCardPool();
        }

        [Test]
        public void DefaultPack_Converted_HasNoValidationErrors()
        {
            IReadOnlyList<ValidationIssue> issues = PackConfigValidator.Validate(_config, _pool);

            Assert.That(PackConfigValidator.HasErrors(issues), Is.False, string.Join("\n", issues.Select(issue => issue.ToString())));
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
