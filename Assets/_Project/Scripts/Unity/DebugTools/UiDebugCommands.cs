using System;
using System.IO;
using static System.FormattableString;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// UI check commands (STORE_MARKUP_README §6). <c>ui.capture &lt;name&gt;</c> saves the Game view to
    /// Docs/UI/Captures/&lt;name&gt;.png so it can be compared with the mockups, and warns when the view
    /// isn't the 1920×1080 reference. The capture itself is injected (ScreenCapture in the editor host),
    /// so the command is plain C# like the others.
    /// </summary>
    public sealed class UiDebugCommands
    {
        public const string Prefix = "ui.";

        public const string HelpText =
            "UI commands (Play Mode):\n" +
            "  ui.capture <name>                save the Game view to Docs/UI/Captures/<name>.png (compare at 1920×1080)";

        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        private readonly string _folder;
        private readonly Action<string> _capture;
        private readonly int _width;
        private readonly int _height;

        /// <param name="capture">Saves the current Game view to the given path.</param>
        /// <param name="width">Current Game view width in pixels.</param>
        /// <param name="height">Current Game view height in pixels.</param>
        public UiDebugCommands(string folder, Action<string> capture, int width, int height)
        {
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _width = width;
            _height = height;
        }

        public static bool Handles(string commandLine)
        {
            return commandLine != null && commandLine.TrimStart().StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public string Execute(string commandLine)
        {
            string[] parts = (commandLine ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !string.Equals(parts[0], "ui.capture", StringComparison.OrdinalIgnoreCase))
            {
                return $"Unknown UI command '{commandLine}'.\n{HelpText}";
            }

            if (parts.Length != 2 || !IsSafeName(parts[1]))
            {
                return "Usage: ui.capture <name>, where name uses letters, digits, '-' and '_' only.";
            }

            string path = Path.Combine(_folder, parts[1] + ".png");
            _capture(path);
            string size = Invariant($"{_width}×{_height}");
            return _width == ReferenceWidth && _height == ReferenceHeight
                ? $"Saved {path} ({size})."
                : $"Saved {path} at {size}, not {ReferenceWidth}×{ReferenceHeight}: set the Game view to 1920×1080 before comparing with the mockups.";
        }

        private static bool IsSafeName(string name)
        {
            foreach (char character in name)
            {
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                {
                    return false;
                }
            }

            return name.Length > 0;
        }
    }
}
