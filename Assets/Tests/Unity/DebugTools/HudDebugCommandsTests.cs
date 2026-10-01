using Game.Core.Session;
using Game.Unity.DebugTools;
using Game.Unity.UI.Controls;
using Game.Unity.UI.Hud;
using NUnit.Framework;

namespace Game.Unity.Tests.DebugTools
{
    public sealed class HudDebugCommandsTests
    {
        [TestCase("hud.prepnight 2", true)]
        [TestCase("  HUD.toast hi", true)]
        [TestCase("open 5", false)]
        [TestCase(null, false)]
        public void Handles_CommandLine_OnlyHudPrefix(string commandLine, bool expected)
        {
            Assert.That(HudDebugCommands.Handles(commandLine), Is.EqualTo(expected));
        }

        [Test]
        public void Execute_PrepNightWithoutDay_KeepsCurrentDay()
        {
            var hud = new FakeHud { Day = 4 };

            new HudDebugCommands(hud).Execute("hud.prepnight");

            Assert.That(hud.LastDay, Is.EqualTo(4));
            Assert.That(hud.LastKind, Is.EqualTo(DayKind.PrepNight));
        }

        [Test]
        public void Execute_ShowDay_SetsDayKindTimeAndClearsClosing()
        {
            var hud = new FakeHud { Day = 2, IsClosingSoon = true };

            new HudDebugCommands(hud).Execute("hud.showday 16 45 3");

            Assert.That(hud.LastDay, Is.EqualTo(3));
            Assert.That(hud.LastKind, Is.EqualTo(DayKind.ShowDay));
            Assert.That(hud.Hour, Is.EqualTo(16));
            Assert.That(hud.Minute, Is.EqualTo(45));
            Assert.That(hud.IsClosingSoon, Is.False);
        }

        [TestCase("hud.showday 24 00")]
        [TestCase("hud.showday 10 60")]
        [TestCase("hud.showday 10")]
        public void Execute_ShowDayOutOfRange_ChangesNothing(string commandLine)
        {
            var hud = new FakeHud();

            string output = new HudDebugCommands(hud).Execute(commandLine);

            Assert.That(output, Does.StartWith("Usage"));
            Assert.That(hud.LastKind, Is.Null);
        }

        [TestCase("hud.sale 1400")]
        [TestCase("hud.buy 15000 Booster Box")]
        public void Execute_RemovedCashCommand_IsUnknown(string commandLine)
        {
            // Cash changes only through EconomyService now (money.add / money.set / buy).
            string output = new HudDebugCommands(new FakeHud()).Execute(commandLine);

            Assert.That(output, Does.StartWith("Unknown HUD command"));
        }

        [Test]
        public void Execute_Toast_ShowsNeutralToastWithAllWords()
        {
            var hud = new FakeHud();

            new HudDebugCommands(hud).Execute("hud.toast Pack stored in binder");

            Assert.That(hud.ToastTitle, Is.EqualTo("Pack stored in binder"));
            Assert.That(hud.LastToastKind, Is.EqualTo(ToastKind.Neutral));
        }

        [Test]
        public void Execute_ClosingOff_HidesClosingSoon()
        {
            var hud = new FakeHud { IsClosingSoon = true };

            new HudDebugCommands(hud).Execute("hud.closing off");

            Assert.That(hud.IsClosingSoon, Is.False);
        }

        private sealed class FakeHud : IHudSink
        {
            public int Day { get; set; } = 1;
            public int? LastDay { get; private set; }
            public DayKind? LastKind { get; private set; }
            public int Hour { get; private set; }
            public int Minute { get; private set; }
            public bool IsClosingSoon { get; set; }
            public string ToastTitle { get; private set; }
            public ToastKind? LastToastKind { get; private set; }

            public void ShowDay(int day, DayKind kind)
            {
                Day = day;
                LastDay = day;
                LastKind = kind;
            }

            public void SetTime(int hour, int minute)
            {
                Hour = hour;
                Minute = minute;
            }

            public void SetClosingSoon(bool isClosingSoon) => IsClosingSoon = isClosingSoon;

            public void ShowToast(ToastKind kind, string title, string detail, long? amountCents)
            {
                LastToastKind = kind;
                ToastTitle = title;
            }
        }
    }
}
