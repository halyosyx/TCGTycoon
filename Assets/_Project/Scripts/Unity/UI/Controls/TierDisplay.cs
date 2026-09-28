using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// The UI kit's tier table (style guide §6): tier numbers 1 to 7, lowest first, with the name and
    /// short code each control shows. Colours and glyphs are USS classes keyed by the same number
    /// (<c>--color-tier-N</c>, <c>.tier-glyph--N</c>), so this table holds text only.
    /// </summary>
    public static class TierDisplay
    {
        public const int Lowest = 1;
        public const int Highest = 7;

        private static readonly string[] s_names =
        {
            "Common", "Uncommon", "Rare", "Holographic", "Full Art", "Alternate Illustration", "Special Illustration",
        };

        private static readonly string[] s_codes = { "C", "U", "R", "H", "FA", "AI", "SI" };

        /// <summary>Out-of-range numbers show as the nearest tier rather than throwing inside a layout pass.</summary>
        public static int Clamp(int tier) => Mathf.Clamp(tier, Lowest, Highest);

        public static string NameOf(int tier) => s_names[Clamp(tier) - Lowest];

        public static string CodeOf(int tier) => s_codes[Clamp(tier) - Lowest];

        /// <summary>The kit's tier number for a Core rarity tier: Common is 1.</summary>
        public static int FromRarity(RarityTier tier) => Clamp((int)tier + Lowest);

        /// <summary>A block's modifier class for a tier, e.g. <c>card-face--tier-3</c>.</summary>
        public static string ModifierClass(string block, int tier) => block + "--tier-" + Clamp(tier);
    }
}
