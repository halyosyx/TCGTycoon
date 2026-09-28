namespace Game.Unity.UI.PackOpening
{
    /// <summary>Where the pack opening screen is in its flow.</summary>
    public enum PackRevealState
    {
        /// <summary>No pack on screen.</summary>
        Idle,

        /// <summary>Cards are shown as a stack and revealed one at a time.</summary>
        Revealing,

        /// <summary>Every card is face up in a row.</summary>
        Row,

        /// <summary>One card from the row is shown large in the centre for inspection.</summary>
        Showcase,
    }
}
