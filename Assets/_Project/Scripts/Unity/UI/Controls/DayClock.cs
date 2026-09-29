using System.Globalization;
using Game.Core.Session;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// HUD day readout: one headline, "Day 1  10:00 PM", in the same HUD number style as the cash, with
    /// the day kind ("Prep Night" / "Show Day") as its own line beneath. On a Prep Night the clock is
    /// fixed at 10:00 PM; on a Show Day it shows the time pushed in with <see cref="SetTime"/> and, in
    /// the last hour, "Closing soon" after the headline.
    /// Display only: the day cycle and the show clock come from a presenter.
    /// </summary>
    [UxmlElement]
    public partial class DayClock : VisualElement
    {
        public const string ClassName = "day-clock";
        public const string LineClassName = ClassName + "__line";
        public const string HeadlineClassName = ClassName + "__headline";
        public const string KindClassName = ClassName + "__kind";
        public const string ClosingClassName = ClassName + "__closing";

        public const string PrepNightName = "Prep Night";
        public const string ShowDayName = "Show Day";
        public const string ClosingSoonText = "Closing soon";

        // GDD v1.5: prep happens at night with the clock fixed at 10:00 PM, so prep stays untimed.
        public const int PrepNightHour = 22;
        public const int PrepNightMinute = 0;

        private const int HoursPerDay = 24;
        private const int MinutesPerHour = 60;
        private const int HoursPerHalfDay = 12;

        private readonly Label _headline;
        private readonly Label _kind;
        private readonly Label _closing;

        private int _dayNumber = 1;
        private DayKind _dayKind = DayKind.PrepNight;
        private int _hour = PrepNightHour;
        private int _minute = PrepNightMinute;
        private bool _isClosingSoon;

        public DayClock()
        {
            AddToClassList(ClassName);

            // First line: the headline (same .num-hud role as the cash amount), then "Closing soon" when due.
            // Second line: the day kind.
            var line = new VisualElement();
            line.AddToClassList(LineClassName);
            _headline = AddLabel(line, KitClasses.NumberHud, HeadlineClassName);
            _closing = AddLabel(line, KitClasses.FontDisplayBold, ClosingClassName);
            _closing.text = ClosingSoonText;
            Add(line);

            _kind = AddLabel(this, KitClasses.TextSubheading, KindClassName);
            _kind.AddToClassList(KitClasses.TextMuted);
            Refresh();
        }

        public int Day => _dayNumber;

        public DayKind Kind => _dayKind;

        /// <summary>The first line, e.g. "Day 3  10:00 PM".</summary>
        public string HeadlineText => _headline.text;

        public string KindText => _kind.text;

        public bool IsClosingSoonShown => _closing.style.display != DisplayStyle.None;

        /// <summary>Shows a day. A Prep Night shows the fixed 10:00 PM clock; a Show Day shows the last time set.</summary>
        public void SetDay(int day, DayKind kind)
        {
            _dayNumber = day;
            _dayKind = kind;
            Refresh();
        }

        /// <summary>Sets the Show Day clock (24-hour input, shown as 12-hour). Ignored on a Prep Night.</summary>
        public void SetTime(int hour, int minute)
        {
            _hour = ((hour % HoursPerDay) + HoursPerDay) % HoursPerDay;
            _minute = ((minute % MinutesPerHour) + MinutesPerHour) % MinutesPerHour;
            Refresh();
        }

        /// <summary>Shows "Closing soon" beside the clock during a Show Day's last hour.</summary>
        public void SetClosingSoon(bool isClosingSoon)
        {
            _isClosingSoon = isClosingSoon;
            Refresh();
        }

        /// <summary>The headline for a day and a 24-hour time: day 3 at 22:00 → "Day 3  10:00 PM".</summary>
        public static string FormatHeadline(int day, int hour, int minute)
        {
            string time = FormatTime(hour, minute, out string meridiem);
            return string.Format(CultureInfo.InvariantCulture, "Day {0}  {1} {2}", day, time, meridiem);
        }

        /// <summary>Formats a 24-hour time as 12-hour clock text and "AM"/"PM": 13:05 → "1:05", "PM".</summary>
        public static string FormatTime(int hour, int minute, out string meridiem)
        {
            meridiem = hour < HoursPerHalfDay ? "AM" : "PM";
            int displayHour = hour % HoursPerHalfDay;
            if (displayHour == 0)
            {
                displayHour = HoursPerHalfDay;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", displayHour, minute);
        }

        private void Refresh()
        {
            bool isPrepNight = _dayKind == DayKind.PrepNight;
            int hour = isPrepNight ? PrepNightHour : _hour;
            int minute = isPrepNight ? PrepNightMinute : _minute;
            _headline.text = FormatHeadline(_dayNumber, hour, minute);
            _kind.text = isPrepNight ? PrepNightName : ShowDayName;

            _closing.style.display = !isPrepNight && _isClosingSoon ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static Label AddLabel(VisualElement parent, string typographyClass, string className)
        {
            var label = new Label();
            label.AddToClassList(typographyClass);
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }
    }
}
