using System;
using System.Globalization;
using Game.Core.Common;
using Game.Core.Economy;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Play Mode commands on the running session's cash. They go through <see cref="EconomyService"/>
    /// like any feature (reason Debug, so the ledger shows them), and the HUD follows its event.
    /// Plain C#, like <see cref="PackDebugCommands"/>.
    /// </summary>
    public sealed class EconomyDebugCommands
    {
        public const string Prefix = "money.";

        public const string HelpText =
            "Money commands (Play Mode, on the scene's session):\n" +
            "  money.set <cents>                set cash (0 or more)\n" +
            "  money.add <cents>                add cash; a negative amount takes it (never below 0)";

        private const string DebugItemId = "debug";

        private readonly EconomyService _economy;

        public EconomyDebugCommands(EconomyService economy)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        /// <summary>True for command lines this class runs (they start with "money.").</summary>
        public static bool Handles(string commandLine)
        {
            return commandLine != null && commandLine.TrimStart().StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public string Execute(string commandLine)
        {
            string[] parts = (commandLine ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            switch (parts[0].ToLowerInvariant())
            {
                case "money.set":
                    return parts.Length == 2 && TryCents(parts[1], out long target) && target >= 0
                        ? Change(target - _economy.BalanceCents)
                        : "Usage: money.set <cents>, where cents is 0 or more.";
                case "money.add":
                    return parts.Length == 2 && TryCents(parts[1], out long delta) && delta != 0
                        ? Change(delta)
                        : "Usage: money.add <cents>, where cents isn't 0 (negative takes cash).";
                default:
                    return $"Unknown money command '{parts[0]}'.\n{HelpText}";
            }
        }

        private string Change(long deltaCents)
        {
            if (deltaCents > 0)
            {
                _economy.Credit(deltaCents, TransactionReason.Debug, DebugItemId);
            }
            else if (deltaCents < 0 && !_economy.TryDebit(-deltaCents, TransactionReason.Debug, DebugItemId))
            {
                return $"Can't take {Money.FormatDisplay(-deltaCents)}: cash is {Money.FormatDisplay(_economy.BalanceCents)}.";
            }

            return $"{Money.FormatDelta(deltaCents)}; cash {Money.FormatDisplay(_economy.BalanceCents)}.";
        }

        private static bool TryCents(string text, out long cents)
        {
            return long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out cents);
        }
    }
}
