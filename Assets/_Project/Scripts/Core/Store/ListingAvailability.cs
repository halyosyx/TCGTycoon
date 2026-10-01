namespace Game.Core.Store
{
    /// <summary>Whether a store listing can be bought, is shown as coming soon, or isn't shown.</summary>
    public enum ListingAvailability
    {
        Available = 0,
        ComingSoon = 1,
        Hidden = 2,
    }
}
