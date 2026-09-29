using Game.Core.Content;
using Game.Unity.UI.Controls;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI.Controls
{
    public sealed class UiKitControlTests
    {
        [Test]
        public void TierDisplay_TierTable_MatchesStyleGuideInLadderOrder()
        {
            string[] names = { "Common", "Uncommon", "Rare", "Holographic", "Full Art", "Alternate Illustration", "Special Illustration" };
            string[] codes = { "C", "U", "R", "H", "FA", "AI", "SI" };

            for (int tier = TierDisplay.Lowest; tier <= TierDisplay.Highest; tier++)
            {
                Assert.That(TierDisplay.NameOf(tier), Is.EqualTo(names[tier - 1]));
                Assert.That(TierDisplay.CodeOf(tier), Is.EqualTo(codes[tier - 1]));
            }
        }

        [Test]
        public void TierDisplay_FromRarity_CommonIsOneAndSpecialIllustrationIsSeven()
        {
            Assert.That(TierDisplay.FromRarity(RarityTier.Common), Is.EqualTo(1));
            Assert.That(TierDisplay.FromRarity(RarityTier.SpecialIllustration), Is.EqualTo(7));
        }

        [TestCase(0, 1)]
        [TestCase(8, 7)]
        public void CardFace_TierOutOfRange_ClampsToNearestTier(int requested, int expected)
        {
            var card = new CardFace { Tier = requested };

            Assert.That(card.Tier, Is.EqualTo(expected));
        }

        [Test]
        public void CardFace_SetCard_ShowsNameIdTierNameAndOnlyTheNewTierClass()
        {
            var card = new CardFace();
            card.SetCard(3, "Zenkin", "SetA_Rare_01");
            card.SetCard(5, "Orunos Colossus", "SetA_FullArt_01");

            Assert.That(card.CardName, Is.EqualTo("Orunos Colossus"));
            Assert.That(card.CardId, Is.EqualTo("SetA_FullArt_01"));
            Assert.That(card.TierName, Is.EqualTo("Full Art"));
            Assert.That(card.ClassListContains("card-face--tier-5"), Is.True);
            Assert.That(card.ClassListContains("card-face--tier-3"), Is.False);
        }

        [Test]
        public void CardFace_SetProduct_NeutralFaceWithDetailAndNoTierClass()
        {
            var card = new CardFace();
            card.SetCard(5, "Orunos", "SetA_FullArt_01");

            card.SetProduct("Booster Box", "Set A · 36 packs");

            Assert.That(card.IsProduct, Is.True);
            Assert.That(card.ClassListContains(CardFace.NeutralClassName), Is.True);
            Assert.That(card.ClassListContains("card-face--tier-5"), Is.False);
            Assert.That(card.CardName, Is.EqualTo("Booster Box"));
            Assert.That(card.TierName, Is.EqualTo("Set A · 36 packs"));
            Assert.That(card.CardId, Is.Empty);
        }

        [Test]
        public void CardFace_SetCardAfterProduct_BackToTierColouredCard()
        {
            var card = new CardFace();
            card.SetProduct("Booster Pack", "Set A · 1 pack");

            card.SetCard(3, "Rynorth", "SetA_Rare_04");

            Assert.That(card.IsProduct, Is.False);
            Assert.That(card.ClassListContains("card-face--tier-3"), Is.True);
            Assert.That(card.TierName, Is.EqualTo("Rare"));
        }

        [TestCase(152f, 4f)]
        [TestCase(250f, 6f)]
        [TestCase(60f, 3f)]
        public void CardFace_BorderWidthFor_IsFortiethOfWidthButAtLeastTheFloor(float cardWidth, float expected)
        {
            // 3 is the --border-width-card-min token's value.
            Assert.That(CardFace.BorderWidthFor(cardWidth, 3f), Is.EqualTo(expected));
        }

        [TestCase("3px", true, 3f)]
        [TestCase(" 2.5px ", true, 2.5f)]
        [TestCase("4", true, 4f)]
        [TestCase("wide", false, 0f)]
        [TestCase(null, false, 0f)]
        public void CardFace_TryParsePixels_ReadsUssLengths(string value, bool expectedParsed, float expectedPixels)
        {
            bool isParsed = CardFace.TryParsePixels(value, out float pixels);

            Assert.That(isParsed, Is.EqualTo(expectedParsed));
            if (expectedParsed)
            {
                Assert.That(pixels, Is.EqualTo(expectedPixels));
            }
        }

        [Test]
        public void TierChip_Compact_ShowsCodeInsteadOfName()
        {
            var chip = new TierChip { Tier = 6 };
            Assert.That(chip.Text, Is.EqualTo("Alternate Illustration"));

            chip.Compact = true;

            Assert.That(chip.Text, Is.EqualTo("AI"));
            Assert.That(chip.ClassListContains("tier-chip--tier-6"), Is.True);
        }

        [Test]
        public void CopyBadge_Count_ShowsCountWithPrefix()
        {
            var badge = new CopyBadge { Count = 12 };

            Assert.That(badge.text, Is.EqualTo("x12"));
        }

        [Test]
        public void KitTab_Active_TogglesActiveClass()
        {
            var tab = new KitTab { Title = "Set A", Count = 34, Active = true };
            Assert.That(tab.ClassListContains(KitTab.ActiveClassName), Is.True);
            Assert.That(tab.CountText, Is.EqualTo("34"));

            tab.Active = false;

            Assert.That(tab.ClassListContains(KitTab.ActiveClassName), Is.False);
        }

        [Test]
        public void KitButton_Variant_KeepsExactlyOneVariantClass()
        {
            var button = new KitButton { Variant = KitButton.ButtonVariant.Primary };
            button.Variant = KitButton.ButtonVariant.Ghost;

            Assert.That(button.ClassListContains("kit-button--ghost"), Is.True);
            Assert.That(button.ClassListContains("kit-button--primary"), Is.False);
            Assert.That(button.ClassListContains("kit-button--secondary"), Is.False);
        }

        [Test]
        public void KeyHints_Hints_ParsesPairsAndSpacesAllButFirst()
        {
            var hints = new KeyHints { Hints = "Tab:Inventory; F:Booth setup ;Esc:Pause;broken" };

            Assert.That(hints.HintCount, Is.EqualTo(3));
            Assert.That(hints[0].ClassListContains(KeyHints.SpacedHintClassName), Is.False);
            Assert.That(hints[1].ClassListContains(KeyHints.SpacedHintClassName), Is.True);
            Assert.That(hints[1].Q<Keycap>().text, Is.EqualTo("F"));
            Assert.That(hints[1].Q<Label>(className: KeyHints.LabelClassName).text, Is.EqualTo("Booth setup"));
        }

        [Test]
        public void Panel_Title_ShowsHeaderOnlyWhenSetAndChildrenGoToBody()
        {
            var panel = new Panel();
            var child = new Label("Body");
            panel.Add(child);
            VisualElement header = panel.hierarchy[0];
            Assert.That(header.style.display.value, Is.EqualTo(DisplayStyle.None));

            panel.Title = "Binder";

            Assert.That(header.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            // parent is the logical parent (the panel); hierarchy.parent is the element that holds it.
            Assert.That(child.parent, Is.SameAs(panel));
            Assert.That(child.hierarchy.parent.ClassListContains(Panel.BodyClassName), Is.True);
        }
    }
}
