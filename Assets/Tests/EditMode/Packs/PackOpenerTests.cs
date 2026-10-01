using System;
using System.Collections.Generic;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Packs;
using Game.Core.Tests.TestUtilities;
using NUnit.Framework;

namespace Game.Core.Tests.Packs
{
    public sealed class PackOpenerTests
    {
        private const int Seed = 20260926;
        private const int ManyPacks = 100_000;

        [Test]
        public void Open_StartingPack_ReturnsOneCardPerSlot()
        {
            PackOpener opener = CreateOpener(TestContent.StartingPack());

            OpenedPack pack = opener.Open();

            Assert.That(pack.Cards.Count, Is.EqualTo(7));
            Assert.That(pack.PackId, Is.EqualTo("test-pack"));
        }

        [Test]
        public void Open_StartingPack_EachCardTierIsEligibleForItsSlotInOrder()
        {
            PackConfig config = TestContent.StartingPack();
            PackOpener opener = CreateOpener(config);

            for (int i = 0; i < 10_000; i++)
            {
                AssertCardsMatchSlots(config, opener.Open());
            }
        }

        [Test]
        public void Open_SameSeed_ProducesIdenticalSequenceOfOpenings()
        {
            PackOpener first = CreateOpener(TestContent.StartingPack());
            PackOpener second = CreateOpener(TestContent.StartingPack());

            for (int packNumber = 0; packNumber < 1_000; packNumber++)
            {
                IReadOnlyList<Card> expected = first.Open().Cards;
                IReadOnlyList<Card> actual = second.Open().Cards;
                for (int slot = 0; slot < expected.Count; slot++)
                {
                    Assert.That(actual[slot].Id, Is.EqualTo(expected[slot].Id), $"Pack {packNumber}, slot {slot + 1} differs.");
                }
            }
        }

        [Test]
        public void Open_OneHundredThousandPacks_EachSlotMatchesConfiguredRatesWithinTolerance()
        {
            PackConfig config = TestContent.StartingPack();

            PackTally tally = PackSimulation.Run(CreateOpener(config), ManyPacks);

            for (int slot = 0; slot < config.Slots.Count; slot++)
            {
                foreach (RarityTier tier in RarityTiers.All)
                {
                    double expected = PackAnalysis.TierProbability(config.Slots[slot], tier);
                    Assert.That(
                        tally.ObservedTierRate(slot, tier),
                        Is.EqualTo(expected).Within(Tolerance.ForRate(expected, ManyPacks)),
                        $"Slot {slot + 1}, {tier}");
                }
            }
        }

        [Test]
        public void Open_SingleSlotPack_ReturnsOneEligibleCard()
        {
            PackConfig config = TestContent.Pack(100, TestContent.Slot(TestContent.Weight(RarityTier.HoloFullArt, 1)));

            OpenedPack pack = CreateOpener(config).Open();

            Assert.That(pack.Cards.Count, Is.EqualTo(1));
            Assert.That(pack.Cards[0].Tier, Is.EqualTo(RarityTier.HoloFullArt));
        }

        [Test]
        public void Open_SlotWithOneTier_AlwaysRollsThatTier()
        {
            PackConfig config = TestContent.Pack(
                100,
                TestContent.Slot(TestContent.Weight(RarityTier.HoloFullArt, 3)),
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 1)));
            PackOpener opener = CreateOpener(config);

            for (int i = 0; i < 1_000; i++)
            {
                Assert.That(opener.Open().Cards[0].Tier, Is.EqualTo(RarityTier.HoloFullArt));
            }
        }

        [TestCase(3)]
        [TestCase(12)]
        public void Open_PackOfNSlots_OneEligibleCardPerSlotInOrder(int slotCount)
        {
            // Slot count is data: alternating single-tier slots, so each card's slot is checkable.
            var slots = new List<PackSlot>();
            for (int slot = 0; slot < slotCount; slot++)
            {
                RarityTier tier = slot % 2 == 0 ? RarityTier.Common : RarityTier.HoloFullArt;
                slots.Add(TestContent.Slot(TestContent.Weight(tier, 1)));
            }

            PackConfig config = TestContent.Pack(100, slots.ToArray());
            PackOpener opener = CreateOpener(config);

            for (int i = 0; i < 1_000; i++)
            {
                OpenedPack pack = opener.Open();
                Assert.That(pack.Cards.Count, Is.EqualTo(slotCount));
                AssertCardsMatchSlots(config, pack);
            }
        }

        [Test]
        public void Open_TenSlotPackWithUnusualTiers_ReturnsTenEligibleCards()
        {
            // Rarest tiers first and mixed slots: nothing in code assumes a slot count or tier layout.
            var slots = new List<PackSlot>
            {
                TestContent.Slot(TestContent.Weight(RarityTier.SpecialFullArtHolo, 1)),
                TestContent.Slot(TestContent.Weight(RarityTier.Common, 1), TestContent.Weight(RarityTier.SpecialFullArtHolo, 1)),
            };
            for (int i = 0; i < 8; i++)
            {
                slots.Add(TestContent.Slot(TestContent.Weight(RarityTier.Uncommon, 5), TestContent.Weight(RarityTier.HoloFullArt, 1)));
            }

            PackConfig config = TestContent.Pack(1_000, slots.ToArray());
            PackOpener opener = CreateOpener(config);

            for (int i = 0; i < 1_000; i++)
            {
                OpenedPack pack = opener.Open();
                Assert.That(pack.Cards.Count, Is.EqualTo(10));
                AssertCardsMatchSlots(config, pack);
            }
        }

        [Test]
        public void Constructor_TierWithoutCardsInPool_Throws()
        {
            var poolWithoutHits = new CardPool(new[] { TestContent.CreateCard("OnlyCommon", RarityTier.Common) });

            Assert.Throws<InvalidOperationException>(
                () => new PackOpener(TestContent.StartingPack(), poolWithoutHits, new SeededRng(Seed)));
        }

        private static PackOpener CreateOpener(PackConfig config)
        {
            return new PackOpener(config, TestContent.SetAPool(), new SeededRng(Seed));
        }

        private static void AssertCardsMatchSlots(PackConfig config, OpenedPack pack)
        {
            Assert.That(pack.Cards.Count, Is.EqualTo(config.Slots.Count));
            for (int slot = 0; slot < config.Slots.Count; slot++)
            {
                RarityTier tier = pack.Cards[slot].Tier;
                Assert.That(
                    PackAnalysis.TierProbability(config.Slots[slot], tier),
                    Is.GreaterThan(0d),
                    $"Slot {slot + 1} can't roll {tier}, but it did.");
            }
        }
    }
}
