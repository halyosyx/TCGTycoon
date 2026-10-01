namespace Game.Core.Content
{
    /// <summary>
    /// Card rarity, ordered from most common to rarest (GDD v1.7: four tiers). "Tier X or better"
    /// comparisons rely on this order.
    /// <para>
    /// Values are stored as integers in assets, so they are never reused: 2 to 6 belonged to the
    /// seven-tier ladder (Rare, Holographic, Full Art, Alternate Illustration, Special Illustration)
    /// and stay retired, which makes any stale reference an undefined tier that validation reports
    /// instead of silently reading as a new tier. Values are not contiguous: index arrays with
    /// <see cref="RarityTiers.IndexOf"/>, never by casting.
    /// </para>
    /// </summary>
    public enum RarityTier
    {
        Common = 0,
        Uncommon = 1,
        HoloFullArt = 7,
        SpecialFullArtHolo = 8,
    }
}
