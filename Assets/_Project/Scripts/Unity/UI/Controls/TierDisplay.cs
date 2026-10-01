using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// The UI kit's tier table (GDD v1.7, four tiers): tier numbers 1 to 4, lowest first, with the
    /// display name, short name (chips, tabs) and code each control shows. Colours and glyphs are USS
    /// classes keyed by the same number (<c>--color-tier-N</c>, <c>.tier-glyph--N</c>), so this table
    /// holds text only. The names match the Rarity Palette (a Data test checks it).
    /// </summary>
    public static class TierDisplay
    {
        public const int Lowest = 1;
        public const int Highest = 4;

        private static readonly string[] s_names = { "Common", "Uncommon", "Holographic Full Art", "Special Full Art Holo" };

        private static readonly string[] s_shortNames = { "Common", "Uncommon", "Holo FA", "Special" };

        private static readonly string[] s_codes = { "C", "U", "HFA", "SFAH" };

        /// <summary>Out-of-range numbers show as the nearest tier rather than throwing inside a layout pass.</summary>
        public static int Clamp(int tier) => Mathf.Clamp(tier, Lowest, Highest);

        public static string NameOf(int tier) => s_names[Clamp(tier) - Lowest];

        public static string ShortNameOf(int tier) => s_shortNames[Clamp(tier) - Lowest];

        public static string CodeOf(int tier) => s_codes[Clamp(tier) - Lowest];

        /// <summary>
        /// The kit's tier number for a Core rarity tier: Common is 1. An undefined (stale) tier shows as
        /// the lowest rather than throwing inside a layout pass; validation reports it elsewhere.
        /// </summary>
        public static int FromRarity(RarityTier tier)
        {
            return RarityTiers.IsDefined(tier) ? RarityTiers.IndexOf(tier) + Lowest : Lowest;
        }

        /// <summary>A block's modifier class for a tier, e.g. <c>card-face--tier-3</c>.</summary>
        public static string ModifierClass(string block, int tier) => block + "--tier-" + Clamp(tier);
    }
}
