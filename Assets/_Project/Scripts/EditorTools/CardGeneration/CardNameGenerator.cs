using Game.Core.Common;
using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Seeded creature-like card names built from small syllable tables ("Vorlune", "Thalarmir Sentinel").
    /// A name depends only on (seed, tier, index, attempt), so the same inputs always give the same name
    /// and changing one tier's card count doesn't rename cards in other tiers. Higher tiers get slightly
    /// longer, more ornate names.
    /// </summary>
    public static class CardNameGenerator
    {
        // Folds (seed, tier, index, attempt) into one RNG seed. Any large odd multipliers work; these
        // only have to stay fixed so existing names don't change.
        private const long SeedMultiplier = 1_000_003L;
        private const long TierMultiplier = 1_009L;
        private const long IndexMultiplier = 101L;

        private static readonly string[] s_openings =
        {
            "Vor", "Em", "Thal", "Bra", "Kess", "Myr", "Or", "Zen", "Fal", "Ryn",
            "Cor", "Ash", "Lum", "Dra", "Sil", "Vel", "Nym", "Tor", "Grim", "Iri",
        };

        private static readonly string[] s_middles = { "a", "e", "i", "o", "ar", "el", "or", "un", "ith", "ae" };

        private static readonly string[] s_endings =
        {
            "lune", "kin", "ra", "dor", "wyn", "mir", "gar", "ris", "vex", "lin", "dros", "th", "ane", "os",
        };

        private static readonly string[] s_titles =
        {
            "Sentinel", "Warden", "Seer", "Stalker", "Drake", "Herald", "Wisp", "Colossus", "Tidecaller", "Wyrm",
        };

        private static readonly string[] s_ornateTitles =
        {
            "of the First Light", "the Unbroken", "Crowned in Ash", "Keeper of Tides", "the Last Dawn",
        };

        /// <param name="index">The card's number within its tier.</param>
        /// <param name="attempt">Bump to get a different name for the same card (used to resolve duplicates).</param>
        public static string Generate(int seed, RarityTier tier, int index, int attempt = 0)
        {
            var rng = new SeededRng(((seed * SeedMultiplier + (int)tier) * TierMultiplier + index) * IndexMultiplier + attempt);

            // Common and Uncommon get two syllables; Rare and up get three.
            string name = tier <= RarityTier.Uncommon
                ? Pick(s_openings, rng) + Pick(s_endings, rng)
                : Pick(s_openings, rng) + Pick(s_middles, rng) + Pick(s_endings, rng);

            if (tier == RarityTier.SpecialIllustration)
            {
                return name + " " + Pick(s_ornateTitles, rng);
            }

            return tier >= RarityTier.FullArt ? name + " " + Pick(s_titles, rng) : name;
        }

        private static string Pick(string[] options, IRng rng) => options[rng.NextInt(options.Length)];
    }
}
