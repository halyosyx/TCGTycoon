namespace Game.Unity.UI.PackOpening
{
    /// <summary>Moments of the rip that get a sound, in the order they happen.</summary>
    public enum PackTearCue
    {
        /// <summary>The back seam starts to tear (the rip click).</summary>
        SeamTear,

        /// <summary>The back flaps start to swing open.</summary>
        WrapperOpen,

        /// <summary>The cards start sliding out.</summary>
        CardsSlide,
    }
}
