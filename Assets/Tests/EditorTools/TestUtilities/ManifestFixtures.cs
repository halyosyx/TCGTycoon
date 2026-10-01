using System.Collections.Generic;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;

namespace Game.EditorTools.Tests.TestUtilities
{
    /// <summary>
    /// Small two-set manifests in code: an in-print set (prefix TA, x100) and an out-of-print set
    /// (prefix TB, x180), one or two cards per tier, at the GDD v1.7 tier prices. Set ids are TestA and
    /// TestB so nothing matches the project's real SetA pack.
    /// </summary>
    public static class ManifestFixtures
    {
        public const string InPrintSetId = "TestA";
        public const string OutOfPrintSetId = "TestB";

        public static List<SetManifestEntry> Sets()
        {
            return new List<SetManifestEntry>
            {
                new SetManifestEntry(InPrintSetId, "Test: In Print", "In Print", "TA", SetLifecycle.InPrint, 100, "TestA.csv", 2),
                new SetManifestEntry(OutOfPrintSetId, "Test: Out Of Print", "Out Of Print", "TB", SetLifecycle.OutOfPrint, 180, "TestB.csv", 3),
            };
        }

        public static List<TierPriceEntry> Prices()
        {
            return new List<TierPriceEntry>
            {
                new TierPriceEntry(RarityTier.Common, 5, VolatilityTier.Low, 2),
                new TierPriceEntry(RarityTier.Uncommon, 15, VolatilityTier.Low, 3),
                new TierPriceEntry(RarityTier.HoloFullArt, 240, VolatilityTier.Medium, 4),
                new TierPriceEntry(RarityTier.SpecialFullArtHolo, 7000, VolatilityTier.High, 5),
            };
        }

        public static List<CardManifestEntry> Cards(string prefix, string namePrefix)
        {
            return new List<CardManifestEntry>
            {
                new CardManifestEntry($"{prefix}_C_001", namePrefix + "Common One", RarityTier.Common, lineNumber: 2),
                new CardManifestEntry($"{prefix}_C_002", namePrefix + "Common Two", RarityTier.Common, lineNumber: 3),
                new CardManifestEntry($"{prefix}_U_001", namePrefix + "Uncommon One", RarityTier.Uncommon, lineNumber: 4),
                new CardManifestEntry($"{prefix}_HFA_01", namePrefix + "Holo One", RarityTier.HoloFullArt, "Shines.", "foil", 5),
                new CardManifestEntry($"{prefix}_SFAH_01", namePrefix + "Special One", RarityTier.SpecialFullArtHolo, lineNumber: 6),
            };
        }

        public static Dictionary<string, IReadOnlyList<CardManifestEntry>> CardsBySet()
        {
            return new Dictionary<string, IReadOnlyList<CardManifestEntry>>
            {
                { InPrintSetId, Cards("TA", "A ") },
                { OutOfPrintSetId, Cards("TB", "B ") },
            };
        }

        public static CardDataPlan ValidPlan() => CardDataPlan.Build(Sets(), CardsBySet(), Prices());
    }
}
