using Game.EditorTools.UIStyles;
using Game.Unity.Definitions;
using UnityEditor;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Inspector for the Rarity Palette. Pushes every colour edit into the generated materials and the
    /// UI kit's tier tokens straight away, so the palette stays the single definition of tier colours.
    /// </summary>
    [CustomEditor(typeof(RarityPaletteDefinition))]
    public sealed class RarityPaletteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox($"These colours drive the tier materials, world cards, UI cards and the UI kit's tier tokens ({RarityTokenExporter.OutputPath}). Both update as you edit.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                var palette = (RarityPaletteDefinition)target;
                PaletteMaterialSync.SyncColors(palette);

                // Only the project's palette feeds the tokens; another palette asset is just data.
                if (AssetDatabase.GetAssetPath(palette) == CardAssetGenerator.DefaultPalettePath)
                {
                    RarityTokenExporter.ScheduleExport();
                }
            }
        }
    }
}
