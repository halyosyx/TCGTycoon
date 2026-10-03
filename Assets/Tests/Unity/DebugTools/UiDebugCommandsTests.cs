using System.Collections.Generic;
using System.IO;
using Game.Unity.DebugTools;
using NUnit.Framework;

namespace Game.Unity.Tests.DebugTools
{
    public sealed class UiDebugCommandsTests
    {
        private const string Folder = "C:/Captures";

        [TestCase("ui.capture browse", true)]
        [TestCase(" UI.capture x", true)]
        [TestCase("store.open", false)]
        [TestCase(null, false)]
        public void Handles_CommandLine_OnlyUiPrefix(string commandLine, bool expected)
        {
            Assert.That(UiDebugCommands.Handles(commandLine), Is.EqualTo(expected));
        }

        [Test]
        public void Execute_Capture_SavesNamedPngInTheCaptureFolder()
        {
            var saved = new List<string>();
            var commands = new UiDebugCommands(Folder, saved.Add, 1920, 1080);

            string output = commands.Execute("ui.capture store_browse");

            Assert.That(saved, Is.EqualTo(new[] { Path.Combine(Folder, "store_browse.png") }));
            Assert.That(output, Does.Contain("store_browse.png").And.Not.Contain("not 1920"));
        }

        [Test]
        public void Execute_CaptureAtOtherSize_Warns()
        {
            var saved = new List<string>();

            string output = new UiDebugCommands(Folder, saved.Add, 1280, 720).Execute("ui.capture small");

            Assert.That(saved.Count, Is.EqualTo(1));
            Assert.That(output, Does.Contain("1280×720").And.Contain("not 1920×1080"));
        }

        [TestCase("ui.capture")]
        [TestCase("ui.capture ../escape")]
        [TestCase("ui.capture two words")]
        [TestCase("ui.capture a/b")]
        public void Execute_BadName_UsageAndNoCapture(string commandLine)
        {
            var saved = new List<string>();

            string output = new UiDebugCommands(Folder, saved.Add, 1920, 1080).Execute(commandLine);

            Assert.That(output, Does.StartWith("Usage"));
            Assert.That(saved, Is.Empty);
        }
    }
}
