namespace Game.Core.Content
{
    /// <summary>Whether a card set is still printed (cheap, always in stock) or out of print (scarce, appreciating).</summary>
    public enum SetLifecycle
    {
        InPrint = 0,
        OutOfPrint = 1,
    }
}
