using System.Collections.Generic;
using System.Linq;
using Game.Core.Content;
using Game.Unity.Definitions;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    /// <summary>
    /// Checks the real generated card data against GDD v1.7 / Docs/Systems/CARD_DATA_AND_SETS.md: both
    /// sets present with 40/26/10/4 cards, ids unique across sets, out-of-print values exactly 1.8x the
    /// in-print ones, the default pack openable from either set's tiers, the tier price table's values,
    /// and no content asset still holding a tier removed with the seven-tier ladder.
    /// </summary>
    public sealed class CardDataAssetTests
    {
        private const string InPrintSetPath = "Assets/_Project/Data/Generated/SetA.asset";
        private const string OutOfPrintSetPath = "Assets/_Project/Data/Generated/SetB.asset";
        private const string DefaultPackPath = "Assets/_Project/Data/Products/SetA_BoosterPack.asset";
        private const string TierPricesPath = "Assets/_Project/Data/Balance/TierPrices.asset";
        private const string PalettePath = "Assets/_Project/Data/Visuals/RarityPalette.asset";
        private const string DataFolder = "Assets/_Project/Data";

        private static readonly Dictionary<RarityTier, int> s_cardsPerTier = new Dictionary<RarityTier, int>
        {
            { RarityTier.Common, 40 },
            { RarityTier.Uncommon, 26 },
            { RarityTier.HoloFullArt, 10 },
            { RarityTier.SpecialFullArtHolo, 4 },
        };

        [TestCase(InPrintSetPath, "Champions", "RC", SetLifecycle.InPrint)]
        [TestCase(OutOfPrintSetPath, "Origins", "MO", SetLifecycle.OutOfPrint)]
        public void Set_IdentityAndCardCountsPerTier_MatchTheDoc(string path, string shortName, string prefix, SetLifecycle lifecycle)
        {
            CardSetDefinition set = LoadSet(path);

            Assert.That(set.ShortName, Is.EqualTo(shortName));
            Assert.That(set.IdPrefix, Is.EqualTo(prefix));
            Assert.That(set.Lifecycle, Is.EqualTo(lifecycle));
            Assert.That(set.MissingCardCount, Is.EqualTo(0));
            Assert.That(set.Cards.Count, Is.EqualTo(80));
            foreach (KeyValuePair<RarityTier, int> expected in s_cardsPerTier)
            {
                Assert.That(set.Cards.Count(card => card.Tier == expected.Key), Is.EqualTo(expected.Value), expected.Key.ToString());
            }

            Assert.That(set.Cards.All(card => card.Id.StartsWith(prefix + "_")), Is.True, "Every id starts with the set's prefix.");
        }

        [Test]
        public void Sets_CardIds_AreUniqueAcrossBothSets()
        {
            List<string> ids = LoadSet(InPrintSetPath).Cards.Concat(LoadSet(OutOfPrintSetPath).Cards).Select(card => card.Id).ToList();

            Assert.That(ids, Is.Unique);
            Assert.That(ids.Count, Is.EqualTo(160));
        }

        [Test]
        public void OutOfPrintValues_AreInPrintValuesTimesOnePointEightExactly()
        {
            Dictionary<RarityTier, long> inPrint = ValueByTier(LoadSet(InPrintSetPath));
            Dictionary<RarityTier, long> outOfPrint = ValueByTier(LoadSet(OutOfPrintSetPath));

            foreach (RarityTier tier in RarityTiers.All)
            {
                Assert.That(outOfPrint[tier] * 10, Is.EqualTo(inPrint[tier] * 18), tier.ToString());
            }

            Assert.That(outOfPrint[RarityTier.SpecialFullArtHolo], Is.EqualTo(12600));
        }

        [TestCase(InPrintSetPath)]
        [TestCase(OutOfPrintSetPath)]
        public void DefaultPack_EveryWeightedTier_HasCardsInTheSet(string setPath)
        {
            PackConfig pack = AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(DefaultPackPath).ToPackConfig();
            CardPool pool = LoadSet(setPath).ToCardPool();

            foreach (PackSlot slot in pack.Slots)
            {
                foreach (TierWeight entry in slot.Entries.Where(entry => entry.Weight > 0))
                {
                    Assert.That(pool.HasCards(entry.Tier), $"The pack can roll {entry.Tier}, but {setPath} has none.");
                }
            }
        }

        [Test]
        public void TierPrices_MatchTheDocValuesAndVolatility()
        {
            var prices = AssetDatabase.LoadAssetAtPath<TierPriceTableDefinition>(TierPricesPath);
            Assert.That(prices != null, $"Tier price table not found at {TierPricesPath}.");

            Assert.That(prices.FindTierProblems(), Is.Empty);
            Assert.That(prices.PriceCentsOf(RarityTier.Common), Is.EqualTo(5));
            Assert.That(prices.PriceCentsOf(RarityTier.Uncommon), Is.EqualTo(15));
            Assert.That(prices.PriceCentsOf(RarityTier.HoloFullArt), Is.EqualTo(240));
            Assert.That(prices.PriceCentsOf(RarityTier.SpecialFullArtHolo), Is.EqualTo(7000));
            Assert.That(prices.VolatilityOf(RarityTier.Common), Is.EqualTo(VolatilityTier.Low));
            Assert.That(prices.VolatilityOf(RarityTier.Uncommon), Is.EqualTo(VolatilityTier.Low));
            Assert.That(prices.VolatilityOf(RarityTier.HoloFullArt), Is.EqualTo(VolatilityTier.Medium));
            Assert.That(prices.VolatilityOf(RarityTier.SpecialFullArtHolo), Is.EqualTo(VolatilityTier.High));
        }

        [Test]
        public void ContentAssets_NoneUsesARemovedTier()
        {
            var problems = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CardDefinition), new[] { DataFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                if (!RarityTiers.IsDefined(card.Tier))
                {
                    problems.Add($"{path}: {RarityTiers.Describe(card.Tier)}");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(PackConfigDefinition), new[] { DataFolder }))
            {
                problems.AddRange(AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(AssetDatabase.GUIDToAssetPath(guid)).FindTierProblems());
            }

            problems.AddRange(AssetDatabase.LoadAssetAtPath<TierPriceTableDefinition>(TierPricesPath).FindTierProblems());
            problems.AddRange(AssetDatabase.LoadAssetAtPath<RarityPaletteDefinition>(PalettePath).FindTierProblems());

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static CardSetDefinition LoadSet(string path)
        {
            var set = AssetDatabase.LoadAssetAtPath<CardSetDefinition>(path);
            Assert.That(set != null, $"Card set not found at {path}. Run TCG > Generate Card Data.");
            return set;
        }

        // Every card of a tier has the same value (tier base price x set scale).
        private static Dictionary<RarityTier, long> ValueByTier(CardSetDefinition set)
        {
            var values = new Dictionary<RarityTier, long>();
            foreach (CardDefinition card in set.Cards)
            {
                if (values.TryGetValue(card.Tier, out long value))
                {
                    Assert.That(card.ValueCents, Is.EqualTo(value), $"{card.Id} differs from other {card.Tier} cards in {set.Id}.");
                }
                else
                {
                    values.Add(card.Tier, card.ValueCents);
                }
            }

            return values;
        }
    }
}
