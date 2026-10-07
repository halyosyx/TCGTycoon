namespace Game.Unity.UI.PackOpening
{
    /// <summary>The reaction a revealed card gets, from its tier's <see cref="TierTell"/>.</summary>
    public enum TierTellKind
    {
        /// <summary>Nothing (Common and Uncommon).</summary>
        None,

        /// <summary>One quick, card-local flash (Holographic Full Art).</summary>
        Flash,

        /// <summary>The flash plus a single burst of sparkles (Special Full Art Holo only).</summary>
        FlashAndSparkle,
    }
}
