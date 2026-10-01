using Game.Core.Content;
using Game.Unity.Definitions;
using Game.Unity.UI.Controls;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    /// <summary>
    /// Checks the real Rarity Palette asset against the UI kit's tier table, so a tier renamed in one
    /// place can't show two different names in the world and in the UI.
    /// </summary>
    public sealed class RarityPaletteAssetTests
    {
        private const string PalettePath = "Assets/_Project/Data/Visuals/RarityPalette.asset";

        [Test]
        public void RarityPalette_TierNames_MatchUiKitTierTable()
        {
            var palette = AssetDatabase.LoadAssetAtPath<RarityPaletteDefinition>(PalettePath);
            Assert.That(palette != null, $"Rarity Palette not found at {PalettePath}.");

            Assert.That(palette.FindTierProblems(), Is.Empty);
            foreach (RarityTier tier in RarityTiers.All)
            {
                int uiTier = TierDisplay.FromRarity(tier);
                Assert.That(palette.DisplayNameOf(tier), Is.EqualTo(TierDisplay.NameOf(uiTier)), tier.ToString());
                Assert.That(palette.ShortNameOf(tier), Is.EqualTo(TierDisplay.ShortNameOf(uiTier)), tier.ToString());
            }
        }
    }
}
