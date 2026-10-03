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
    /// TCG > Generate Card Data's pipeline: reads the CSV manifests (<see cref="CardDataPlan"/>) and
    /// upserts the generated assets through the AssetDatabase: cards, card sets, the Tier Price Table,
    /// tier materials, pack and card prefabs, the UI Toolkit card template and a README.
    /// <list type="bullet">
    /// <item>Upsert by id: new rows create assets, changed rows update the existing asset in place.</item>
    /// <item>Never deletes or re-creates, so GUIDs (and every reference to them) are stable. Assets no
    /// manifest produces any more are reported as orphans.</item>
    /// <item>Idempotent: values are written only when they differ, so a repeat run changes no files.</item>
    /// <item>Everything is validated first; any error means nothing is written.</item>
    /// </list>
    /// </summary>
    public static class CardAssetGenerator
    {
        public const string DefaultPalettePath = "Assets/_Project/Data/Visuals/RarityPalette.asset";

        private const string AssetsRoot = "Assets";
        private const string TierMaterialPrefix = "Tier_";
        private const string MaterialExtension = ".mat";

        /// <summary>World text needs TextMesh Pro's essentials (default font and shaders) in the project.</summary>
        public static bool HasTmpEssentials() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        public static RarityPaletteDefinition LoadOrCreateDefaultPalette(out bool created)
        {
            return LoadOrCreate<RarityPaletteDefinition>(DefaultPalettePath, palette => palette.ResetToDefaults(), out created);
        }

        /// <summary>The generated card template's path for an output folder.</summary>
        public static string CardTemplatePath(string outputFolder) => outputFolder.TrimEnd('/') + "/UI/" + CardTemplateFiles.UxmlFileName;

        public static CardGenerationReport Generate(CardGenerationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var report = new CardGenerationReport();
            string folder = (options.OutputFolder ?? string.Empty).Trim().TrimEnd('/');
            RarityPaletteDefinition palette = options.Palette;
            if (palette == null) report.AddError("No Rarity Palette is assigned.");
            if (!folder.StartsWith(AssetsRoot + "/", StringComparison.Ordinal)) report.AddError($"The output folder '{options.OutputFolder}' must be inside Assets/.");
            if (!(options.PriceTablePath ?? string.Empty).StartsWith(AssetsRoot + "/", StringComparison.Ordinal)) report.AddError($"The price table path '{options.PriceTablePath}' must be inside Assets/.");
            if (!HasTmpEssentials()) report.AddError("TextMesh Pro Essential Resources aren't imported (Window > TextMeshPro > Import TMP Essential Resources).");
            if (palette != null)
            {
                foreach (string problem in palette.FindTierProblems()) report.AddError(problem);
            }

            CardDataPlan plan = CardDataPlan.LoadFolder(options.ManifestFolder);
            foreach (string error in plan.Errors) report.AddError(error);
            if (report.Succeeded && options.IncludeProjectReferences)
            {
                CheckPacks(plan, report);
            }

            Dictionary<string, CardDefinition> existingCards = report.Succeeded ? FindExistingCards(folder + "/Cards", report) : null;
            if (!report.Succeeded)
            {
                return report;
            }

            string cardsFolder = EnsureFolder(folder + "/Cards");
            string materialsFolder = EnsureFolder(folder + "/Materials");
            string prefabsFolder = EnsureFolder(folder + "/Prefabs");
            string uiFolder = EnsureFolder(folder + "/UI");

            EnsureTierPrices(options.PriceTablePath, plan.Prices, report);
            EnsureMaterials(materialsFolder, palette, report);
            Dictionary<string, CardDefinition> cards = EnsureCards(cardsFolder, existingCards, plan, report);
            Dictionary<string, CardSetDefinition> sets = EnsureSets(folder, plan, cards, report);
            EnsurePrefab(prefabsFolder + "/BoosterPack.prefab", "BoosterPack", root => CardPrefabBuilder.BuildBoosterPack(root, palette), report);
            EnsurePrefab(prefabsFolder + "/WorldCard.prefab", "WorldCard", root => CardPrefabBuilder.BuildWorldCard(root, palette), report);
            // Stylesheet first: the UXML references it, and importing the UXML before the USS exists logs an error.
            EnsureTextFile(uiFolder + "/" + CardTemplateFiles.UssFileName, CardTemplateFiles.Uss(palette), report);
            EnsureTextFile(uiFolder + "/" + CardTemplateFiles.UxmlFileName, CardTemplateFiles.Uxml(), report);
            EnsureTextFile(folder + "/" + CardTemplateFiles.ReadmeFileName, CardTemplateFiles.Readme(), report);

            ReportOrphanMaterials(materialsFolder, report);
            if (options.IncludeProjectReferences)
            {
                GeneratedReferenceLinker.Relink(sets, CardTemplatePath(folder), report);
            }

            AssetDatabase.SaveAssets();
            return report;
        }

        /// <summary>
        /// Fills empty card names in the manifests (<see cref="ManifestNameFiller"/>) and writes back the
        /// files that changed. Names already present are never touched.
        /// </summary>
        public static string FillMissingNames(string manifestFolder, out bool succeeded)
        {
            CardDataPlan plan = CardDataPlan.LoadFolder(manifestFolder, requireNames: false);
            if (!plan.IsValid)
            {
                succeeded = false;
                return "Fill missing names stopped; fix these first:\n" + string.Join("\n", plan.Errors);
            }

            var sets = new List<SetManifestEntry>();
            var cardsBySetId = new Dictionary<string, IReadOnlyList<CardManifestEntry>>(StringComparer.Ordinal);
            foreach (PlannedSet set in plan.Sets)
            {
                sets.Add(set.Entry);
                var entries = new List<CardManifestEntry>(set.Cards.Count);
                foreach (PlannedCard card in set.Cards) entries.Add(card.Entry);
                cardsBySetId.Add(set.SetId, entries);
            }

            Dictionary<string, List<CardManifestEntry>> filled;
            try
            {
                filled = ManifestNameFiller.Fill(sets, cardsBySetId, out int filledCount);
                if (filledCount == 0)
                {
                    succeeded = true;
                    return "Every card already has a name; nothing to fill.";
                }
            }
            catch (InvalidOperationException exception)
            {
                succeeded = false;
                return exception.Message;
            }

            int written = 0;
            foreach (SetManifestEntry set in sets)
            {
                string path = manifestFolder.TrimEnd('/') + "/" + set.CardManifest;
                if (GeneratedTextFiles.WriteIfChanged(path, CardManifest.WriteCards(filled[set.SetId]))) written++;
            }

            succeeded = true;
            return $"Filled missing names; {written} manifest file(s) written. Review the names, then Generate.";
        }

        // A pack that rolls a tier its set has no cards for can't open; catch it before writing the set.
        private static void CheckPacks(CardDataPlan plan, CardGenerationReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(PackConfigDefinition)))
            {
                var pack = AssetDatabase.LoadAssetAtPath<PackConfigDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (pack == null || pack.CardSet == null)
                {
                    continue;
                }

                PlannedSet set = plan.FindSet(pack.CardSet.Id);
                if (set == null)
                {
                    continue;
                }

                foreach (string problem in pack.FindTierProblems()) report.AddError(problem);
                foreach (ValidationIssue issue in PackConfigValidator.Validate(pack.ToPackConfig(), set.ToCardPool()))
                {
                    if (issue.Severity == ValidationSeverity.Error)
                    {
                        report.AddError($"Pack '{pack.name}': {issue}");
                    }
                }
            }
        }

        private static void EnsureTierPrices(string path, IReadOnlyList<TierPriceEntry> prices, CardGenerationReport report)
        {
            var table = AssetDatabase.LoadAssetAtPath<TierPriceTableDefinition>(path);
            bool created = table == null;
            if (created)
            {
                EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
                table = ScriptableObject.CreateInstance<TierPriceTableDefinition>();
            }

            var serialized = new SerializedObject(table);
            SerializedProperty list = serialized.FindProperty(TierPriceTableDefinition.PricesField);
            list.arraySize = prices.Count;
            for (int index = 0; index < prices.Count; index++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative(TierPrice.TierField).intValue = (int)prices[index].Tier;
                entry.FindPropertyRelative(TierPrice.PriceCentsField).longValue = prices[index].BasePriceCents;
                entry.FindPropertyRelative(TierPrice.VolatilityField).intValue = (int)prices[index].Volatility;
            }

            bool changed = serialized.ApplyModifiedPropertiesWithoutUndo();
            if (created)
            {
                AssetDatabase.CreateAsset(table, path);
            }

            report.Count(created ? AssetChange.Created : changed ? AssetChange.Updated : AssetChange.Unchanged);
        }

        private static void EnsureMaterials(string folder, RarityPaletteDefinition palette, CardGenerationReport report)
        {
            var serializedPalette = new SerializedObject(palette);
            SerializedProperty styles = serializedPalette.FindProperty(RarityPaletteDefinition.TiersField);
            foreach (RarityTier tier in RarityTiers.All)
            {
                Material material = EnsureMaterial(TierMaterialPath(folder, tier), palette.ColorOf(tier), report);
                SerializedProperty style = FindStyle(styles, tier);
                if (style != null)
                {
                    style.FindPropertyRelative(RarityStyle.MaterialField).objectReferenceValue = material;
                }
            }

            serializedPalette.FindProperty(RarityPaletteDefinition.CardFaceMaterialField).objectReferenceValue =
                EnsureMaterial(folder + "/CardFace.mat", palette.CardFaceColor, report);
            serializedPalette.FindProperty(RarityPaletteDefinition.PackMaterialField).objectReferenceValue =
                EnsureMaterial(folder + "/BoosterPack.mat", palette.PackColor, report);

            if (serializedPalette.ApplyModifiedPropertiesWithoutUndo())
            {
                report.AddNote("Linked the generated materials into the Rarity Palette.");
            }
        }

        private static string TierMaterialPath(string folder, RarityTier tier) => $"{folder}/{TierMaterialPrefix}{tier}{MaterialExtension}";

        private static Material EnsureMaterial(string path, Color color, CardGenerationReport report)
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

            report.Count(PaletteMaterialSync.ApplyFlatColor(material, color) ? AssetChange.Updated : AssetChange.Unchanged);
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

        // Matched by id anywhere under Cards/, so a card keeps its asset (and GUID) wherever it sits.
        // Runs before anything is written: two assets with one id is an error that stops the run.
        private static Dictionary<string, CardDefinition> FindExistingCards(string folder, CardGenerationReport report)
        {
            var existing = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return existing;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CardDefinition), new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                if (card == null || string.IsNullOrEmpty(card.Id))
                {
                    report.AddOrphan($"{path} (no id)");
                    continue;
                }

                if (existing.ContainsKey(card.Id))
                {
                    report.AddError($"Two generated card assets share the id {card.Id}: {AssetDatabase.GetAssetPath(existing[card.Id])} and {path}. Delete one.");
                    continue;
                }

                existing.Add(card.Id, card);
            }

            return existing;
        }

        private static Dictionary<string, CardDefinition> EnsureCards(
            string folder,
            Dictionary<string, CardDefinition> existing,
            CardDataPlan plan,
            CardGenerationReport report)
        {
            var result = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
            foreach (PlannedSet set in plan.Sets)
            {
                string setFolder = null;
                foreach (PlannedCard planned in set.Cards)
                {
                    if (existing.TryGetValue(planned.Id, out CardDefinition definition))
                    {
                        report.Count(Apply(definition, planned) ? AssetChange.Updated : AssetChange.Unchanged);
                    }
                    else
                    {
                        setFolder = setFolder ?? EnsureFolder(folder + "/" + set.SetId);
                        definition = ScriptableObject.CreateInstance<CardDefinition>();
                        Apply(definition, planned);
                        AssetDatabase.CreateAsset(definition, $"{setFolder}/{planned.Id}.asset");
                        report.Count(AssetChange.Created);
                    }

                    result.Add(planned.Id, definition);
                }
            }

            foreach (KeyValuePair<string, CardDefinition> card in existing)
            {
                if (!result.ContainsKey(card.Key))
                {
                    report.AddOrphan($"{AssetDatabase.GetAssetPath(card.Value)} (card {card.Key}, in no manifest)");
                }
            }

            return result;
        }

        private static bool Apply(CardDefinition definition, PlannedCard card)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty(CardDefinition.IdField).stringValue = card.Id;
            serialized.FindProperty(CardDefinition.DisplayNameField).stringValue = card.Entry.Name;
            serialized.FindProperty(CardDefinition.TierField).intValue = (int)card.Tier;
            serialized.FindProperty(CardDefinition.ValueCentsField).longValue = card.ValueCents;
            serialized.FindProperty(CardDefinition.FlavourField).stringValue = card.Entry.Flavour;
            serialized.FindProperty(CardDefinition.ArtHintField).stringValue = card.Entry.ArtHint;
            return serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Dictionary<string, CardSetDefinition> EnsureSets(
            string folder,
            CardDataPlan plan,
            Dictionary<string, CardDefinition> cards,
            CardGenerationReport report)
        {
            var existing = new Dictionary<string, CardSetDefinition>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CardSetDefinition), new[] { folder }))
            {
                var set = AssetDatabase.LoadAssetAtPath<CardSetDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (set != null && !string.IsNullOrEmpty(set.Id) && !existing.ContainsKey(set.Id))
                {
                    existing.Add(set.Id, set);
                }
            }

            var result = new Dictionary<string, CardSetDefinition>(StringComparer.Ordinal);
            foreach (PlannedSet planned in plan.Sets)
            {
                bool created = !existing.TryGetValue(planned.SetId, out CardSetDefinition set);
                if (created)
                {
                    set = ScriptableObject.CreateInstance<CardSetDefinition>();
                }

                SetManifestEntry entry = planned.Entry;
                var serialized = new SerializedObject(set);
                serialized.FindProperty(CardSetDefinition.IdField).stringValue = entry.SetId;
                serialized.FindProperty(CardSetDefinition.DisplayNameField).stringValue = entry.DisplayName;
                serialized.FindProperty(CardSetDefinition.ShortNameField).stringValue = entry.ShortName;
                serialized.FindProperty(CardSetDefinition.IdPrefixField).stringValue = entry.IdPrefix;
                serialized.FindProperty(CardSetDefinition.LifecycleField).intValue = (int)entry.Lifecycle;
                serialized.FindProperty(CardSetDefinition.PriceScalePercentField).intValue = entry.PriceScalePercent;
                if (ColorUtility.TryParseHtmlString(entry.Colour, out Color colour))
                {
                    serialized.FindProperty(CardSetDefinition.ColourField).colorValue = colour;
                }
                SerializedProperty cardList = serialized.FindProperty(CardSetDefinition.CardsField);
                cardList.arraySize = planned.Cards.Count;
                for (int index = 0; index < planned.Cards.Count; index++)
                {
                    cardList.GetArrayElementAtIndex(index).objectReferenceValue = cards[planned.Cards[index].Id];
                }

                bool changed = serialized.ApplyModifiedPropertiesWithoutUndo();
                if (created)
                {
                    AssetDatabase.CreateAsset(set, $"{folder}/{entry.SetId}.asset");
                }

                report.Count(created ? AssetChange.Created : changed ? AssetChange.Updated : AssetChange.Unchanged);
                result.Add(planned.SetId, set);
            }

            foreach (KeyValuePair<string, CardSetDefinition> set in existing)
            {
                if (!result.ContainsKey(set.Key))
                {
                    report.AddOrphan($"{AssetDatabase.GetAssetPath(set.Value)} (set {set.Key}, not in {CardManifest.SetsFileName})");
                }
            }

            return result;
        }

        private static void ReportOrphanMaterials(string folder, CardGenerationReport report)
        {
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (RarityTier tier in RarityTiers.All)
            {
                expected.Add(TierMaterialPath(folder, tier));
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith(TierMaterialPrefix, StringComparison.Ordinal) && !expected.Contains(path))
                {
                    report.AddOrphan($"{path} (material for a tier that no longer exists)");
                }
            }
        }

        private static void EnsurePrefab(string path, string rootName, Func<GameObject, bool> build, CardGenerationReport report)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

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

        private static void EnsureTextFile(string path, string content, CardGenerationReport report)
        {
            bool exists = File.Exists(path);
            bool isWritten = GeneratedTextFiles.WriteIfChanged(path, content);
            report.Count(!isWritten ? AssetChange.Unchanged : exists ? AssetChange.Updated : AssetChange.Created);
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
