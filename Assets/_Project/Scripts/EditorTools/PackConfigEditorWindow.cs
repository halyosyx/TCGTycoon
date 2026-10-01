using System.Collections.Generic;
using System.Text;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Unity.Definitions;
using UnityEditor;
using UnityEngine;
using static System.FormattableString;

namespace Game.EditorTools
{
    /// <summary>
    /// Tune a pack configuration without Play Mode: edit slots and weights, see exact odds and
    /// validation, and simulate openings. Odds, validation and simulation all run the game's own Core
    /// code, and the asset is re-read on every repaint, so edits take effect with no recompile.
    /// </summary>
    public sealed class PackConfigEditorWindow : EditorWindow
    {
        private const int DefaultPackCount = 100_000;
        private const int MaxPackCount = 5_000_000;
        private const int DefaultSeed = 12345;
        private const float LabelColumnWidth = 190f;
        private const float NumberColumnWidth = 90f;
        private const float WeightFieldWidth = 70f;
        private const float SmallButtonWidth = 22f;
        private const float ButtonWidth = 100f;

        [SerializeField] private PackConfigDefinition _pack;
        [SerializeField] private int _packCount = DefaultPackCount;
        [SerializeField] private int _seed = DefaultSeed;

        private SerializedObject _serializedPack;
        private Vector2 _scroll;
        private PackTally _lastTally;
        private PackConfig _lastSimulatedConfig;
        private CardPool _lastSimulatedPool;
        private string _lastSimulationSnapshot;
        private int _lastSimulationSeed;

        [MenuItem("TCG/Pack Configuration Editor")]
        public static void Open() => GetWindow<PackConfigEditorWindow>("Pack Configuration");

        private void OnEnable()
        {
            if (_pack == null)
            {
                _pack = PackAssets.FindDefault();
            }
        }

        private void OnDisable()
        {
            _serializedPack?.Dispose();
            _serializedPack = null;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is PackConfigDefinition selected && selected != _pack)
            {
                _pack = selected;
                ClearSimulation();
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _pack = (PackConfigDefinition)EditorGUILayout.ObjectField("Pack", _pack, typeof(PackConfigDefinition), false);
            if (EditorGUI.EndChangeCheck())
            {
                ClearSimulation();
            }

            if (_pack == null)
            {
                EditorGUILayout.HelpBox("Pick a Pack Configuration asset (Create > TCG > Pack Configuration).", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawConfiguration();

            PackConfig config = _pack.ToPackConfig();
            if (!PackAssets.TryGetPool(_pack, out CardPool pool, out string poolError))
            {
                EditorGUILayout.HelpBox(poolError, MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            IReadOnlyList<ValidationIssue> issues = PackConfigValidator.Validate(config, pool);
            DrawValidation(issues, _pack.CardSet.MissingCardCount);
            if (!PackConfigValidator.HasErrors(issues))
            {
                DrawPackOdds(config);
                DrawExpectedValue(config, pool);
                DrawSimulation(config, pool);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfiguration()
        {
            if (_serializedPack == null || _serializedPack.targetObject != _pack)
            {
                _serializedPack?.Dispose();
                _serializedPack = new SerializedObject(_pack);
            }

            _serializedPack.Update();
            PackConfig configBeforeEdits = _pack.ToPackConfig();

            EditorGUILayout.PropertyField(_serializedPack.FindProperty(PackConfigDefinition.PriceField), new GUIContent("Price (cents)"));
            EditorGUILayout.PropertyField(_serializedPack.FindProperty(PackConfigDefinition.CardSetField));
            DrawSlots(_serializedPack.FindProperty(PackConfigDefinition.SlotsField), configBeforeEdits);

            _serializedPack.ApplyModifiedProperties();
        }

        private static void DrawSlots(SerializedProperty slots, PackConfig config)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Slots (reveal order), with each tier's chance in that slot", EditorStyles.boldLabel);

            int slotToRemove = -1;
            for (int slotIndex = 0; slotIndex < slots.arraySize; slotIndex++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Invariant($"Slot {slotIndex + 1}"), EditorStyles.boldLabel);
                if (GUILayout.Button("Remove slot", GUILayout.Width(ButtonWidth)))
                {
                    slotToRemove = slotIndex;
                }

                EditorGUILayout.EndHorizontal();

                PackSlot slot = slotIndex < config.Slots.Count ? config.Slots[slotIndex] : null;
                DrawSlotEntries(slots.GetArrayElementAtIndex(slotIndex).FindPropertyRelative(PackSlotData.EntriesField), slot);
                EditorGUILayout.EndVertical();
            }

            if (slotToRemove >= 0)
            {
                slots.DeleteArrayElementAtIndex(slotToRemove);
            }

            if (GUILayout.Button("Add slot", GUILayout.Width(ButtonWidth)))
            {
                slots.arraySize++;
            }
        }

        private static void DrawSlotEntries(SerializedProperty entries, PackSlot slot)
        {
            int entryToRemove = -1;
            for (int entryIndex = 0; entryIndex < entries.arraySize; entryIndex++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(entryIndex);
                SerializedProperty tier = entry.FindPropertyRelative(TierWeightData.TierField);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(tier, GUIContent.none, GUILayout.Width(LabelColumnWidth));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative(TierWeightData.WeightField), GUIContent.none, GUILayout.Width(WeightFieldWidth));
                string chance = slot == null ? string.Empty : Percent(PackAnalysis.TierProbability(slot, (RarityTier)tier.intValue));
                GUILayout.Label(chance, GUILayout.Width(NumberColumnWidth));
                if (GUILayout.Button("×", GUILayout.Width(SmallButtonWidth)))
                {
                    entryToRemove = entryIndex;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (entryToRemove >= 0)
            {
                entries.DeleteArrayElementAtIndex(entryToRemove);
            }

            if (GUILayout.Button("Add tier", GUILayout.Width(ButtonWidth)))
            {
                AddEntry(entries);
            }
        }

        private static void AddEntry(SerializedProperty entries)
        {
            // New entries start at weight 0 (no effect until tuned) on the next tier up, to avoid a duplicate-tier warning.
            // Stepping through the ladder, not "value + 1": tier values skip the retired 2-6.
            RarityTier nextTier = RarityTiers.All[0];
            if (entries.arraySize > 0)
            {
                var lastTier = (RarityTier)entries.GetArrayElementAtIndex(entries.arraySize - 1).FindPropertyRelative(TierWeightData.TierField).intValue;
                nextTier = RarityTiers.NextRarer(lastTier);
            }

            entries.arraySize++;
            SerializedProperty added = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            added.FindPropertyRelative(TierWeightData.TierField).intValue = (int)nextTier;
            added.FindPropertyRelative(TierWeightData.WeightField).intValue = 0;
        }

        private static void DrawValidation(IReadOnlyList<ValidationIssue> issues, int missingCardCount)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            if (missingCardCount > 0)
            {
                EditorGUILayout.HelpBox(Invariant($"The card set has {missingCardCount} empty entries; they're ignored."), MessageType.Warning);
            }

            if (issues.Count == 0 && missingCardCount == 0)
            {
                EditorGUILayout.HelpBox("No issues.", MessageType.Info);
            }

            foreach (ValidationIssue issue in issues)
            {
                MessageType type = issue.Severity == ValidationSeverity.Error ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.ToString(), type);
            }
        }

        private static void DrawPackOdds(PackConfig config)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pack odds: at least one card of this tier or better", EditorStyles.boldLabel);
            foreach (RarityTier tier in RarityTiers.All)
            {
                DrawRow(tier.ToString(), Percent(PackAnalysis.ChanceOfAtLeastOne(config, tier)));
            }
        }

        private static void DrawExpectedValue(PackConfig config, CardPool pool)
        {
            double expected = PackAnalysis.ExpectedValueCents(config, pool);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Expected Rip EV", DescribeValue(expected, config), EditorStyles.boldLabel);
        }

        private void DrawSimulation(PackConfig config, CardPool pool)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simulate", EditorStyles.boldLabel);
            _packCount = Mathf.Clamp(EditorGUILayout.IntField("Packs", _packCount), 1, MaxPackCount);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (GUILayout.Button("Simulate", GUILayout.Width(ButtonWidth)))
            {
                RunSimulation(config, pool);
            }

            if (_lastTally == null)
            {
                return;
            }

            if (Snapshot(config, pool) != _lastSimulationSnapshot)
            {
                EditorGUILayout.HelpBox("The configuration changed since this simulation. Simulate again to refresh.", MessageType.Warning);
            }

            EditorGUILayout.LabelField(Invariant($"{_lastTally.PackCount:N0} packs, seed {_lastSimulationSeed}"), EditorStyles.miniLabel);
            for (int slotIndex = 0; slotIndex < _lastSimulatedConfig.Slots.Count; slotIndex++)
            {
                EditorGUILayout.LabelField(Invariant($"Slot {slotIndex + 1}"), EditorStyles.boldLabel);
                DrawRow("Tier", "Expected", "Observed");
                foreach (RarityTier tier in RarityTiers.All)
                {
                    double expected = PackAnalysis.TierProbability(_lastSimulatedConfig.Slots[slotIndex], tier);
                    double observed = _lastTally.ObservedTierRate(slotIndex, tier);
                    if (expected > 0d || observed > 0d)
                    {
                        DrawRow(tier.ToString(), Percent(expected), Percent(observed));
                    }
                }
            }

            EditorGUILayout.LabelField("Pack: at least one of tier or better", EditorStyles.boldLabel);
            DrawRow("Tier", "Expected", "Observed");
            foreach (RarityTier tier in RarityTiers.All)
            {
                DrawRow(tier.ToString(), Percent(PackAnalysis.ChanceOfAtLeastOne(_lastSimulatedConfig, tier)), Percent(_lastTally.ObservedChanceOfAtLeastOne(tier)));
            }

            double observedEv = _lastTally.AverageValueCents;
            double expectedEv = PackAnalysis.ExpectedValueCents(_lastSimulatedConfig, _lastSimulatedPool);
            EditorGUILayout.LabelField("Observed Rip EV", DescribeValue(observedEv, _lastSimulatedConfig) + Invariant($"  ± {_lastTally.StandardErrorCents:0.0}¢ (1 standard error)"));
            EditorGUILayout.LabelField("Expected Rip EV", DescribeValue(expectedEv, _lastSimulatedConfig));
        }

        private void RunSimulation(PackConfig config, CardPool pool)
        {
            var opener = new PackOpener(config, pool, new SeededRng(_seed));
            _lastTally = PackSimulation.Run(opener, _packCount);
            _lastSimulatedConfig = config;
            _lastSimulatedPool = pool;
            _lastSimulationSnapshot = Snapshot(config, pool);
            _lastSimulationSeed = _seed;
        }

        private void ClearSimulation()
        {
            _lastTally = null;
            _lastSimulatedConfig = null;
            _lastSimulatedPool = null;
            _lastSimulationSnapshot = null;
        }

        private static void DrawRow(string label, params string[] columns)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(LabelColumnWidth));
            foreach (string column in columns)
            {
                GUILayout.Label(column, GUILayout.Width(NumberColumnWidth));
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>A compact description of everything that affects results, used to detect stale simulations.</summary>
        private static string Snapshot(PackConfig config, CardPool pool)
        {
            var snapshot = new StringBuilder();
            snapshot.Append(config.PriceCents).Append('|');
            foreach (PackSlot slot in config.Slots)
            {
                foreach (TierWeight entry in slot.Entries)
                {
                    snapshot.Append((int)entry.Tier).Append(':').Append(entry.Weight).Append(',');
                }

                snapshot.Append('/');
            }

            foreach (Card card in pool.Cards)
            {
                snapshot.Append(card.Id).Append(':').Append((int)card.Tier).Append(':').Append(card.ValueCents).Append(',');
            }

            return snapshot.ToString();
        }

        private static string DescribeValue(double valueCents, PackConfig config)
        {
            string share = config.PriceCents > 0 ? Invariant($"{valueCents / config.PriceCents * 100d:0.0}%") : "n/a";
            return Invariant($"{valueCents:0.0}¢ ({Money.FormatAverage(valueCents)}), {share} of {Money.Format(config.PriceCents)}");
        }

        private static string Percent(double share) => Invariant($"{share * 100d:0.00}%");
    }
}
