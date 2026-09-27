using System;
using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>Options for the prototype card generator. The defaults produce the "Mythbound: First Light" placeholder set.</summary>
    [Serializable]
    public sealed class CardGenerationSettings
    {
        public const string DefaultOutputFolder = "Assets/_Project/Data/Generated";

        public string SetId = "SetA";
        public string SetDisplayName = "Mythbound: First Light";
        public int Seed = 1;
        public string OutputFolder = DefaultOutputFolder;

        /// <summary>On: update existing generated assets to match and delete cards no longer produced. Off: only create missing assets.</summary>
        public bool Overwrite = true;

        /// <summary>Cards to generate per tier, indexed by <see cref="RarityTier"/>.</summary>
        public int[] CardsPerTier = { 12, 9, 5, 3, 2, 2, 1 };

        public int CountOf(RarityTier tier)
        {
            int index = (int)tier;
            return CardsPerTier != null && index >= 0 && index < CardsPerTier.Length ? CardsPerTier[index] : 0;
        }
    }
}
