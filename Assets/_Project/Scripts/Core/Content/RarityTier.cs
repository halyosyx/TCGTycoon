namespace Game.Core.Content
{
    /// <summary>
    /// Card rarity, ordered from most common to rarest. "Tier X or better" comparisons rely on this
    /// order, and the values must stay contiguous from 0 because tallies index arrays by tier.
    /// </summary>
    public enum RarityTier
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Holographic = 3,
        FullArt = 4,
        AlternateIllustration = 5,
        SpecialIllustration = 6,
    }
}
