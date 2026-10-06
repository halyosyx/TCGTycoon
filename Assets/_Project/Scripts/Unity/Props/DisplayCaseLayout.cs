using System.Collections.Generic;
using Game.Core.Inventory;
using UnityEngine;

namespace Game.Unity.Props
{
    /// <summary>
    /// The display case's 5 × 5 grid and what goes in it. The case keeps no list of its own: every
    /// layout starts from Core's stacks, one slot per copy in <see cref="ItemLocation.DisplayCase"/>, in
    /// Core order. Pure, so it is tested without a scene.
    /// </summary>
    public static class DisplayCaseLayout
    {
        public const int Columns = 5;
        public const int Rows = 5;
        public const int SlotCount = Columns * Rows;

        /// <summary>
        /// A slot's centre in case space: X across, Z away from the vendor, Y up. Slot 0 is the far left
        /// corner as the vendor sees it, then left to right and row by row toward the vendor.
        /// </summary>
        /// <param name="cardSize">Card width (X) and height (laid along Z), in metres.</param>
        /// <param name="gap">Space between cards, in metres: X between columns, Y between rows.</param>
        /// <param name="lift">Height above the case floor, in metres.</param>
        public static Vector3 SlotPosition(int slot, Vector2 cardSize, Vector2 gap, float lift)
        {
            int column = slot % Columns;
            int row = slot / Columns;
            float pitchX = cardSize.x + gap.x;
            float pitchZ = cardSize.y + gap.y;
            float x = (column - (Columns - 1) * 0.5f) * pitchX;
            float z = ((Rows - 1) * 0.5f - row) * pitchZ;
            return new Vector3(x, lift, z);
        }

        /// <summary>The same gap between columns and rows.</summary>
        public static Vector3 SlotPosition(int slot, Vector2 cardSize, float gap, float lift)
        {
            return SlotPosition(slot, cardSize, new Vector2(gap, gap), lift);
        }

        /// <summary>
        /// Fills <paramref name="slots"/> (cleared first) with one entry per card copy in the display case,
        /// in Core order, up to <see cref="SlotCount"/>. Returns how many slots are filled.
        /// </summary>
        public static int Fill(IReadOnlyList<InventoryStack> stacks, List<ItemRef> slots)
        {
            slots.Clear();
            foreach (InventoryStack stack in stacks)
            {
                if (stack.Location != ItemLocation.DisplayCase)
                {
                    continue;
                }

                ItemRef card = ItemRef.Card(stack.CardId, stack.Tier);
                for (int copy = 0; copy < stack.Count && slots.Count < SlotCount; copy++)
                {
                    slots.Add(card);
                }
            }

            return slots.Count;
        }
    }
}
