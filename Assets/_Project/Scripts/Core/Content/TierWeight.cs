namespace Game.Core.Content
{
    /// <summary>
    /// One entry in a pack slot: a rarity tier and its integer weight. Weights are relative, so a slot's
    /// weights never need to sum to 100. Invalid values (negative weights) are representable so the
    /// validator can report them.
    /// </summary>
    public readonly struct TierWeight
    {
        public TierWeight(RarityTier tier, int weight)
        {
            Tier = tier;
            Weight = weight;
        }

        public RarityTier Tier { get; }

        public int Weight { get; }
    }
}
