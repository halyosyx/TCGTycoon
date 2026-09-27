using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using NUnit.Framework;

namespace Game.EditorTools.Tests.CardGeneration
{
    public sealed class CardNameGeneratorTests
    {
        private const int Seed = 1;
        private const int NamesPerTier = 50;

        [Test]
        public void Generate_SameInputs_ReturnsSameName()
        {
            foreach (RarityTier tier in RarityTiers.All)
            {
                for (int index = 0; index < NamesPerTier; index++)
                {
                    Assert.That(CardNameGenerator.Generate(Seed, tier, index), Is.EqualTo(CardNameGenerator.Generate(Seed, tier, index)));
                }
            }
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceMostlyDifferentNames()
        {
            int matches = 0;
            for (int index = 0; index < NamesPerTier; index++)
            {
                if (CardNameGenerator.Generate(1, RarityTier.Rare, index) == CardNameGenerator.Generate(2, RarityTier.Rare, index)) matches++;
            }

            Assert.That(matches, Is.LessThan(5));
        }

        [Test]
        public void Generate_DifferentAttempt_ChangesName()
        {
            Assert.That(CardNameGenerator.Generate(Seed, RarityTier.Common, 0, attempt: 1), Is.Not.EqualTo(CardNameGenerator.Generate(Seed, RarityTier.Common, 0)));
        }

        [TestCase(RarityTier.Common)]
        [TestCase(RarityTier.Uncommon)]
        [TestCase(RarityTier.Rare)]
        [TestCase(RarityTier.Holographic)]
        public void Generate_BelowFullArt_IsOneCapitalisedWord(RarityTier tier)
        {
            for (int index = 0; index < NamesPerTier; index++)
            {
                string name = CardNameGenerator.Generate(Seed, tier, index);
                Assert.That(name, Does.Not.Contain(" "), name);
                Assert.That(char.IsUpper(name[0]), name);
            }
        }

        [TestCase(RarityTier.FullArt)]
        [TestCase(RarityTier.AlternateIllustration)]
        [TestCase(RarityTier.SpecialIllustration)]
        public void Generate_FullArtAndAbove_AddsATitle(RarityTier tier)
        {
            for (int index = 0; index < NamesPerTier; index++)
            {
                string name = CardNameGenerator.Generate(Seed, tier, index);
                Assert.That(name, Does.Contain(" "), name);
                Assert.That(char.IsUpper(name[0]), name);
            }
        }
    }
}
