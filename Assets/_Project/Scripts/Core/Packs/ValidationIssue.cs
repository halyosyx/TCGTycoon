namespace Game.Core.Packs
{
    /// <summary>A problem found in a pack configuration or its card pool.</summary>
    public sealed class ValidationIssue
    {
        /// <summary><see cref="SlotIndex"/> value for issues that aren't about one slot.</summary>
        public const int PackLevel = -1;

        public ValidationIssue(ValidationSeverity severity, int slotIndex, string message)
        {
            Severity = severity;
            SlotIndex = slotIndex;
            Message = message ?? string.Empty;
        }

        public ValidationSeverity Severity { get; }

        /// <summary>Zero-based slot index, or <see cref="PackLevel"/>.</summary>
        public int SlotIndex { get; }

        public string Message { get; }

        public override string ToString()
        {
            return SlotIndex == PackLevel
                ? $"{Severity}: {Message}"
                : $"{Severity}: Slot {SlotIndex + 1}: {Message}";
        }
    }
}
