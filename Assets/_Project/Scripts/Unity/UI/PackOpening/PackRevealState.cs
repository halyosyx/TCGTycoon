namespace Game.Unity.UI.PackOpening
{
    /// <summary>Where the pack opening screen is in its flow.</summary>
    public enum PackRevealState
    {
        /// <summary>No pack on screen.</summary>
        Idle,

        /// <summary>
        /// The held pack is moving to the centre of the view, back turned to the player, and waits for the
        /// rip click. Nothing is committed: backing out leaves the pack in the hand.
        /// </summary>
        Zooming,

        /// <summary>
        /// The back seam is tearing and the wrapper opening. The cards are already owned: they were
        /// committed by the rip click, before this started.
        /// </summary>
        Ripping,

        /// <summary>Cards are shown face up as a stack and swiped away one at a time.</summary>
        Revealing,

        /// <summary>Every card is face up in a row.</summary>
        Row,

        /// <summary>One card from the row is shown large in the centre for inspection.</summary>
        Showcase,
    }
}
