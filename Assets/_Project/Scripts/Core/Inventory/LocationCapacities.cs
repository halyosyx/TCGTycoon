using System;

namespace Game.Core.Inventory
{
    /// <summary>
    /// How much each limited location holds. Binder and Placed are unlimited. Immutable; pass a custom
    /// one to <see cref="InventoryService"/> to tune it (defaults: 10 cards or 1 pack in hand, 25 in the
    /// display case).
    /// </summary>
    public sealed class LocationCapacities
    {
        public const int DefaultHeldCards = 10;
        public const int DefaultHeldSealed = 1;
        public const int DefaultDisplayCase = 25;

        public static readonly LocationCapacities Default = new LocationCapacities(DefaultHeldCards, DefaultHeldSealed, DefaultDisplayCase);

        public LocationCapacities(int heldCards, int heldSealed, int displayCase)
        {
            if (heldCards < 0) throw new ArgumentOutOfRangeException(nameof(heldCards));
            if (heldSealed < 0) throw new ArgumentOutOfRangeException(nameof(heldSealed));
            if (displayCase < 0) throw new ArgumentOutOfRangeException(nameof(displayCase));

            HeldCards = heldCards;
            HeldSealed = heldSealed;
            DisplayCase = displayCase;
        }

        /// <summary>Single cards the hand holds (when it holds no sealed product).</summary>
        public int HeldCards { get; }

        /// <summary>Sealed units the hand holds (when it holds no cards).</summary>
        public int HeldSealed { get; }

        /// <summary>Single cards the display case holds.</summary>
        public int DisplayCase { get; }
    }
}
