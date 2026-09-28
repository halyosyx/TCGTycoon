using Game.Core.Content;
using Game.Unity.Definitions;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// TCG > Generate Prototype Card Data: options for the placeholder card generator and a report of
    /// the last run. Failures are shown here and logged as errors, never as blocking dialogs.
    /// </summary>
    public sealed class CardGeneratorWindow : EditorWindow
    {
        private const float CountFieldWidth = 60f;

        [SerializeField] private CardGenerationSettings _settings = new CardGenerationSettings();
        [SerializeField] private RarityPaletteDefinition _palette;
        [SerializeField] private TierPriceTableDefinition _prices;
        [SerializeField] private PackConfigDefinition _pack;

        private string _lastReport;
        private bool _lastSucceeded;
        private Vector2 _scroll;

        [MenuItem("TCG/Generate Prototype Card Data")]
        public static void Open() => GetWindow<CardGeneratorWindow>("Card Generator");

        private void OnEnable()
        {
            // Saved window settings may predate a newly added tier.
            if (_settings == null) _settings = new CardGenerationSettings();
            if (_settings.CardsPerTier == null || _settings.CardsPerTier.Length != RarityTiers.Count)
            {
                System.Array.Resize(ref _settings.CardsPerTier, RarityTiers.Count);
            }

            if (_palette == null) _palette = AssetDatabase.LoadAssetAtPath<RarityPaletteDefinition>(CardAssetGenerator.DefaultPalettePath);
            if (_prices == null) _prices = AssetDatabase.LoadAssetAtPath<TierPriceTableDefinition>(CardAssetGenerator.DefaultPriceTablePath);
            if (_pack == null) _pack = PackAssets.FindDefault();
        }

        private void OnDisable() => UnsubscribeFromImport();

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Set", EditorStyles.boldLabel);
            _settings.SetId = EditorGUILayout.TextField("Set id", _settings.SetId);
            _settings.SetDisplayName = EditorGUILayout.TextField("Display name", _settings.SetDisplayName);
            _settings.Seed = EditorGUILayout.IntField(new GUIContent("Seed", "Same seed, same names."), _settings.Seed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cards per tier", EditorStyles.boldLabel);
            foreach (RarityTier tier in RarityTiers.All)
            {
                int index = (int)tier;
                _settings.CardsPerTier[index] = Mathf.Max(0, EditorGUILayout.IntField(tier.ToString(), _settings.CardsPerTier[index], GUILayout.MinWidth(CountFieldWidth)));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _settings.OutputFolder = EditorGUILayout.TextField("Folder", _settings.OutputFolder);
            _settings.Overwrite = EditorGUILayout.Toggle(new GUIContent("Overwrite", "On: update existing generated assets and delete cards no longer produced. Off: only create missing assets."), _settings.Overwrite);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
            _palette = (RarityPaletteDefinition)EditorGUILayout.ObjectField(new GUIContent("Rarity palette", "Created with default colours if empty."), _palette, typeof(RarityPaletteDefinition), false);
            _prices = (TierPriceTableDefinition)EditorGUILayout.ObjectField(new GUIContent("Tier prices", "Created with the F1a prices if empty."), _prices, typeof(TierPriceTableDefinition), false);
            _pack = (PackConfigDefinition)EditorGUILayout.ObjectField(new GUIContent("Pack to validate", "Checked before writing; pointed at the generated set when its set is missing or has the same id."), _pack, typeof(PackConfigDefinition), false);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate"))
            {
                Generate();
            }

            if (!string.IsNullOrEmpty(_lastReport))
            {
                EditorGUILayout.HelpBox(_lastReport, _lastSucceeded ? MessageType.Info : MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Runs the generator with the window's settings. Public so automation can drive the same path as the button.</summary>
        public CardGenerationReport Generate()
        {
            if (!CardAssetGenerator.HasTmpEssentials())
            {
                StartTmpImport();
                return null;
            }

            string createdInputs = string.Empty;
            if (_palette == null)
            {
                _palette = CardAssetGenerator.LoadOrCreateDefaultPalette(out bool created);
                if (created) createdInputs += $"\nCreated the Rarity Palette at {CardAssetGenerator.DefaultPalettePath}.";
            }

            if (_prices == null)
            {
                _prices = CardAssetGenerator.LoadOrCreateDefaultPriceTable(out bool created);
                if (created) createdInputs += $"\nCreated the Tier Price Table at {CardAssetGenerator.DefaultPriceTablePath}.";
            }

            CardGenerationReport report = CardAssetGenerator.Generate(_settings, _palette, _prices, _pack);
            _lastSucceeded = report.Succeeded;
            _lastReport = report + createdInputs;
            if (report.Succeeded)
            {
                Debug.Log(_lastReport);
            }
            else
            {
                Debug.LogError(_lastReport);
            }

            Repaint();
            return report;
        }

        // The TMP import is asynchronous, so generation resumes when Unity reports it finished.
        private void StartTmpImport()
        {
            UnsubscribeFromImport();
            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageFailed += OnImportFailed;
            AssetDatabase.importPackageCancelled += OnImportCancelled;
            _lastSucceeded = true;
            _lastReport = "Importing TextMesh Pro Essential Resources. Generation runs automatically when the import finishes.";
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        private void OnImportCompleted(string packageName)
        {
            UnsubscribeFromImport();
            if (CardAssetGenerator.HasTmpEssentials())
            {
                Generate();
            }
        }

        private void OnImportFailed(string packageName, string errorMessage)
        {
            UnsubscribeFromImport();
            _lastSucceeded = false;
            _lastReport = $"Importing TextMesh Pro Essential Resources failed: {errorMessage}";
            Debug.LogError(_lastReport);
            Repaint();
        }

        private void OnImportCancelled(string packageName)
        {
            UnsubscribeFromImport();
            _lastSucceeded = false;
            _lastReport = "Importing TextMesh Pro Essential Resources was cancelled, so nothing was generated.";
            Repaint();
        }

        private void UnsubscribeFromImport()
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
        }
    }
}
