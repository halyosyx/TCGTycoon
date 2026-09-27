using Game.Unity.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Keeps generated materials equal to the palette, the single source of card colours. Used by the
    /// generator when it creates materials and by the palette inspector whenever a colour is edited.
    /// </summary>
    public static class PaletteMaterialSync
    {
        /// <summary>Low smoothness gives the flat, matte placeholder look.</summary>
        public const float Smoothness = 0.15f;

        public const string LitShaderName = "Universal Render Pipeline/Lit";

        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_smoothnessId = Shader.PropertyToID("_Smoothness");

        /// <summary>Makes <paramref name="material"/> URP Lit, flat <paramref name="color"/>, low smoothness. Returns true if anything changed.</summary>
        public static bool ApplyFlatColor(Material material, Color color)
        {
            bool changed = false;
            Shader lit = Shader.Find(LitShaderName);
            if (lit != null && material.shader != lit)
            {
                material.shader = lit;
                changed = true;
            }

            if (material.HasProperty(s_baseColorId) && material.GetColor(s_baseColorId) != color)
            {
                material.SetColor(s_baseColorId, color);
                changed = true;
            }

            if (material.HasProperty(s_smoothnessId) && !Mathf.Approximately(material.GetFloat(s_smoothnessId), Smoothness))
            {
                material.SetFloat(s_smoothnessId, Smoothness);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(material);
            }

            return changed;
        }

        /// <summary>Pushes every palette colour into its linked material. Returns how many materials changed.</summary>
        public static int SyncColors(RarityPaletteDefinition palette)
        {
            int changedCount = 0;
            foreach (RarityStyle style in palette.Tiers)
            {
                if (style != null && style.Material != null && ApplyFlatColor(style.Material, style.Color))
                {
                    changedCount++;
                }
            }

            if (palette.CardFaceMaterial != null && ApplyFlatColor(palette.CardFaceMaterial, palette.CardFaceColor))
            {
                changedCount++;
            }

            if (palette.PackMaterial != null && ApplyFlatColor(palette.PackMaterial, palette.PackColor))
            {
                changedCount++;
            }

            return changedCount;
        }
    }
}
