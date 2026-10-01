using System.Collections.Generic;
using Game.Core.Content;
using Game.EditorTools.CardGeneration;
using NUnit.Framework;

namespace Game.EditorTools.Tests.CardGeneration
{
    public sealed class CardManifestTests
    {
        private const string CardsText =
            "id,name,tier,flavour,artHint\n" +
            "RC_C_001,Zenkin,Common,,\n" +
            "RC_U_001,Silmir,Uncommon,\"Quick, quiet\",fox\n" +
            "RC_HFA_01,Orunos Colossus,HoloFullArt,,\n" +
            "RC_SFAH_01,Braolin Crowned in Ash,SpecialFullArtHolo,Rules the ash.,crown\n";

        private const string SetsText =
            "setId,displayName,shortName,idPrefix,lifecycle,priceScalePercent,cardManifest\n" +
            "SetA,Mythbound: Reigning Champions,Champions,RC,InPrint,100,Champions.csv\n" +
            "SetB,Mythbound: Mythical Origins,Origins,MO,OutOfPrint,180,Origins.csv\n";

        private const string PricesText =
            "tier,basePriceCents,volatilityTier\n" +
            "Common,5,Low\n" +
            "Uncommon,15,Low\n" +
            "HoloFullArt,240,Medium\n" +
            "SpecialFullArtHolo,7000,High\n";

        [Test]
        public void Cards_ParseWriteParse_IdenticalTextAndEntries()
        {
            var errors = new List<string>();
            List<CardManifestEntry> first = CardManifest.ReadCards(CardsText, "Champions.csv", errors);
            string written = CardManifest.WriteCards(first);
            List<CardManifestEntry> second = CardManifest.ReadCards(written, "Champions.csv", errors);

            Assert.That(errors, Is.Empty);
            Assert.That(written, Is.EqualTo(CardsText));
            Assert.That(Describe(second), Is.EqualTo(Describe(first)));
            Assert.That(first[1].Flavour, Is.EqualTo("Quick, quiet"));
            Assert.That(first[3].Tier, Is.EqualTo(RarityTier.SpecialFullArtHolo));
            Assert.That(first[3].LineNumber, Is.EqualTo(5));
        }

        [Test]
        public void Sets_ParseWriteParse_IdenticalText()
        {
            var errors = new List<string>();
            List<SetManifestEntry> sets = CardManifest.ReadSets(SetsText, "Sets.csv", errors);

            Assert.That(errors, Is.Empty);
            Assert.That(CardManifest.WriteSets(sets), Is.EqualTo(SetsText));
            Assert.That(sets[1].Lifecycle, Is.EqualTo(SetLifecycle.OutOfPrint));
            Assert.That(sets[1].PriceScalePercent, Is.EqualTo(180));
            Assert.That(sets[1].ShortName, Is.EqualTo("Origins"));
        }

        [Test]
        public void TierPrices_ParseWriteParse_IdenticalText()
        {
            var errors = new List<string>();
            List<TierPriceEntry> prices = CardManifest.ReadTierPrices(PricesText, "TierPrices.csv", errors);

            Assert.That(errors, Is.Empty);
            Assert.That(CardManifest.WriteTierPrices(prices), Is.EqualTo(PricesText));
            Assert.That(prices[3].BasePriceCents, Is.EqualTo(7000));
            Assert.That(prices[3].Volatility, Is.EqualTo(VolatilityTier.High));
        }

        [Test]
        public void ReadCards_RemovedTierName_ErrorSaysItWasRemoved()
        {
            var errors = new List<string>();

            List<CardManifestEntry> cards = CardManifest.ReadCards("id,name,tier,flavour,artHint\nRC_FA_01,Old,FullArt,,\n", "Champions.csv", errors);

            Assert.That(cards, Is.Empty);
            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.StartWith("Champions.csv:2:").And.Contain("removed"));
        }

        [Test]
        public void ReadCards_UnknownTierName_ErrorNamesTheValidTiers()
        {
            var errors = new List<string>();

            CardManifest.ReadCards("id,name,tier,flavour,artHint\nRC_C_001,Zenkin,Mythic,,\n", "Champions.csv", errors);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.Contain("unknown tier \"Mythic\"").And.Contain("HoloFullArt"));
        }

        [Test]
        public void ReadCards_WrongHeader_FileLevelError()
        {
            var errors = new List<string>();

            CardManifest.ReadCards("id,name,rarity\nRC_C_001,Zenkin,Common\n", "Champions.csv", errors);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.StartWith("Champions.csv: the header must be"));
        }

        [Test]
        public void ReadTierPrices_NotANumber_ErrorWithLine()
        {
            var errors = new List<string>();

            CardManifest.ReadTierPrices("tier,basePriceCents,volatilityTier\nCommon,five,Low\n", "TierPrices.csv", errors);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.StartWith("TierPrices.csv:2:"));
        }

        private static List<string> Describe(List<CardManifestEntry> cards)
        {
            var lines = new List<string>();
            foreach (CardManifestEntry card in cards)
            {
                lines.Add($"{card.Id}|{card.Name}|{card.Tier}|{card.Flavour}|{card.ArtHint}");
            }

            return lines;
        }
    }
}
