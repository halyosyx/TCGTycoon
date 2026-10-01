using System;
using System.Collections.Generic;
using Game.Unity.Definitions;
using Game.Unity.UI.PackOpening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// After a generator run, points the project's known references back at the generated assets and
    /// reports any it can't fix. The generator upserts in place, so GUIDs normally survive and nothing
    /// changes; this is the safety net for a generated folder that was deleted or a reference that was
    /// cleared. Known references: each pack configuration's card set (matched by set id), and in open
    /// scenes the Pack Opening Screen's card template. A missing BoosterPack prefab instance can't be
    /// re-linked automatically; it is reported with its scene path.
    /// </summary>
    public static class GeneratedReferenceLinker
    {
        /// <summary>Scenes holding known references; one that isn't open is reported as not checked.</summary>
        public static readonly IReadOnlyList<string> KnownScenePaths = Array.AsReadOnly(new[] { "Assets/_Project/Scenes/Prototype_Home.unity" });

        public static void Relink(IReadOnlyDictionary<string, CardSetDefinition> setsById, string cardTemplatePath, CardGenerationReport report)
        {
            if (setsById == null) throw new ArgumentNullException(nameof(setsById));
            if (report == null) throw new ArgumentNullException(nameof(report));

            RelinkPacks(setsById, report);
            RelinkScenes(cardTemplatePath, report);
        }

        private static void RelinkPacks(IReadOnlyDictionary<string, CardSetDefinition> setsById, CardGenerationReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(PackConfigDefinition)))
            {
                var pack = AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (pack == null)
                {
                    continue;
                }

                if (pack.CardSet == null)
                {
                    report.AddNote($"Couldn't re-link pack '{pack.name}': it has no card set, so its set is unknown. Assign one in the Inspector.");
                    continue;
                }

                if (!setsById.TryGetValue(pack.CardSet.Id ?? string.Empty, out CardSetDefinition generated) || generated == pack.CardSet)
                {
                    continue;
                }

                var serialized = new SerializedObject(pack);
                serialized.FindProperty(PackConfigDefinition.CardSetField).objectReferenceValue = generated;
                if (serialized.ApplyModifiedPropertiesWithoutUndo())
                {
                    report.AddNote($"Re-linked pack '{pack.name}' to the generated set '{generated.name}'.");
                }
            }
        }

        private static void RelinkScenes(string cardTemplatePath, CardGenerationReport report)
        {
            var template = string.IsNullOrEmpty(cardTemplatePath) ? null : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(cardTemplatePath);
            var openScenePaths = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded)
                {
                    continue;
                }

                openScenePaths.Add(scene.path);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    RelinkCardTemplates(root, template, scene, report);
                    ReportMissingPrefabs(root.transform, scene, report);
                }
            }

            foreach (string path in KnownScenePaths)
            {
                if (!openScenePaths.Contains(path))
                {
                    report.AddNote($"Scene {path} isn't open, so its references weren't checked. Open it and generate again to check them.");
                }
            }
        }

        private static void RelinkCardTemplates(GameObject root, VisualTreeAsset template, Scene scene, CardGenerationReport report)
        {
            foreach (PackOpeningScreen screen in root.GetComponentsInChildren<PackOpeningScreen>(true))
            {
                var serialized = new SerializedObject(screen);
                SerializedProperty property = serialized.FindProperty(PackOpeningScreen.CardTemplateField);
                if (property == null || property.objectReferenceValue != null)
                {
                    continue;
                }

                if (template == null)
                {
                    report.AddNote($"Couldn't re-link the card template on '{screen.name}' in {scene.path}: the generated template is missing.");
                    continue;
                }

                property.objectReferenceValue = template;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                report.AddNote($"Re-linked the card template on '{screen.name}' in {scene.path}. Save the scene to keep it.");
            }
        }

        private static void ReportMissingPrefabs(Transform transform, Scene scene, CardGenerationReport report)
        {
            if (PrefabUtility.IsPrefabAssetMissing(transform.gameObject))
            {
                report.AddNote($"Couldn't re-link '{PathOf(transform)}' in {scene.path}: its prefab asset is missing. Place the generated prefab again.");
                return;
            }

            foreach (Transform child in transform)
            {
                ReportMissingPrefabs(child, scene, report);
            }
        }

        private static string PathOf(Transform transform)
        {
            return transform.parent == null ? transform.name : PathOf(transform.parent) + "/" + transform.name;
        }
    }
}
