using System.Collections.Generic;
using Game.Core.Content;

namespace Game.Core.Tests.TestUtilities
{
    /// <summary>
    /// Builds the default pack (GDD v1.7, 7 slots, 500 cents) and a small Set A pool in code. The pack
    /// mirrors the default asset (Data/Products/SetA_BoosterPack) and the cards mirror the in-print tier
    /// values (Data/Manifests/TierPrices.csv), not the exact generated cards, so expected values match
    /// while card names and counts may differ. Game.Data.Tests checks the real assets separately, so
    /// drift between the two is caught.
    /// </summary>
    public static class TestContent
    {
        public const long StartingPackPriceCents = 500;

        public const long CommonValueCents = 5;
        public const long UncommonValueCents = 15;
        public const long HoloFullArtValueCents = 240;
        public const long SpecialFullArtHoloValueCents = 7000;

        /// <summary>
        /// Exact expected pull value of the default pack: slots 1-4 20 + slot 5 15 + slot 6 42 (Uncommon 88,
        /// Holo FA 12) + slot 7 341.4 (Holo FA 985, Special 15) = 418.4 cents, 83.7% of the price.
        /// </summary>
        public const double StartingExpectedValueCents = 418.4;

        public const string SetId = "SetA";

        public static PackConfig StartingPack()
        {
            return Pack(
                StartingPackPriceCents,
                Slot(Weight(RarityTier.Common, 100)),
                Slot(Weight(RarityTier.Common, 100)),
                Slot(Weight(RarityTier.Common, 100)),
                Slot(Weight(RarityTier.Common, 100)),
                Slot(Weight(RarityTier.Uncommon, 100)),
                Slot(Weight(RarityTier.Uncommon, 88), Weight(RarityTier.HoloFullArt, 12)),
                Slot(Weight(RarityTier.HoloFullArt, 985), Weight(RarityTier.SpecialFullArtHolo, 15)));
        }

        /// <summary>A 20-card pool with every tier, at the in-print tier values.</summary>
        public static CardPool SetAPool()
        {
            var cards = new List<Card>();
            AddCards(cards, RarityTier.Common, "C", count: 10, valueCents: CommonValueCents);
            AddCards(cards, RarityTier.Uncommon, "U", count: 6, valueCents: UncommonValueCents);
            AddCards(cards, RarityTier.HoloFullArt, "HFA", count: 3, valueCents: HoloFullArtValueCents);
            AddCards(cards, RarityTier.SpecialFullArtHolo, "SFAH", count: 1, valueCents: SpecialFullArtHoloValueCents);
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
                string id = $"RC_{code}_{number:000}";
                cards.Add(new Card(id, id, SetId, tier, valueCents));
            }
        }
    }
}
