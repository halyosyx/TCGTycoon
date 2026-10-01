namespace Game.Core.Store
{
    /// <summary>
    /// Whether the cart can be ordered. Drives styling only: the store UI never turns it into an
    /// explanatory sentence (STORE_UI_REQUIREMENTS STR-11, STR-24).
    /// </summary>
    public enum OrderCheck
    {
        Ok = 0,
        Empty = 1,
        OverBalance = 2,
        OutOfStock = 3,
    }
}
