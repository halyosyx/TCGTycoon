using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using Game.EditorTools.Tests.TestUtilities;
using Game.Unity.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Tests.CardGeneration
{
    /// <summary>
    /// Runs the real generator on temporary manifests (outside Assets/) into a temporary asset folder,
    /// with an in-memory palette and project references off, so the project's own data, packs and
    /// scenes are never touched.
    /// </summary>
    public sealed class CardAssetGeneratorTests
    {
        private const string TempFolder = "Assets/_TempCardGeneratorTest";
        private const string OutputFolder = TempFolder + "/Generated";
        private const string PriceTablePath = TempFolder + "/TierPrices.asset";

        private RarityPaletteDefinition _palette;
        private string _manifestFolder;

        [SetUp]
        public void CreateInputs()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            _palette = ScriptableObject.CreateInstance<RarityPaletteDefinition>();
            _palette.ResetToDefaults();
            _manifestFolder = Path.Combine(Path.GetTempPath(), "TCGCardGeneratorTest");
            if (Directory.Exists(_manifestFolder)) Directory.Delete(_manifestFolder, true);
            Directory.CreateDirectory(_manifestFolder);
            WriteManifests(ManifestFixtures.Cards("TA", "A "), ManifestFixtures.Cards("TB", "B "));
        }

        [TearDown]
        public void DeleteOutputs()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            Object.DestroyImmediate(_palette);
            if (Directory.Exists(_manifestFolder)) Directory.Delete(_manifestFolder, true);
        }

        [Test]
        public void Generate_EmptyFolder_CreatesSetsCardsPricesAndVisuals()
        {
            CardGenerationReport report = Generate();

            Assert.That(report.Succeeded, report.ToString());
            CardSetDefinition inPrint = LoadSet(ManifestFixtures.InPrintSetId);
            CardSetDefinition outOfPrint = LoadSet(ManifestFixtures.OutOfPrintSetId);
            Assert.That(inPrint.Cards.Count, Is.EqualTo(5));
            Assert.That(outOfPrint.Cards.Count, Is.EqualTo(5));
            Assert.That(outOfPrint.ShortName, Is.EqualTo("Out Of Print"));
            Assert.That(outOfPrint.Lifecycle, Is.EqualTo(SetLifecycle.OutOfPrint));
            Assert.That(outOfPrint.Cards.Select(card => card.ValueCents), Is.EqualTo(new long[] { 9, 9, 27, 432, 12600 }));
            Assert.That(AssetDatabase.LoadAssetAtPath<CardDefinition>(OutputFolder + "/Cards/TestA/TA_C_001.asset") != null, "Card not written under Cards/<setId>/.");

            var prices = AssetDatabase.LoadAssetAtPath<TierPriceTableDefinition>(PriceTablePath);
            Assert.That(prices.FindTierProblems(), Is.Empty);
            Assert.That(prices.PriceCentsOf(RarityTier.SpecialFullArtHolo), Is.EqualTo(7000));
            Assert.That(prices.VolatilityOf(RarityTier.HoloFullArt), Is.EqualTo(VolatilityTier.Medium));

            Assert.That(AssetDatabase.FindAssets("t:Material", new[] { OutputFolder }).Length, Is.EqualTo(RarityTiers.Count + 2));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + "/Prefabs/BoosterPack.prefab") != null, "BoosterPack prefab missing");
            Assert.That(File.Exists(OutputFolder + "/UI/CardTemplate.uxml"), "UXML missing");
            Assert.That(report.Orphans, Is.Empty, report.ToString());
        }

        [Test]
        public void Generate_SecondRun_ChangesNothing()
        {
            Assert.That(Generate().Succeeded);
            Dictionary<string, string> before = HashFiles();

            CardGenerationReport second = Generate();

            Assert.That(second.Succeeded, second.ToString());
            Assert.That(second.Created, Is.EqualTo(0), second.ToString());
            Assert.That(second.Updated, Is.EqualTo(0), second.ToString());
            Assert.That(HashFiles(), Is.EqualTo(before), "A file changed on the second run.");
        }

        [Test]
        public void Generate_ManifestToAssetsToRows_RoundTripsExactly()
        {
            Assert.That(Generate().Succeeded);

            List<string> fromAssets = LoadSet(ManifestFixtures.InPrintSetId).Cards
                .Select(card => $"{card.Id}|{card.DisplayName}|{card.Tier}|{card.Flavour}|{card.ArtHint}").ToList();
            List<string> fromManifest = ManifestFixtures.Cards("TA", "A ")
                .Select(card => $"{card.Id}|{card.Name}|{card.Tier}|{card.Flavour}|{card.ArtHint}").ToList();

            Assert.That(fromAssets, Is.EqualTo(fromManifest));
        }

        [Test]
        public void Generate_RenamedRow_UpdatesTheSameAssetInPlace()
        {
            Assert.That(Generate().Succeeded);
            string path = OutputFolder + "/Cards/TestA/TA_HFA_01.asset";
            string guidBefore = AssetDatabase.AssetPathToGUID(path);

            List<CardManifestEntry> renamed = ManifestFixtures.Cards("TA", "A ");
            renamed[3] = renamed[3].WithName("Renamed By Hand");
            WriteManifests(renamed, ManifestFixtures.Cards("TB", "B "));
            CardGenerationReport report = Generate();

            Assert.That(report.Succeeded, report.ToString());
            Assert.That(report.Created, Is.EqualTo(0), report.ToString());
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guidBefore));
            Assert.That(AssetDatabase.LoadAssetAtPath<CardDefinition>(path).DisplayName, Is.EqualTo("Renamed By Hand"));
        }

        [Test]
        public void Generate_RemovedRow_IsReportedAndTheAssetIsKept()
        {
            Assert.That(Generate().Succeeded);
            List<CardManifestEntry> fewer = ManifestFixtures.Cards("TA", "A ");
            fewer.RemoveAt(1);
            WriteManifests(fewer, ManifestFixtures.Cards("TB", "B "));

            CardGenerationReport report = Generate();

            Assert.That(report.Succeeded, report.ToString());
            Assert.That(report.Orphans, Has.Some.Contains("TA_C_002"));
            Assert.That(AssetDatabase.LoadAssetAtPath<CardDefinition>(OutputFolder + "/Cards/TestA/TA_C_002.asset") != null, "The orphan must not be deleted.");
            Assert.That(LoadSet(ManifestFixtures.InPrintSetId).Cards.Count, Is.EqualTo(4));
        }

        [Test]
        public void Generate_InvalidManifest_WritesNothing()
        {
            List<CardManifestEntry> broken = ManifestFixtures.Cards("TA", "A ");
            broken.Add(new CardManifestEntry("TA_C_001", "Duplicate id", RarityTier.Common));
            WriteManifests(broken, ManifestFixtures.Cards("TB", "B "));

            CardGenerationReport report = Generate();

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Errors, Has.Some.Contains("TA_C_001 is already used"));
            Assert.That(AssetDatabase.IsValidFolder(TempFolder), Is.False, "Nothing should be written when validation fails.");
        }

        [Test]
        public void Generate_PaletteWithRemovedTier_FailsNamingIt()
        {
            var stale = ScriptableObject.CreateInstance<RarityPaletteDefinition>();
            var serialized = new SerializedObject(stale);
            SerializedProperty tiers = serialized.FindProperty(RarityPaletteDefinition.TiersField);
            tiers.arraySize = 1;
            tiers.GetArrayElementAtIndex(0).FindPropertyRelative(RarityStyle.TierField).intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CardGenerationReport report = CardAssetGenerator.Generate(Options(stale));
            Object.DestroyImmediate(stale);

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Errors, Has.Some.Contains("removed tier FullArt (4)"));
            Assert.That(AssetDatabase.IsValidFolder(TempFolder), Is.False);
        }

        private CardGenerationReport Generate() => CardAssetGenerator.Generate(Options(_palette));

        private CardGenerationOptions Options(RarityPaletteDefinition palette)
        {
            return new CardGenerationOptions
            {
                ManifestFolder = _manifestFolder,
                OutputFolder = OutputFolder,
                PriceTablePath = PriceTablePath,
                Palette = palette,
                IncludeProjectReferences = false,
            };
        }

        private void WriteManifests(List<CardManifestEntry> inPrint, List<CardManifestEntry> outOfPrint)
        {
            File.WriteAllText(Path.Combine(_manifestFolder, CardManifest.SetsFileName), CardManifest.WriteSets(ManifestFixtures.Sets()));
            File.WriteAllText(Path.Combine(_manifestFolder, CardManifest.TierPricesFileName), CardManifest.WriteTierPrices(ManifestFixtures.Prices()));
            File.WriteAllText(Path.Combine(_manifestFolder, "TestA.csv"), CardManifest.WriteCards(inPrint));
            File.WriteAllText(Path.Combine(_manifestFolder, "TestB.csv"), CardManifest.WriteCards(outOfPrint));
        }

        private static CardSetDefinition LoadSet(string setId) => AssetDatabase.LoadAssetAtPath<CardSetDefinition>($"{OutputFolder}/{setId}.asset");

        private static Dictionary<string, string> HashFiles()
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Directory.GetFiles(TempFolder, "*", SearchOption.AllDirectories)
                    .OrderBy(path => path)
                    .ToDictionary(path => path, path => System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))));
            }
        }
    }
}
