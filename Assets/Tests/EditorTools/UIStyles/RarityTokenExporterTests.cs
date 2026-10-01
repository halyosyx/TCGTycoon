using System.IO;
using Game.Core.Content;
using Game.EditorTools;
using Game.EditorTools.CardGeneration;
using Game.EditorTools.UIStyles;
using Game.Unity.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Tests.UIStyles
{
    public sealed class RarityTokenExporterTests
    {
        private const string TempFolder = "Assets/_TempRarityTokenTest";
        private const string TempPath = TempFolder + "/RarityTokens.uss";

        private RarityPaletteDefinition _palette;

        [SetUp]
        public void SetUp()
        {
            _palette = ScriptableObject.CreateInstance<RarityPaletteDefinition>();
            _palette.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_palette);
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
        }

        // The guard the brief asks for: the committed stylesheet must match the project's palette.
        [Test]
        public void ProjectTokens_MatchRarityPaletteAsset()
        {
            var palette = AssetDatabase.LoadAssetAtPath<RarityPaletteDefinition>(CardAssetGenerator.DefaultPalettePath);
            Assert.That(palette, Is.Not.Null, $"No Rarity Palette at {CardAssetGenerator.DefaultPalettePath}.");

            Assert.That(
                GeneratedTextFiles.ReadNormalized(RarityTokenExporter.OutputPath),
                Is.EqualTo(RarityTokenExporter.BuildUss(palette)),
                $"{RarityTokenExporter.OutputPath} is out of date with the Rarity Palette. Run {RarityTokenExporter.MenuPath}.");
        }

        [Test]
        public void BuildUss_DefaultPalette_DeclaresOneTokenPerTierInLadderOrder()
        {
            string uss = RarityTokenExporter.BuildUss(_palette);

            int previousIndex = -1;
            foreach (RarityTier tier in RarityTiers.All)
            {
                string declaration = $"{RarityTokenExporter.TokenName(tier)}: #{ColorUtility.ToHtmlStringRGB(_palette.ColorOf(tier))};";
                int index = uss.IndexOf(declaration, System.StringComparison.Ordinal);
                Assert.That(index, Is.GreaterThan(previousIndex), declaration);
                previousIndex = index;
            }
        }

        [Test]
        public void TokenName_LowestAndHighestTier_AreOneAndFour()
        {
            Assert.That(RarityTokenExporter.TokenName(RarityTier.Common), Is.EqualTo("--color-tier-1"));
            Assert.That(RarityTokenExporter.TokenName(RarityTier.HoloFullArt), Is.EqualTo("--color-tier-3"));
            Assert.That(RarityTokenExporter.TokenName(RarityTier.SpecialFullArtHolo), Is.EqualTo("--color-tier-4"));
        }

        [Test]
        public void BuildUss_SamePalette_ReturnsIdenticalText()
        {
            Assert.That(RarityTokenExporter.BuildUss(_palette), Is.EqualTo(RarityTokenExporter.BuildUss(_palette)));
        }

        [Test]
        public void Export_SecondRunWithSamePalette_LeavesFileUnchanged()
        {
            AssetDatabase.CreateFolder("Assets", "_TempRarityTokenTest");

            bool isFirstWritten = RarityTokenExporter.Export(_palette, TempPath);
            System.DateTime firstWrite = File.GetLastWriteTimeUtc(TempPath);
            bool isSecondWritten = RarityTokenExporter.Export(_palette, TempPath);

            Assert.That(isFirstWritten, Is.True);
            Assert.That(isSecondWritten, Is.False);
            Assert.That(File.GetLastWriteTimeUtc(TempPath), Is.EqualTo(firstWrite));
            Assert.That(RarityTokenExporter.IsUpToDate(_palette, TempPath), Is.True);
        }

        [Test]
        public void IsUpToDate_PaletteColourChanged_ReturnsFalse()
        {
            AssetDatabase.CreateFolder("Assets", "_TempRarityTokenTest");
            RarityTokenExporter.Export(_palette, TempPath);

            var serialized = new SerializedObject(_palette);
            SerializedProperty firstTier = serialized.FindProperty(RarityPaletteDefinition.TiersField).GetArrayElementAtIndex(0);
            firstTier.FindPropertyRelative(RarityStyle.ColorField).colorValue = Color.black;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(RarityTokenExporter.IsUpToDate(_palette, TempPath), Is.False);
        }

        [Test]
        public void MissingTiers_DefaultPalette_IsEmpty()
        {
            Assert.That(RarityTokenExporter.MissingTiers(_palette), Is.Empty);
        }
    }
}
