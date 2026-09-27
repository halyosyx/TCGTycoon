using Game.Unity.Definitions;
using UnityEditor;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Inspector for the Rarity Palette. Pushes every colour edit into the generated materials straight
    /// away, so the palette stays the single definition of card colours.
    /// </summary>
    [CustomEditor(typeof(RarityPaletteDefinition))]
    public sealed class RarityPaletteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("These colours drive the tier materials, world cards and UI cards. Materials update as you edit.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                PaletteMaterialSync.SyncColors((RarityPaletteDefinition)target);
            }
        }
    }
}
