namespace Game.Core.Inventory
{
    /// <summary>
    /// Result of moving every single card from one location to another
    /// (<see cref="InventoryService.MoveAllCards"/>): how many moved, how many stayed where they were, and
    /// why the rest stayed. Nothing is ever dropped.
    /// </summary>
    public readonly struct CardsMoveResult
    {
        public CardsMoveResult(int moved, int left, MoveFailure failure)
        {
            Moved = moved;
            Left = left;
            Failure = failure;
        }

        public int Moved { get; }

        /// <summary>Cards still at the source, because there was no room or they weren't allowed.</summary>
        public int Left { get; }

        /// <summary><see cref="MoveFailure.None"/> when everything moved.</summary>
        public MoveFailure Failure { get; }

        public bool IsComplete => Moved > 0 && Left == 0;
    }
}
