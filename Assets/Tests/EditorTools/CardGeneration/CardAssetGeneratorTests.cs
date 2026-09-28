using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using Game.Unity.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Tests.CardGeneration
{
    /// <summary>
    /// Runs the real generator into a temporary folder with an in-memory palette and price table, so
    /// the project's own palette, prices and pack are never touched.
    /// </summary>
    public sealed class CardAssetGeneratorTests
    {
        private const string TempFolder = "Assets/_TempCardGeneratorTest";

        private RarityPaletteDefinition _palette;
        private TierPriceTableDefinition _prices;

        [SetUp]
        public void CreateInputs()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            _palette = ScriptableObject.CreateInstance<RarityPaletteDefinition>();
            _palette.ResetToDefaults();
            _prices = ScriptableObject.CreateInstance<TierPriceTableDefinition>();
            _prices.ResetToDefaults();
        }

        [TearDown]
        public void DeleteOutputs()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            Object.DestroyImmediate(_palette);
            Object.DestroyImmediate(_prices);
        }

        [Test]
        public void Generate_EmptyFolder_CreatesPoolMaterialsPrefabsAndTemplate()
        {
            CardGenerationReport report = Generate(Settings());

            Assert.That(report.Succeeded, report.ToString());
            CardSetDefinition set = LoadSet();
            Assert.That(set.Cards.Count, Is.EqualTo(34));
            Assert.That(set.MissingCardCount, Is.EqualTo(0));
            Assert.That(AssetDatabase.FindAssets("t:Material", new[] { TempFolder }).Length, Is.EqualTo(RarityTiers.Count + 2));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(TempFolder + "/Prefabs/BoosterPack.prefab") != null, "BoosterPack prefab missing");
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(TempFolder + "/Prefabs/WorldCard.prefab") != null, "WorldCard prefab missing");
            Assert.That(File.Exists(TempFolder + "/UI/CardTemplate.uxml"), "UXML missing");
            Assert.That(File.Exists(TempFolder + "/UI/CardTemplate.uss"), "USS missing");
            Assert.That(File.Exists(TempFolder + "/README.md"), "README missing");
            foreach (RarityTier tier in RarityTiers.All)
            {
                Assert.That(_palette.MaterialOf(tier) != null, $"Palette has no material linked for {tier}.");
            }
        }

        [Test]
        public void Generate_SecondRun_ChangesNothing()
        {
            Assert.That(Generate(Settings()).Succeeded);
            Dictionary<string, string> before = HashFiles();

            CardGenerationReport second = Generate(Settings());

            Assert.That(second.Succeeded, second.ToString());
            Assert.That(second.Created, Is.EqualTo(0), second.ToString());
            Assert.That(second.Updated, Is.EqualTo(0), second.ToString());
            Assert.That(second.Deleted, Is.EqualTo(0), second.ToString());
            Assert.That(HashFiles(), Is.EqualTo(before), "A file changed on the second run.");
        }

        [Test]
        public void Generate_AfterDeletingFolder_RestoresSameCards()
        {
            Assert.That(Generate(Settings()).Succeeded);
            List<string> before = DescribeCards();

            AssetDatabase.DeleteAsset(TempFolder);
            Assert.That(Generate(Settings()).Succeeded);

            Assert.That(DescribeCards(), Is.EqualTo(before));
        }

        [Test]
        public void Generate_ReducedCountWithOverwrite_DeletesStaleCards()
        {
            Assert.That(Generate(Settings()).Succeeded);
            CardGenerationSettings fewer = Settings();
            fewer.CardsPerTier[(int)RarityTier.Common] = 10;

            CardGenerationReport report = Generate(fewer);

            Assert.That(report.Deleted, Is.EqualTo(2), report.ToString());
            Assert.That(LoadSet().Cards.Count, Is.EqualTo(32));
        }

        [Test]
        public void Generate_PackTierWithoutCards_FailsBeforeWritingAnything()
        {
            CardGenerationSettings settings = Settings();
            settings.CardsPerTier[(int)RarityTier.FullArt] = 0;
            PackConfigDefinition pack = CreatePackRollingFullArt();

            CardGenerationReport report = CardAssetGenerator.Generate(settings, _palette, _prices, pack);
            Object.DestroyImmediate(pack);

            Assert.That(report.Succeeded, Is.False);
            Assert.That(report.Errors, Has.Some.Contains("FullArt"));
            Assert.That(AssetDatabase.IsValidFolder(TempFolder), Is.False, "Nothing should be written when validation fails.");
        }

        private CardGenerationReport Generate(CardGenerationSettings settings)
        {
            return CardAssetGenerator.Generate(settings, _palette, _prices, packToLink: null);
        }

        private static CardGenerationSettings Settings() => new CardGenerationSettings { OutputFolder = TempFolder };

        private static CardSetDefinition LoadSet() => AssetDatabase.LoadAssetAtPath<CardSetDefinition>(TempFolder + "/SetA.asset");

        private static List<string> DescribeCards()
        {
            return LoadSet().Cards.Select(card => $"{card.Id}|{card.DisplayName}|{card.Tier}|{card.ValueCents}").ToList();
        }

        private static Dictionary<string, string> HashFiles()
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Directory.GetFiles(TempFolder, "*", SearchOption.AllDirectories)
                    .OrderBy(path => path)
                    .ToDictionary(path => path, path => System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))));
            }
        }

        private static PackConfigDefinition CreatePackRollingFullArt()
        {
            var pack = ScriptableObject.CreateInstance<PackConfigDefinition>();
            var serialized = new SerializedObject(pack);
            SerializedProperty slots = serialized.FindProperty(PackConfigDefinition.SlotsField);
            slots.arraySize = 1;
            SerializedProperty entries = slots.GetArrayElementAtIndex(0).FindPropertyRelative(PackSlotData.EntriesField);
            entries.arraySize = 1;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative(TierWeightData.TierField).intValue = (int)RarityTier.FullArt;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative(TierWeightData.WeightField).intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return pack;
        }
    }
}
