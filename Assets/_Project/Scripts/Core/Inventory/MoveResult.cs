namespace Game.Core.Inventory
{
    /// <summary>What <see cref="InventoryService.Move"/> did: success with the count moved, or why it refused.</summary>
    public readonly struct MoveResult
    {
        private MoveResult(MoveFailure failure, int count)
        {
            Failure = failure;
            Count = count;
        }

        public bool IsSuccess => Failure == MoveFailure.None;

        public MoveFailure Failure { get; }

        /// <summary>Copies moved; 0 on failure.</summary>
        public int Count { get; }

        public static MoveResult Moved(int count) => new MoveResult(MoveFailure.None, count);

        public static MoveResult Refused(MoveFailure failure) => new MoveResult(failure, 0);

        public override string ToString() => IsSuccess ? $"Moved {Count}" : $"Refused: {Failure}";
    }
}
