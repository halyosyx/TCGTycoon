using Game.Core.Common;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// HUD cash (style guide §7): the amount, right-aligned with no label or icon, and an event line
    /// under it for about two seconds after a sale or purchase ("Sale ▲ +$14.00"). The plate hugs the
    /// amount: the event line is collapsed while hidden, fades in, and collapses again after fading out.
    /// Table fees never appear here; they belong on the Results screen. Money arrives as cents.
    /// </summary>
    [UxmlElement]
    public partial class CashReadout : VisualElement
    {
        public const string ClassName = "cash-readout";
        public const string AmountClassName = ClassName + "__amount";
        public const string EventClassName = ClassName + "__event";
        public const string EventVisibleClassName = EventClassName + "--visible";
        public const string EventCollapsedClassName = EventClassName + "--collapsed";
        public const string EventLabelClassName = ClassName + "__event-label";

        /// <summary>How long the event line stays up, in milliseconds (style guide: about 2 s).</summary>
        public const long EventMilliseconds = 2000;

        private readonly Label _amount;
        private readonly VisualElement _event;
        private readonly Label _eventLabel;
        private readonly SignedAmount _eventDelta;
        private readonly IVisualElementScheduledItem _hideEvent;
        private readonly IVisualElementScheduledItem _collapseEvent;
        private long _amountCents;

        public CashReadout()
        {
            AddToClassList(ClassName);

            _amount = new Label();
            _amount.AddToClassList(KitClasses.NumberHud);
            _amount.AddToClassList(AmountClassName);

            _event = new VisualElement();
            _event.AddToClassList(EventClassName);
            _event.AddToClassList(EventCollapsedClassName);
            _eventLabel = new Label();
            _eventLabel.AddToClassList(KitClasses.TextCaption);
            _eventLabel.AddToClassList(EventLabelClassName);
            _eventDelta = new SignedAmount();
            _event.Add(_eventLabel);
            _event.Add(_eventDelta);

            Add(_amount);
            Add(_event);

            // One scheduled item, re-armed per event, so a burst of sales keeps the latest line up.
            _hideEvent = schedule.Execute(HideEvent);
            _hideEvent.Pause();
            _collapseEvent = schedule.Execute(CollapseEvent);
            _collapseEvent.Pause();
            AmountCents = 0;
        }

        [UxmlAttribute]
        public long AmountCents
        {
            get => _amountCents;
            set
            {
                _amountCents = value;
                _amount.text = Money.FormatDisplay(value);
            }
        }

        public string AmountText => _amount.text;

        /// <summary>True from <see cref="ShowEvent"/> until the line has faded out and collapsed.</summary>
        public bool IsEventShown => !_event.ClassListContains(EventCollapsedClassName);

        public string EventLabelText => _eventLabel.text;

        public SignedAmount EventDelta => _eventDelta;

        public void SetAmount(long cents) => AmountCents = cents;

        /// <summary>Shows "{label} ▲ +$x" (or ▼ −$x) under the amount for about two seconds.</summary>
        public void ShowEvent(string label, long deltaCents)
        {
            _eventLabel.text = label ?? string.Empty;
            _eventDelta.Cents = deltaCents;
            _collapseEvent.Pause();
            bool wasCollapsed = _event.ClassListContains(EventCollapsedClassName);
            _event.RemoveFromClassList(EventCollapsedClassName);
            if (wasCollapsed)
            {
                // Shown without the visible class first, so the next frame's class change runs the fade-in.
                _event.schedule.Execute(() => _event.AddToClassList(EventVisibleClassName));
            }
            else
            {
                _event.AddToClassList(EventVisibleClassName);
            }

            _hideEvent.ExecuteLater(EventMilliseconds);
        }

        private void HideEvent()
        {
            _hideEvent.Pause();
            _event.RemoveFromClassList(EventVisibleClassName);

            // Collapse once faded (--duration-medium), so the plate shrinks back to the amount.
            long fadeMilliseconds = UiTransitions.LongestMilliseconds(_event);
            if (fadeMilliseconds <= 0)
            {
                CollapseEvent();
            }
            else
            {
                _collapseEvent.ExecuteLater(fadeMilliseconds);
            }
        }

        private void CollapseEvent()
        {
            _collapseEvent.Pause();
            _event.AddToClassList(EventCollapsedClassName);
        }
    }
}
