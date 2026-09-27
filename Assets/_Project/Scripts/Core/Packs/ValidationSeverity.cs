namespace Game.Core.Packs
{
    /// <summary>How serious a configuration problem is.</summary>
    public enum ValidationSeverity
    {
        /// <summary>Allowed, but probably a mistake.</summary>
        Warning = 0,

        /// <summary>The pack can't be opened until this is fixed.</summary>
        Error = 1,
    }
}
