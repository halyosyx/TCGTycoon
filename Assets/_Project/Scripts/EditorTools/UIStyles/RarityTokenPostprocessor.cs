using System;
using Game.EditorTools.CardGeneration;
using UnityEditor;

namespace Game.EditorTools.UIStyles
{
    /// <summary>
    /// Re-exports the rarity tokens whenever the Rarity Palette asset is imported: saved after an
    /// Inspector edit, changed by git, or created. The export itself waits until the import finishes.
    /// </summary>
    internal sealed class RarityTokenPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Array.IndexOf(importedAssets, CardAssetGenerator.DefaultPalettePath) >= 0)
            {
                RarityTokenExporter.ScheduleExport();
            }
        }
    }
}
