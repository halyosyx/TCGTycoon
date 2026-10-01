using Game.Unity.Definitions;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// TCG > Generate Card Data: generates every card set from the CSV manifests in Data/Manifests and
    /// reports the run (counts, orphans, re-links). Fill missing names writes generated names into empty
    /// name cells only, so names typed into the CSVs stick. Failures are shown here and logged as errors,
    /// never as blocking dialogs.
    /// </summary>
    public sealed class CardGeneratorWindow : EditorWindow
    {
        [SerializeField] private CardGenerationOptions _options = new CardGenerationOptions();
        [SerializeField] private RarityPaletteDefinition _palette;

        private string _lastReport;
        private bool _lastSucceeded;
        private Vector2 _scroll;

        [MenuItem("TCG/Generate Card Data")]
        public static void Open() => GetWindow<CardGeneratorWindow>("Card Data");

        private void OnEnable()
        {
            if (_options == null) _options = new CardGenerationOptions();
            if (_palette == null) _palette = AssetDatabase.LoadAssetAtPath<RarityPaletteDefinition>(CardAssetGenerator.DefaultPalettePath);
        }

        private void OnDisable() => UnsubscribeFromImport();

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);
            _options.ManifestFolder = EditorGUILayout.TextField(new GUIContent("Manifests", "Sets.csv, one card CSV per set, TierPrices.csv."), _options.ManifestFolder);
            _options.OutputFolder = EditorGUILayout.TextField(new GUIContent("Output", "Generated cards, sets and visuals."), _options.OutputFolder);
            _options.PriceTablePath = EditorGUILayout.TextField(new GUIContent("Tier price table", "Written from TierPrices.csv."), _options.PriceTablePath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
            _palette = (RarityPaletteDefinition)EditorGUILayout.ObjectField(new GUIContent("Rarity palette", "Created with default colours if empty."), _palette, typeof(RarityPaletteDefinition), false);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Generate", "Upsert every set by id. Never deletes; reports orphans.")))
                {
                    Generate();
                }

                if (GUILayout.Button(new GUIContent("Fill missing names", "Write generated names into empty name cells. Existing names are kept.")))
                {
                    FillMissingNames();
                }
            }

            if (!string.IsNullOrEmpty(_lastReport))
            {
                EditorGUILayout.HelpBox(_lastReport, _lastSucceeded ? MessageType.Info : MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Runs the generator with the window's options. Public so automation can drive the same path as the button.</summary>
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

            _options.Palette = _palette;
            CardGenerationReport report = CardAssetGenerator.Generate(_options);
            Show(report.Succeeded, report + createdInputs);
            return report;
        }

        /// <summary>Fills empty names in the manifests. Public so automation can drive the same path as the button.</summary>
        public string FillMissingNames()
        {
            string message = CardAssetGenerator.FillMissingNames(_options.ManifestFolder, out bool succeeded);
            Show(succeeded, message);
            return message;
        }

        private void Show(bool succeeded, string message)
        {
            _lastSucceeded = succeeded;
            _lastReport = message;
            if (succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            Repaint();
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
            Show(false, $"Importing TextMesh Pro Essential Resources failed: {errorMessage}");
        }

        private void OnImportCancelled(string packageName)
        {
            UnsubscribeFromImport();
            Show(false, "Importing TextMesh Pro Essential Resources was cancelled, so nothing was generated.");
        }

        private void UnsubscribeFromImport()
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
        }
    }
}
