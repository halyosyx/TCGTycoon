using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using Game.Unity.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.UIStyles
{
    /// <summary>
    /// Writes the UI kit's tier colour tokens (--color-tier-1..7) from the Rarity Palette, the single
    /// source of tier colours, into <see cref="OutputPath"/>. Runs from the menu and automatically when
    /// the palette changes. Writing is idempotent: an unchanged palette leaves the file untouched.
    /// </summary>
    public static class RarityTokenExporter
    {
        public const string OutputPath = "Assets/_Project/UI/Styles/RarityTokens.uss";
        public const string MenuPath = "TCG/UI/Export Rarity Tokens";

        /// <summary>Token for a tier: tier 1 is the lowest (Common), so the number is the enum value plus one.</summary>
        public static string TokenName(RarityTier tier) => "--color-tier-" + ((int)tier + 1).ToString(CultureInfo.InvariantCulture);

        /// <summary>The full stylesheet text for a palette, with LF line endings.</summary>
        public static string BuildUss(RarityPaletteDefinition palette)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));

            var text = new StringBuilder();
            text.Append("/* GENERATED from the Rarity Palette (").Append(CardAssetGenerator.DefaultPalettePath).Append(") by ").Append(MenuPath).Append(".\n");
            text.Append("   Do not edit: change the palette and this file is rewritten. Tier 1 is the lowest tier. */\n");
            text.Append(":root {\n");
            foreach (RarityTier tier in RarityTiers.All)
            {
                text.Append("    ").Append(TokenName(tier)).Append(": ").Append(ToUssColor(palette.ColorOf(tier))).Append(";")
                    .Append("   /* ").Append(palette.DisplayNameOf(tier)).Append(" */\n");
            }

            text.Append("}\n");
            return text.ToString();
        }

        /// <summary>Tiers the palette has no entry for; their token falls back to the palette's missing-tier colour.</summary>
        public static IReadOnlyList<RarityTier> MissingTiers(RarityPaletteDefinition palette)
        {
            var missing = new List<RarityTier>();
            foreach (RarityTier tier in RarityTiers.All)
            {
                if (!palette.Defines(tier))
                {
                    missing.Add(tier);
                }
            }

            return missing;
        }

        /// <summary>Writes the stylesheet for <paramref name="palette"/> to <paramref name="path"/>. Returns whether the file changed.</summary>
        public static bool Export(RarityPaletteDefinition palette, string path = OutputPath)
        {
            IReadOnlyList<RarityTier> missing = MissingTiers(palette);
            if (missing.Count > 0)
            {
                Debug.LogError($"Rarity Palette has no colour for {string.Join(", ", missing)}; {path} uses the missing-tier colour for them.", palette);
            }

            return GeneratedTextFiles.WriteIfChanged(path, BuildUss(palette));
        }

        /// <summary>True when the file on disk matches what the palette would produce.</summary>
        public static bool IsUpToDate(RarityPaletteDefinition palette, string path = OutputPath)
        {
            return GeneratedTextFiles.Matches(path, BuildUss(palette));
        }

        /// <summary>
        /// Exports from the default palette after the current editor update, so it runs outside asset
        /// imports and inspector drawing. Calling it repeatedly in one update exports once.
        /// </summary>
        public static void ScheduleExport()
        {
            EditorApplication.delayCall -= ExportDefaultPalette;
            EditorApplication.delayCall += ExportDefaultPalette;
        }

        [MenuItem(MenuPath)]
        private static void ExportFromMenu()
        {
            RarityPaletteDefinition palette = LoadDefaultPalette();
            if (palette == null)
            {
                Debug.LogError($"No Rarity Palette at {CardAssetGenerator.DefaultPalettePath}; nothing to export.");
                return;
            }

            bool isChanged = Export(palette);
            Debug.Log(isChanged ? $"Rarity tokens written to {OutputPath}." : $"Rarity tokens already up to date ({OutputPath}).");
        }

        private static void ExportDefaultPalette()
        {
            RarityPaletteDefinition palette = LoadDefaultPalette();
            if (palette != null && Export(palette))
            {
                Debug.Log($"Rarity Palette changed: rarity tokens rewritten ({OutputPath}).");
            }
        }

        private static RarityPaletteDefinition LoadDefaultPalette()
        {
            return AssetDatabase.LoadAssetAtPath<RarityPaletteDefinition>(CardAssetGenerator.DefaultPalettePath);
        }

        // Opaque colours as #RRGGBB (what the kit's tokens use); translucent ones keep their alpha.
        private static string ToUssColor(Color color)
        {
            Color32 bytes = color;
            if (bytes.a == byte.MaxValue)
            {
                return "#" + ColorUtility.ToHtmlStringRGB(color);
            }

            return string.Format(CultureInfo.InvariantCulture, "rgba({0}, {1}, {2}, {3:0.###})", bytes.r, bytes.g, bytes.b, color.a);
        }
    }
}
