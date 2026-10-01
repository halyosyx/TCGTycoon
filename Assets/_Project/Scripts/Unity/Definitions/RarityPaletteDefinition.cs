using System.Collections.Generic;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// The one place card and prop colours are defined. Tier materials, world cards and UI cards all
    /// read it, so changing a colour here changes every card. Kept outside the generated folder so
    /// edits survive regeneration.
    /// </summary>
    [CreateAssetMenu(menuName = "TCG/Rarity Palette", fileName = "RarityPalette")]
    public sealed class RarityPaletteDefinition : ScriptableObject
    {
        public const string TiersField = nameof(_tiers);
        public const string CardFaceMaterialField = nameof(_cardFaceMaterial);
        public const string PackMaterialField = nameof(_packMaterial);

        /// <summary>Shown for a tier the palette doesn't define, so the gap is obvious rather than silently grey.</summary>
        public static readonly Color MissingTierColor = Color.magenta;

        [SerializeField, Tooltip("One entry per rarity tier.")]
        private List<RarityStyle> _tiers = new List<RarityStyle>();

        [SerializeField, Tooltip("Background of the card face behind the text.")]
        private Color _cardFaceColor;

        [SerializeField, Tooltip("Colour of card names and ids.")]
        private Color _cardTextColor;

        [SerializeField, Tooltip("Colour of the booster pack box.")]
        private Color _packColor;

        [SerializeField, Tooltip("Colour of the text printed on the booster pack.")]
        private Color _packLabelColor;

        [SerializeField, Tooltip("Generated material kept in sync with the card face colour.")]
        private Material _cardFaceMaterial;

        [SerializeField, Tooltip("Generated material kept in sync with the pack colour.")]
        private Material _packMaterial;

        public IReadOnlyList<RarityStyle> Tiers => _tiers;

        public Color CardFaceColor => _cardFaceColor;

        public Color CardTextColor => _cardTextColor;

        public Color PackColor => _packColor;

        public Color PackLabelColor => _packLabelColor;

        public Material CardFaceMaterial => _cardFaceMaterial;

        public Material PackMaterial => _packMaterial;

        public Color ColorOf(RarityTier tier)
        {
            RarityStyle style = Find(tier);
            return style == null ? MissingTierColor : style.Color;
        }

        public string DisplayNameOf(RarityTier tier)
        {
            RarityStyle style = Find(tier);
            return style == null ? tier.ToString() : style.DisplayName;
        }

        public Material MaterialOf(RarityTier tier)
        {
            RarityStyle style = Find(tier);
            return style == null ? null : style.Material;
        }

        /// <summary>The tier's short name for chips and tabs ("Holo FA"), falling back to its display name.</summary>
        public string ShortNameOf(RarityTier tier)
        {
            RarityStyle style = Find(tier);
            return style == null ? tier.ToString() : style.ShortName;
        }

        public bool Defines(RarityTier tier) => Find(tier) != null;

        /// <summary>
        /// Entries holding a removed or unknown tier, and defined tiers with no entry, each naming this
        /// asset. Empty when the palette matches the four-tier ladder.
        /// </summary>
        public List<string> FindTierProblems()
        {
            var problems = new List<string>();
            foreach (RarityStyle style in _tiers)
            {
                if (style != null && !RarityTiers.IsDefined(style.Tier))
                {
                    problems.Add($"Rarity palette '{name}' has an entry for {RarityTiers.Describe(style.Tier)}.");
                }
            }

            foreach (RarityTier tier in RarityTiers.All)
            {
                if (!Defines(tier))
                {
                    problems.Add($"Rarity palette '{name}' has no entry for {tier}.");
                }
            }

            return problems;
        }

        /// <summary>
        /// Restores the four-tier colours (GDD v1.7): Common and Uncommon keep the UI kit's colours;
        /// Holographic Full Art and Special Full Art Holo start from the old Full Art pink and Special
        /// Illustration gold until the designer's new token sheet lands. The palette is the single
        /// source of tier colours; TCG > UI > Export Rarity Tokens turns it into --color-tier-1..4. Tier
        /// colours are meant for borders and glyphs: Common is only about 3.7:1 against the card face,
        /// so card names and tier names use the card text colour on kit card faces.
        /// </summary>
        public void ResetToDefaults()
        {
            _tiers = new List<RarityStyle>
            {
                new RarityStyle(RarityTier.Common, "Common", "Common", new Color32(0x7C, 0x76, 0x71, 0xFF)),
                new RarityStyle(RarityTier.Uncommon, "Uncommon", "Uncommon", new Color32(0x55, 0x9A, 0x69, 0xFF)),
                new RarityStyle(RarityTier.HoloFullArt, "Holographic Full Art", "Holo FA", new Color32(0xF2, 0xA5, 0xC7, 0xFF)),
                new RarityStyle(RarityTier.SpecialFullArtHolo, "Special Full Art Holo", "Special", new Color32(0xFD, 0xE0, 0x96, 0xFF)),
            };
            _cardFaceColor = new Color32(0x1B, 0x1F, 0x24, 0xFF);
            _cardTextColor = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
            _packColor = new Color32(0x3A, 0x2F, 0x5B, 0xFF);
            _packLabelColor = new Color32(0xF2, 0xE6, 0xC8, 0xFF);
        }

        private void Reset() => ResetToDefaults();

        private RarityStyle Find(RarityTier tier)
        {
            foreach (RarityStyle style in _tiers)
            {
                if (style != null && style.Tier == tier)
                {
                    return style;
                }
            }

            return null;
        }
    }
}
