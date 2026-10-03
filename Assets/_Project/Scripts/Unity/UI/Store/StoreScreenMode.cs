namespace Game.Unity.UI.Store
{
    /// <summary>The store screen's states (STR-29): closed, then Browsing → CartOpen → OrderPlaced.</summary>
    public enum StoreScreenMode
    {
        Closed = 0,
        Browsing = 1,
        CartOpen = 2,
        OrderPlaced = 3,
    }
}
