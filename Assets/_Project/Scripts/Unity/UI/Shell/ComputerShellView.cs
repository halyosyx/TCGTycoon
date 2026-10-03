using System;
using Game.Unity.UI.Controls;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Shell
{
    /// <summary>
    /// The home computer's frame (ComputerShell.uxml): browser tab and address, taskbar clock, and a kit
    /// <see cref="KeyHints"/> in the key-hints slot. Every in-game app opens inside it; the app's own
    /// tree goes into <see cref="ContentSlot"/>. Holds no game state.
    /// </summary>
    public sealed class ComputerShellView
    {
        private readonly Label _tabTitle;
        private readonly Label _address;
        private readonly Label _time;
        private readonly Label _day;
        private readonly KeyHints _hints;

        /// <param name="shell">An instance of ComputerShell.uxml.</param>
        public ComputerShellView(VisualElement shell)
        {
            Root = shell ?? throw new ArgumentNullException(nameof(shell));
            ContentSlot = shell.Q<VisualElement>("content-slot");
            _tabTitle = shell.Q<Label>("tab-title");
            _address = shell.Q<Label>("address-text");
            _time = shell.Q<Label>("taskbar-time");
            _day = shell.Q<Label>("taskbar-day");
            // The kit's key hints align to the start of their parent; the slot centres them (mockups).
            _hints = new KeyHints();
            _hints.style.alignSelf = Align.Center;
            shell.Q<VisualElement>("key-hints-slot").Add(_hints);
        }

        public VisualElement Root { get; }

        public VisualElement ContentSlot { get; }

        public void SetPage(string tabTitle, string address)
        {
            _tabTitle.text = tabTitle;
            _address.text = address;
        }

        public void SetClock(string time, string day)
        {
            _time.text = time;
            _day.text = day;
        }

        /// <summary>Replaces the key hints ("Key:Label;..." as in <see cref="KeyHints.Hints"/>).</summary>
        public void SetHints(string hints) => _hints.Hints = hints;
    }
}
