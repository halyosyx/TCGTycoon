using System;
using System.Globalization;
using Game.Core.Common;
using Game.Core.Session;
using Game.Unity.UI.Controls;
using Game.Unity.UI.Hud;
using static System.FormattableString;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Text commands that push every HUD state for checking the layout before Core feeds it (the day
    /// cycle and EconomyService arrive in F2). Plain C#, like <see cref="PackDebugCommands"/>, so the
    /// editor console and a future in-game console can host the same commands. Cash changes here are
    /// display only: no economy rules exist yet.
    /// </summary>
    public sealed class HudDebugCommands
    {
        public const string Prefix = "hud.";

        public const string HelpText =
            "HUD commands (Play Mode; display only, no economy yet):\n" +
            "  hud.prepnight [day]              Prep Night: clock fixed at 10:00 PM\n" +
            "  hud.showday <hour> <min> [day]   Show Day at a 24-hour time\n" +
            "  hud.closing [off]                show (or hide) \"Closing soon\" on a Show Day\n" +
            "  hud.sale <cents>                 cash goes up, with a Sale event line\n" +
            "  hud.buy <cents> <label>          cash goes down, with an event line naming the item\n" +
            "  hud.toast <text>                 show a toast";

        private readonly IHudSink _hud;

        public HudDebugCommands(IHudSink hud)
        {
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
        }

        /// <summary>True for command lines this class runs (they start with "hud.").</summary>
        public static bool Handles(string commandLine)
        {
            return commandLine != null && commandLine.TrimStart().StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Runs one command line and returns its output.</summary>
        public string Execute(string commandLine)
        {
            string[] parts = (commandLine ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            switch (parts[0].ToLowerInvariant())
            {
                case "hud.prepnight":
                    return PrepNight(parts);
                case "hud.showday":
                    return ShowDay(parts);
                case "hud.closing":
                    return Closing(parts);
                case "hud.sale":
                    return Sale(parts);
                case "hud.buy":
                    return Buy(parts);
                case "hud.toast":
                    return Toast(parts);
                default:
                    return $"Unknown HUD command '{parts[0]}'.\n{HelpText}";
            }
        }

        private string PrepNight(string[] parts)
        {
            if (!TryDay(parts, 1, out int day))
            {
                return "Usage: hud.prepnight [day], where day is 1 or more.";
            }

            _hud.ShowDay(day, DayKind.PrepNight);
            return Invariant($"Day {day}, Prep Night.");
        }

        private string ShowDay(string[] parts)
        {
            if (parts.Length < 3
                || !TryInt(parts[1], 0, 23, out int hour)
                || !TryInt(parts[2], 0, 59, out int minute)
                || !TryDay(parts, 3, out int day))
            {
                return "Usage: hud.showday <hour 0-23> <minute 0-59> [day].";
            }

            _hud.ShowDay(day, DayKind.ShowDay);
            _hud.SetTime(hour, minute);
            _hud.SetClosingSoon(false);
            return Invariant($"Day {day}, Show Day, {hour:00}:{minute:00}.");
        }

        private string Closing(string[] parts)
        {
            bool isOn = parts.Length < 2 || !string.Equals(parts[1], "off", StringComparison.OrdinalIgnoreCase);
            _hud.SetClosingSoon(isOn);
            return isOn ? "\"Closing soon\" on (shows on a Show Day)." : "\"Closing soon\" off.";
        }

        private string Sale(string[] parts)
        {
            if (parts.Length != 2 || !TryCents(parts[1], out long cents))
            {
                return "Usage: hud.sale <cents>, where cents is more than 0.";
            }

            _hud.SetCash(_hud.CashCents + cents);
            _hud.ShowCashEvent("Sale", cents);
            return $"Sale {Money.FormatDelta(cents)}; cash {Money.FormatDisplay(_hud.CashCents)}.";
        }

        private string Buy(string[] parts)
        {
            if (parts.Length < 3 || !TryCents(parts[1], out long cents))
            {
                return "Usage: hud.buy <cents> <label>, where cents is more than 0.";
            }

            string label = string.Join(" ", parts, 2, parts.Length - 2);
            _hud.SetCash(_hud.CashCents - cents);
            _hud.ShowCashEvent(label, -cents);
            return $"{label} {Money.FormatDelta(-cents)}; cash {Money.FormatDisplay(_hud.CashCents)}.";
        }

        private string Toast(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "Usage: hud.toast <text>.";
            }

            string text = string.Join(" ", parts, 1, parts.Length - 1);
            _hud.ShowToast(ToastKind.Neutral, text, string.Empty, null);
            return $"Toast: {text}";
        }

        private bool TryDay(string[] parts, int index, out int day)
        {
            if (parts.Length <= index)
            {
                day = _hud.Day;
                return true;
            }

            return TryInt(parts[index], 1, int.MaxValue, out day);
        }

        private static bool TryInt(string text, int minimum, int maximum, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= minimum && value <= maximum;
        }

        private static bool TryCents(string text, out long cents)
        {
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out cents) && cents > 0;
        }
    }
}
