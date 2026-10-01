namespace Game.Unity.UI.PackOpening
{
    /// <summary>How the revealed cards lay out once the stack is done.</summary>
    public enum RowArrangement
    {
        /// <summary>Every card on one line, in slot order.</summary>
        SingleRow = 0,

        /// <summary>Two lines: the first half (rounded up) on top, the rest below, each line centred.</summary>
        TwoRows = 1,
    }
}
