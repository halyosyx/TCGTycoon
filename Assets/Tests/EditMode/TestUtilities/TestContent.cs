using System.Collections.Generic;
using Game.Core.Content;

namespace Game.Core.Tests.TestUtilities
{
    /// <summary>
    /// Builds the F1a starting pack and a small Set A pool in code. The pack mirrors the default asset
    /// (Data/Products/SetA_BoosterPack) and the cards mirror its tier values, not the exact generated
    /// cards (Data/Generated), so expected values match while card names and counts may differ.
    /// Game.Data.Tests checks the real assets separately, so drift between the two is caught.
    /// </summary>
    public static class TestContent
    {
        public const long StartingPackPriceCents = 425;

        /// <summary>Exact expected pull value of the starting pack: 15 + 19.5 + 327.5 cents.</summary>
        public const double StartingExpectedValueCents = 362.0;

        public const string SetId = "SetA";

        public static PackConfig StartingPack()
        {
            return Pack(
                StartingPackPriceCents,
                Slot(Weight(RarityTier.Common, 100)),
                Slot(Weight(RarityTier.Common, 100)),
                Slot(Weight(RarityTier.Common, 100)),
                Slot(Weight(RarityTier.Uncommon, 90), Weight(RarityTier.Rare, 10)),
                Slot(
                    Weight(RarityTier.Rare, 600),
                    Weight(RarityTier.Holographic, 250),
                    Weight(RarityTier.FullArt, 105),
                    Weight(RarityTier.AlternateIllustration, 40),
                    Weight(RarityTier.SpecialIllustration, 5)));
        }

        /// <summary>30 placeholder cards with the brief's per-tier values.</summary>
        public static CardPool SetAPool()
        {
            var cards = new List<Card>();
            AddCards(cards, RarityTier.Common, "C", count: 10, valueCents: 5);
            AddCards(cards, RarityTier.Uncommon, "U", count: 7, valueCents: 15);
            AddCards(cards, RarityTier.Rare, "R", count: 5, valueCents: 60);
            AddCards(cards, RarityTier.Holographic, "H", count: 3, valueCents: 250);
            AddCards(cards, RarityTier.FullArt, "FA", count: 2, valueCents: 800);
            AddCards(cards, RarityTier.AlternateIllustration, "AI", count: 2, valueCents: 2500);
            AddCards(cards, RarityTier.SpecialIllustration, "SI", count: 1, valueCents: 9000);
            return new CardPool(cards);
        }

        public static PackConfig Pack(long priceCents, params PackSlot[] slots)
        {
            return new PackConfig("test-pack", "Test Pack", priceCents, slots);
        }

        public static PackSlot Slot(params TierWeight[] entries) => new PackSlot(entries);

        public static TierWeight Weight(RarityTier tier, int weight) => new TierWeight(tier, weight);

        public static Card CreateCard(string id, RarityTier tier, long valueCents = 0)
        {
            return new Card(id, id, SetId, tier, valueCents);
        }

        private static void AddCards(List<Card> cards, RarityTier tier, string code, int count, long valueCents)
        {
            for (int number = 1; number <= count; number++)
            {
                string id = $"{SetId}_{code}{number:00}";
                cards.Add(new Card(id, id, SetId, tier, valueCents));
            }
        }
    }
}
