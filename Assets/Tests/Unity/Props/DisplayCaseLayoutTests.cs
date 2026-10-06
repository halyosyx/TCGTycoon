using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.Props;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests.Props
{
    public sealed class DisplayCaseLayoutTests
    {
        private static readonly Vector2 s_cardSize = new Vector2(0.063f, 0.088f);
        private const float Gap = 0.01f;
        private const float Lift = 0.004f;

        private static readonly Card s_common = new Card("RC_C_001", "Common one", "SetA", RarityTier.Common, 5);
        private static readonly Card s_holo = new Card("RC_HFA_01", "Holo one", "SetA", RarityTier.HoloFullArt, 240);
        private static readonly Card s_other = new Card("RC_U_001", "Uncommon one", "SetA", RarityTier.Uncommon, 15);

        [Test]
        public void SlotPosition_TwentyFiveDistinctSlotsInsideTheGrid()
        {
            var seen = new List<Vector3>();
            float halfWidth = (DisplayCaseLayout.Columns * (s_cardSize.x + Gap)) * 0.5f;
            float halfDepth = (DisplayCaseLayout.Rows * (s_cardSize.y + Gap)) * 0.5f;

            for (int slot = 0; slot < DisplayCaseLayout.SlotCount; slot++)
            {
                Vector3 position = DisplayCaseLayout.SlotPosition(slot, s_cardSize, Gap, Lift);
                Assert.That(seen, Has.None.EqualTo(position), $"Slot {slot}");
                Assert.That(Mathf.Abs(position.x) + s_cardSize.x * 0.5f, Is.LessThanOrEqualTo(halfWidth + 0.0001f));
                Assert.That(Mathf.Abs(position.z) + s_cardSize.y * 0.5f, Is.LessThanOrEqualTo(halfDepth + 0.0001f));
                Assert.That(position.y, Is.EqualTo(Lift));
                seen.Add(position);
            }

            Assert.That(DisplayCaseLayout.SlotCount, Is.EqualTo(25));
        }

        [Test]
        public void SlotPosition_FirstSlotIsTheFarLeftCornerAsTheVendorSeesIt()
        {
            Vector3 first = DisplayCaseLayout.SlotPosition(0, s_cardSize, Gap, Lift);
            Vector3 last = DisplayCaseLayout.SlotPosition(DisplayCaseLayout.SlotCount - 1, s_cardSize, Gap, Lift);

            Assert.That(first.x, Is.LessThan(0f));
            Assert.That(first.z, Is.GreaterThan(0f), "Far side (+Z, away from the vendor).");
            Assert.That(last.x, Is.GreaterThan(0f));
            Assert.That(last.z, Is.LessThan(0f));
        }

        [Test]
        public void SlotPosition_WiderColumnGap_SpreadsColumnsAndKeepsRows()
        {
            var gaps = new Vector2(0.044f, Gap);

            Vector3 first = DisplayCaseLayout.SlotPosition(0, s_cardSize, gaps, Lift);
            Vector3 next = DisplayCaseLayout.SlotPosition(1, s_cardSize, gaps, Lift);
            Vector3 below = DisplayCaseLayout.SlotPosition(DisplayCaseLayout.Columns, s_cardSize, gaps, Lift);

            Assert.That(next.x - first.x, Is.EqualTo(s_cardSize.x + 0.044f).Within(0.0001f));
            Assert.That(first.z - below.z, Is.EqualTo(s_cardSize.y + Gap).Within(0.0001f));
            Assert.That(first.x, Is.EqualTo(-2f * (s_cardSize.x + 0.044f)).Within(0.0001f), "The grid stays centred.");
        }

        [Test]
        public void Fill_OneSlotPerCopyInCoreOrder_OnlyTheDisplayCase()
        {
            var inventory = new InventoryService();
            Add(inventory, s_common, 3, ItemLocation.DisplayCase);
            Add(inventory, s_other, 4, ItemLocation.Binder);
            Add(inventory, s_holo, 2, ItemLocation.DisplayCase);
            var slots = new List<ItemRef>();

            int count = DisplayCaseLayout.Fill(inventory.Stacks, slots);

            Assert.That(count, Is.EqualTo(5));
            ItemRef common = ItemRef.Card(s_common.Id, s_common.Tier);
            ItemRef holo = ItemRef.Card(s_holo.Id, s_holo.Tier);
            Assert.That(slots, Is.EqualTo(new[] { common, common, common, holo, holo }));
        }

        [Test]
        public void Fill_ClearsWhatTheListHeld()
        {
            var inventory = new InventoryService();
            Add(inventory, s_holo, 1, ItemLocation.DisplayCase);
            var slots = new List<ItemRef> { ItemRef.Card("stale", RarityTier.Common) };

            DisplayCaseLayout.Fill(inventory.Stacks, slots);

            Assert.That(slots.Count, Is.EqualTo(1));
            Assert.That(slots[0].Id, Is.EqualTo(s_holo.Id));
        }

        // The case keeps no list: what it shows comes from Core, so the lid can't change it.
        [Test]
        public void Contents_SurviveTheLidClosingAndReopening()
        {
            var inventory = new InventoryService();
            Add(inventory, s_common, 6, ItemLocation.DisplayCase);
            Add(inventory, s_holo, 4, ItemLocation.DisplayCase);
            var before = new List<ItemRef>();
            DisplayCaseLayout.Fill(inventory.Stacks, before);
            var lid = new GlassCaseLid(0.3f, 90f);

            lid.Toggle();
            lid.Tick(1f);
            lid.Toggle();
            lid.Tick(1f);
            Assert.That(lid.State, Is.EqualTo(GlassCaseLidState.Closed));
            lid.Toggle();
            lid.Tick(1f);

            var after = new List<ItemRef>();
            DisplayCaseLayout.Fill(inventory.Stacks, after);
            Assert.That(after, Is.EqualTo(before));
            Assert.That(inventory.CountIn(ItemLocation.DisplayCase), Is.EqualTo(10));
            Assert.That(lid.IsOpen, Is.True);
        }

        private static void Add(InventoryService inventory, Card card, int count, ItemLocation location)
        {
            for (int i = 0; i < count; i++)
            {
                inventory.Add(card, card.ValueCents);
            }

            if (location != ItemLocation.Binder)
            {
                Assert.That(inventory.Move(ItemRef.Card(card.Id, card.Tier), ItemLocation.Binder, location, count).IsSuccess, Is.True, "Setup");
            }
        }
    }
}
