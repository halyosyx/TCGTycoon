using Game.Core.Content;

namespace Game.Unity.UI.Collection
{
    /// <summary>One line of the inventory screen: a stack's card name, tier and count.</summary>
    public readonly struct InventoryRow
    {
        public InventoryRow(string cardName, RarityTier tier, int count)
        {
            CardName = cardName;
            Tier = tier;
            Count = count;
        }

        public string CardName { get; }

        public RarityTier Tier { get; }

        public int Count { get; }
    }
}
