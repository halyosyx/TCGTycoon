using Game.Core.Session;
using Game.Unity.UI.Controls;
using NUnit.Framework;

namespace Game.Unity.Tests.UI.Controls
{
    public sealed class HudControlTests
    {
        private const string Minus = "−";

        [Test]
        public void DayClock_PrepNight_ShowsFixedTenPmAndTaglineWhateverTimeIsSet()
        {
            var clock = new DayClock();
            clock.SetTime(14, 30);

            clock.SetDay(3, DayKind.PrepNight);

            Assert.That(clock.DayText, Is.EqualTo("Day 3"));
            Assert.That(clock.KindText, Is.EqualTo("Prep Night"));
            Assert.That(clock.TimeText, Is.EqualTo("10:00"));
            Assert.That(clock.MeridiemText, Is.EqualTo("PM"));
            Assert.That(clock.IsTaglineShown, Is.True);
        }

        [Test]
        public void DayClock_ShowDay_ShowsSetTimeWithoutTagline()
        {
            var clock = new DayClock();

            clock.SetDay(4, DayKind.ShowDay);
            clock.SetTime(13, 5);

            Assert.That(clock.KindText, Is.EqualTo("Show Day"));
            Assert.That(clock.TimeText, Is.EqualTo("1:05"));
            Assert.That(clock.MeridiemText, Is.EqualTo("PM"));
            Assert.That(clock.IsTaglineShown, Is.False);
        }

        [TestCase(0, 0, "12:00", "AM")]
        [TestCase(9, 30, "9:30", "AM")]
        [TestCase(12, 0, "12:00", "PM")]
        [TestCase(23, 59, "11:59", "PM")]
        public void DayClock_FormatTime_TwentyFourHourInput_ShowsTwelveHourClock(int hour, int minute, string expectedTime, string expectedMeridiem)
        {
            string time = DayClock.FormatTime(hour, minute, out string meridiem);

            Assert.That(time, Is.EqualTo(expectedTime));
            Assert.That(meridiem, Is.EqualTo(expectedMeridiem));
        }

        [Test]
        public void DayClock_ClosingSoon_ShowsOnlyOnAShowDay()
        {
            var clock = new DayClock();
            clock.SetClosingSoon(true);

            clock.SetDay(1, DayKind.PrepNight);
            Assert.That(clock.IsClosingSoonShown, Is.False);

            clock.SetDay(1, DayKind.ShowDay);
            Assert.That(clock.IsClosingSoonShown, Is.True);

            clock.SetClosingSoon(false);
            Assert.That(clock.IsClosingSoonShown, Is.False);
        }

        [Test]
        public void CashReadout_SetAmount_GroupsThousandsWithoutLabel()
        {
            var cash = new CashReadout();

            cash.SetAmount(128450);

            Assert.That(cash.AmountText, Is.EqualTo("$1,284.50"));
            Assert.That(cash.IsEventShown, Is.False);
        }

        [Test]
        public void CashReadout_ShowEvent_Sale_ShowsPositiveSignedDelta()
        {
            var cash = new CashReadout();

            cash.ShowEvent("Sale", 1400);

            Assert.That(cash.IsEventShown, Is.True);
            Assert.That(cash.EventLabelText, Is.EqualTo("Sale"));
            Assert.That(cash.EventDelta.Text, Is.EqualTo("+$14.00"));
            Assert.That(cash.EventDelta.ClassListContains(SignedAmount.PositiveClassName), Is.True);
        }

        [Test]
        public void CashReadout_ShowEvent_Purchase_ShowsRealMinusAndNegativeClass()
        {
            var cash = new CashReadout();

            cash.ShowEvent("Booster Box", -15000);

            Assert.That(cash.EventDelta.Text, Is.EqualTo(Minus + "$150.00"));
            Assert.That(cash.EventDelta.ClassListContains(SignedAmount.NegativeClassName), Is.True);
            Assert.That(cash.EventDelta.ClassListContains(SignedAmount.PositiveClassName), Is.False);
        }

        [Test]
        public void ToastStack_Show_NewestFirstAndCappedAtMax()
        {
            var stack = new ToastStack { MaxToasts = 2 };

            stack.Show(ToastKind.Neutral, "First", string.Empty, null);
            stack.Show(ToastKind.Positive, "Second", "Detail", 1400);
            stack.Show(ToastKind.Neutral, "Third", string.Empty, null);

            Assert.That(stack.Toasts.Count, Is.EqualTo(2));
            Assert.That(stack.Toasts[0].TitleText, Is.EqualTo("Third"));
            Assert.That(stack.Toasts[1].TitleText, Is.EqualTo("Second"));
            Assert.That(stack.childCount, Is.EqualTo(2));
        }

        [Test]
        public void Toast_Set_PositiveWithAmount_ShowsAmountAndPositiveClass()
        {
            var toast = new Toast();

            toast.Set(ToastKind.Positive, "Sold", "Coralin", 25000);

            Assert.That(toast.ClassListContains(Toast.PositiveClassName), Is.True);
            Assert.That(toast.HasAmount, Is.True);
            Assert.That(toast.Amount.Text, Is.EqualTo("+$250.00"));
        }

        [Test]
        public void Toast_Set_NoAmount_HidesAmount()
        {
            var toast = new Toast();

            toast.Set(ToastKind.Neutral, "Saved", string.Empty, null);

            Assert.That(toast.HasAmount, Is.False);
            Assert.That(toast.ClassListContains(Toast.PositiveClassName), Is.False);
        }

        [Test]
        public void Crosshair_Usable_TogglesUsableClass()
        {
            var crosshair = new Crosshair { Usable = true };
            Assert.That(crosshair.ClassListContains(Crosshair.UsableClassName), Is.True);

            crosshair.Usable = false;

            Assert.That(crosshair.ClassListContains(Crosshair.UsableClassName), Is.False);
        }

        [Test]
        public void InteractionPrompt_ShowThenHide_TracksTextAndVisibility()
        {
            var prompt = new InteractionPrompt();
            Assert.That(prompt.IsShown, Is.False);

            prompt.Show("LMB", "Open", "Booster pack");
            Assert.That(prompt.IsShown, Is.True);
            Assert.That(prompt.KeyText, Is.EqualTo("LMB"));
            Assert.That(prompt.VerbText, Is.EqualTo("Open"));
            Assert.That(prompt.ObjectText, Is.EqualTo("Booster pack"));

            prompt.Hide();
            Assert.That(prompt.IsShown, Is.False);
        }
    }
}
