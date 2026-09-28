using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Unity.Definitions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Writes the prototype card pool and its placeholder visuals through the AssetDatabase: cards, the
    /// set, tier materials, pack and card prefabs, the UI Toolkit card template and a README.
    /// Idempotent: assets are matched by card id or path and written only when a value differs, so a
    /// repeat run with the same settings changes no files. Everything is validated before anything is
    /// written, so a bad configuration never leaves a half-generated set.
    /// </summary>
    public static class CardAssetGenerator
    {
        public const string DefaultPalettePath = "Assets/_Project/Data/Visuals/RarityPalette.asset";
        public const string DefaultPriceTablePath = "Assets/_Project/Data/Balance/TierPrices.asset";

        private const string AssetsRoot = "Assets";

        /// <summary>World text needs TextMesh Pro's essentials (default font and shaders) in the project.</summary>
        public static bool HasTmpEssentials() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        public static RarityPaletteDefinition LoadOrCreateDefaultPalette(out bool created)
        {
            return LoadOrCreate<RarityPaletteDefinition>(DefaultPalettePath, palette => palette.ResetToDefaults(), out created);
        }

        public static TierPriceTableDefinition LoadOrCreateDefaultPriceTable(out bool created)
        {
            return LoadOrCreate<TierPriceTableDefinition>(DefaultPriceTablePath, prices => prices.ResetToDefaults(), out created);
        }

        /// <param name="packToLink">
        /// Optional pack to validate against and point at the generated set. It's only touched when its
        /// card set is missing or has the same set id, so packs for other sets are left alone.
        /// </param>
        public static CardGenerationReport Generate(
            CardGenerationSettings settings,
            RarityPaletteDefinition palette,
            TierPriceTableDefinition prices,
            PackConfigDefinition packToLink)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var report = new CardGenerationReport();
            string folder = (settings.OutputFolder ?? string.Empty).Trim().TrimEnd('/');
            if (palette == null) report.AddError("No Rarity Palette is assigned.");
            if (prices == null) report.AddError("No Tier Price Table is assigned.");
            if (!folder.StartsWith(AssetsRoot + "/", StringComparison.Ordinal)) report.AddError($"The output folder '{settings.OutputFolder}' must be inside Assets/.");
            if (!HasTmpEssentials()) report.AddError("TextMesh Pro Essential Resources aren't imported (Window > TextMeshPro > Import TMP Essential Resources).");
            if (!report.Succeeded)
            {
                return report;
            }

            IReadOnlyList<Card> plan;
            try
            {
                plan = CardGenerationPlan.Build(settings, prices.PriceCentsOf);
            }
            catch (ArgumentException exception)
            {
                report.AddError(exception.Message);
                return report;
            }
            catch (InvalidOperationException exception)
            {
                report.AddError(exception.Message);
                return report;
            }

            bool linkPack = packToLink != null && ShouldLink(packToLink, settings.SetId);
            if (linkPack)
            {
                foreach (string problem in CardGenerationPlan.FindPackProblems(plan, packToLink.ToPackConfig()))
                {
                    report.AddError(problem);
                }

                if (!report.Succeeded)
                {
                    return report;
                }
            }
            else if (packToLink != null)
            {
                report.AddNote($"Pack '{packToLink.name}' uses another set, so it was neither validated nor linked.");
            }

            string cardsFolder = EnsureFolder(folder + "/Cards");
            string materialsFolder = EnsureFolder(folder + "/Materials");
            string prefabsFolder = EnsureFolder(folder + "/Prefabs");
            string uiFolder = EnsureFolder(folder + "/UI");

            EnsureMaterials(materialsFolder, palette, settings.Overwrite, report);
            Dictionary<string, CardDefinition> cards = EnsureCards(cardsFolder, plan, settings.Overwrite, report);
            CardSetDefinition set = EnsureSet(folder, settings, plan, cards, report);
            EnsurePrefab(prefabsFolder + "/BoosterPack.prefab", "BoosterPack", settings.Overwrite, root => CardPrefabBuilder.BuildBoosterPack(root, palette), report);
            EnsurePrefab(prefabsFolder + "/WorldCard.prefab", "WorldCard", settings.Overwrite, root => CardPrefabBuilder.BuildWorldCard(root, palette), report);
            // Stylesheet first: the UXML references it, and importing the UXML before the USS exists logs an error.
            EnsureTextFile(uiFolder + "/" + CardTemplateFiles.UssFileName, CardTemplateFiles.Uss(palette), settings.Overwrite, report);
            EnsureTextFile(uiFolder + "/" + CardTemplateFiles.UxmlFileName, CardTemplateFiles.Uxml(), settings.Overwrite, report);
            EnsureTextFile(folder + "/" + CardTemplateFiles.ReadmeFileName, CardTemplateFiles.Readme(settings.SetId), settings.Overwrite, report);

            if (linkPack)
            {
                LinkPack(packToLink, set, report);
            }

            AssetDatabase.SaveAssets();
            return report;
        }

        private static bool ShouldLink(PackConfigDefinition pack, string setId)
        {
            return pack.CardSet == null || string.Equals(pack.CardSet.Id, setId, StringComparison.Ordinal);
        }

        private static void EnsureMaterials(string folder, RarityPaletteDefinition palette, bool overwrite, CardGenerationReport report)
        {
            var serializedPalette = new SerializedObject(palette);
            SerializedProperty styles = serializedPalette.FindProperty(RarityPaletteDefinition.TiersField);
            foreach (RarityTier tier in RarityTiers.All)
            {
                if (!palette.Defines(tier))
                {
                    report.AddNote($"The palette has no entry for {tier}; its material uses the missing-tier colour.");
                }

                Material material = EnsureMaterial($"{folder}/Tier_{tier}.mat", palette.ColorOf(tier), overwrite, report);
                SerializedProperty style = FindStyle(styles, tier);
                if (style != null)
                {
                    style.FindPropertyRelative(RarityStyle.MaterialField).objectReferenceValue = material;
                }
            }

            serializedPalette.FindProperty(RarityPaletteDefinition.CardFaceMaterialField).objectReferenceValue =
                EnsureMaterial(folder + "/CardFace.mat", palette.CardFaceColor, overwrite, report);
            serializedPalette.FindProperty(RarityPaletteDefinition.PackMaterialField).objectReferenceValue =
                EnsureMaterial(folder + "/BoosterPack.mat", palette.PackColor, overwrite, report);

            if (serializedPalette.ApplyModifiedPropertiesWithoutUndo())
            {
                report.AddNote("Linked the generated materials into the Rarity Palette.");
            }
        }

        private static Material EnsureMaterial(string path, Color color, bool overwrite, CardGenerationReport report)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(PaletteMaterialSync.LitShaderName));
                PaletteMaterialSync.ApplyFlatColor(material, color);
                AssetDatabase.CreateAsset(material, path);
                report.Count(AssetChange.Created);
                return material;
            }

            bool changed = overwrite && PaletteMaterialSync.ApplyFlatColor(material, color);
            report.Count(changed ? AssetChange.Updated : AssetChange.Unchanged);
            return material;
        }

        private static SerializedProperty FindStyle(SerializedProperty styles, RarityTier tier)
        {
            for (int index = 0; index < styles.arraySize; index++)
            {
                SerializedProperty style = styles.GetArrayElementAtIndex(index);
                if (style.FindPropertyRelative(RarityStyle.TierField).intValue == (int)tier)
                {
                    return style;
                }
            }

            return null;
        }

        private static Dictionary<string, CardDefinition> EnsureCards(string folder, IReadOnlyList<Card> plan, bool overwrite, CardGenerationReport report)
        {
            var existing = new Dictionary<string, CardDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CardDefinition), new[] { folder }))
            {
                var card = AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (card == null || string.IsNullOrEmpty(card.Id))
                {
                    continue;
                }

                if (!existing.ContainsKey(card.Id))
                {
                    existing.Add(card.Id, card);
                }
                else
                {
                    report.AddNote($"Two generated card assets share the id {card.Id}; the second was ignored.");
                }
            }

            var result = new Dictionary<string, CardDefinition>(plan.Count);
            foreach (Card planned in plan)
            {
                if (existing.TryGetValue(planned.Id, out CardDefinition definition))
                {
                    report.Count(overwrite && Apply(definition, planned) ? AssetChange.Updated : AssetChange.Unchanged);
                }
                else
                {
                    definition = ScriptableObject.CreateInstance<CardDefinition>();
                    Apply(definition, planned);
                    AssetDatabase.CreateAsset(definition, $"{folder}/{planned.Id}.asset");
                    report.Count(AssetChange.Created);
                }

                result.Add(planned.Id, definition);
            }

            if (overwrite)
            {
                foreach (KeyValuePair<string, CardDefinition> stale in existing)
                {
                    if (!result.ContainsKey(stale.Key))
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(stale.Value));
                        report.CountDeleted();
                    }
                }
            }

            return result;
        }

        private static bool Apply(CardDefinition definition, Card card)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty(CardDefinition.IdField).stringValue = card.Id;
            serialized.FindProperty(CardDefinition.DisplayNameField).stringValue = card.DisplayName;
            serialized.FindProperty(CardDefinition.TierField).intValue = (int)card.Tier;
            serialized.FindProperty(CardDefinition.ValueCentsField).longValue = card.ValueCents;
            return serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CardSetDefinition EnsureSet(
            string folder,
            CardGenerationSettings settings,
            IReadOnlyList<Card> plan,
            Dictionary<string, CardDefinition> cards,
            CardGenerationReport report)
        {
            string path = $"{folder}/{settings.SetId}.asset";
            var set = AssetDatabase.LoadAssetAtPath<CardSetDefinition>(path);
            bool created = set == null;
            if (!created && !settings.Overwrite)
            {
                report.Count(AssetChange.Unchanged);
                return set;
            }

            if (created)
            {
                set = ScriptableObject.CreateInstance<CardSetDefinition>();
            }

            var serialized = new SerializedObject(set);
            serialized.FindProperty(CardSetDefinition.IdField).stringValue = settings.SetId;
            serialized.FindProperty(CardSetDefinition.DisplayNameField).stringValue = settings.SetDisplayName;
            SerializedProperty cardList = serialized.FindProperty(CardSetDefinition.CardsField);
            cardList.arraySize = plan.Count;
            for (int index = 0; index < plan.Count; index++)
            {
                cardList.GetArrayElementAtIndex(index).objectReferenceValue = cards[plan[index].Id];
            }

            bool changed = serialized.ApplyModifiedPropertiesWithoutUndo();
            if (created)
            {
                AssetDatabase.CreateAsset(set, path);
            }

            report.Count(created ? AssetChange.Created : changed ? AssetChange.Updated : AssetChange.Unchanged);
            return set;
        }

        private static void EnsurePrefab(string path, string rootName, bool overwrite, Func<GameObject, bool> build, CardGenerationReport report)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            if (exists && !overwrite)
            {
                report.Count(AssetChange.Unchanged);
                return;
            }

            // Existing prefabs are edited in place: rebuilding from scratch would regenerate their
            // internal file ids and rewrite the file on every run.
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(rootName);
            try
            {
                bool changed = build(root);
                if (!exists || changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                    if (!saved)
                    {
                        report.AddError($"Couldn't save the prefab {path}.");
                        return;
                    }
                }

                report.Count(!exists ? AssetChange.Created : changed ? AssetChange.Updated : AssetChange.Unchanged);
            }
            finally
            {
                if (exists)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void EnsureTextFile(string path, string content, bool overwrite, CardGenerationReport report)
        {
            bool exists = File.Exists(path);
            if (exists && !overwrite)
            {
                report.Count(AssetChange.Unchanged);
                return;
            }

            bool isWritten = GeneratedTextFiles.WriteIfChanged(path, content);
            report.Count(!isWritten ? AssetChange.Unchanged : exists ? AssetChange.Updated : AssetChange.Created);
        }

        private static void LinkPack(PackConfigDefinition pack, CardSetDefinition set, CardGenerationReport report)
        {
            var serialized = new SerializedObject(pack);
            serialized.FindProperty(PackConfigDefinition.CardSetField).objectReferenceValue = set;
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                report.AddNote($"Pointed pack '{pack.name}' at the generated set '{set.name}'.");
            }

            // Checked again against the written assets, not just the plan.
            foreach (ValidationIssue issue in PackConfigValidator.Validate(pack.ToPackConfig(), set.ToCardPool()))
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    report.AddError($"{pack.name}: {issue}");
                }
            }
        }

        /// <summary>Creates any missing folders along <paramref name="path"/> and returns it.</summary>
        private static string EnsureFolder(string path)
        {
            string current = AssetsRoot;
            foreach (string part in path.Split('/'))
            {
                if (part == AssetsRoot)
                {
                    continue;
                }

                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }

                current = next;
            }

            return path;
        }

        private static TAsset LoadOrCreate<TAsset>(string path, Action<TAsset> initialise, out bool created)
            where TAsset : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            created = asset == null;
            if (created)
            {
                EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<TAsset>();
                initialise(asset);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
            }

            return asset;
        }
    }
}
