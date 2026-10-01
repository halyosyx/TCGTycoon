namespace Game.Core.Content
{
    /// <summary>
    /// How strongly a card's market price swings day to day (GDD v1.7: Common and Uncommon Low,
    /// Holographic Full Art Medium, Special Full Art Holo High). Read by the market from F4.
    /// </summary>
    public enum VolatilityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }
}
