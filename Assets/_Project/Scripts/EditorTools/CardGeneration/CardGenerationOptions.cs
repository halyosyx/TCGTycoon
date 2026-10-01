using System;
using Game.Unity.Definitions;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>Where TCG > Generate Card Data reads its manifests and writes its assets.</summary>
    [Serializable]
    public sealed class CardGenerationOptions
    {
        public const string DefaultManifestFolder = "Assets/_Project/Data/Manifests";
        public const string DefaultOutputFolder = "Assets/_Project/Data/Generated";
        public const string DefaultPriceTablePath = "Assets/_Project/Data/Balance/TierPrices.asset";

        /// <summary>Folder holding Sets.csv, the card manifests and TierPrices.csv.</summary>
        public string ManifestFolder = DefaultManifestFolder;

        /// <summary>Folder the cards, sets and visuals are written to (inside Assets/).</summary>
        public string OutputFolder = DefaultOutputFolder;

        /// <summary>The Tier Price Table asset written from TierPrices.csv.</summary>
        public string PriceTablePath = DefaultPriceTablePath;

        /// <summary>Tier colours and names for the generated materials, prefabs and card template.</summary>
        [NonSerialized]
        public RarityPaletteDefinition Palette;

        /// <summary>
        /// Validate pack configurations against the planned sets and re-link project references after the
        /// run. Off only for tests that generate into a temporary folder.
        /// </summary>
        public bool IncludeProjectReferences = true;
    }
}
