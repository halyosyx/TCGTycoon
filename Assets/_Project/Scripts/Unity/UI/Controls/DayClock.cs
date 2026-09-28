using System.Globalization;
using Game.Core.Session;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// HUD day readout (style guide §7): "Day {n}" with the day kind, then the clock. On a Prep
    /// Night the clock is fixed at 10:00 PM with the "It's Investin' time" line; on a Show Day it shows
    /// the time pushed in with <see cref="SetTime"/> and, in the last hour, "Closing soon".
    /// Display only: the day cycle and the show clock come from a presenter.
    /// </summary>
    [UxmlElement]
    public partial class DayClock : VisualElement
    {
        public const string ClassName = "day-clock";
        public const string HeadingClassName = ClassName + "__heading";
        public const string DayClassName = ClassName + "__day";
        public const string KindClassName = ClassName + "__kind";
        public const string ClockClassName = ClassName + "__clock";
        public const string TimeClassName = ClassName + "__time";
        public const string MeridiemClassName = ClassName + "__meridiem";
        public const string ClosingClassName = ClassName + "__closing";
        public const string TaglineClassName = ClassName + "__tagline";

        public const string PrepNightName = "Prep Night";
        public const string ShowDayName = "Show Day";
        public const string PrepNightTagline = "It's Investin' time";
        public const string ClosingSoonText = "Closing soon";

        // GDD v1.5: prep happens at night with the clock fixed at 10:00 PM, so prep stays untimed.
        public const int PrepNightHour = 22;
        public const int PrepNightMinute = 0;

        private const int HoursPerDay = 24;
        private const int MinutesPerHour = 60;
        private const int HoursPerHalfDay = 12;

        private readonly Label _day;
        private readonly Label _kind;
        private readonly Label _time;
        private readonly Label _meridiem;
        private readonly Label _closing;
        private readonly Label _tagline;

        private int _dayNumber = 1;
        private DayKind _dayKind = DayKind.PrepNight;
        private int _hour = PrepNightHour;
        private int _minute = PrepNightMinute;
        private bool _isClosingSoon;

        public DayClock()
        {
            AddToClassList(ClassName);

            var heading = new VisualElement();
            heading.AddToClassList(HeadingClassName);
            // Style guide §7: "Day {n}" is heading-sized but ExtraBold, so it takes the font class and
            // .day-clock__day sets the size, rather than the Bold .text-heading role.
            _day = AddLabel(heading, KitClasses.FontDisplayExtraBold, DayClassName);
            _kind = AddLabel(heading, KitClasses.TextSubheading, KindClassName);
            _kind.AddToClassList(KitClasses.TextMuted);

            var clock = new VisualElement();
            clock.AddToClassList(ClockClassName);
            _time = AddLabel(clock, KitClasses.NumberHud, TimeClassName);
            _meridiem = AddLabel(clock, KitClasses.FontDisplayBold, MeridiemClassName);
            _closing = AddLabel(clock, KitClasses.FontDisplayBold, ClosingClassName);
            _closing.text = ClosingSoonText;

            _tagline = new Label(PrepNightTagline);
            _tagline.AddToClassList(KitClasses.FontDisplayBold);
            _tagline.AddToClassList(TaglineClassName);

            Add(heading);
            Add(clock);
            Add(_tagline);
            Refresh();
        }

        public int Day => _dayNumber;

        public DayKind Kind => _dayKind;

        public string DayText => _day.text;

        public string KindText => _kind.text;

        public string TimeText => _time.text;

        public string MeridiemText => _meridiem.text;

        public bool IsTaglineShown => _tagline.style.display != DisplayStyle.None;

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
            _day.text = "Day " + _dayNumber.ToString(CultureInfo.InvariantCulture);
            _kind.text = isPrepNight ? PrepNightName : ShowDayName;

            int hour = isPrepNight ? PrepNightHour : _hour;
            int minute = isPrepNight ? PrepNightMinute : _minute;
            _time.text = FormatTime(hour, minute, out string meridiem);
            _meridiem.text = meridiem;

            _tagline.style.display = isPrepNight ? DisplayStyle.Flex : DisplayStyle.None;
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
