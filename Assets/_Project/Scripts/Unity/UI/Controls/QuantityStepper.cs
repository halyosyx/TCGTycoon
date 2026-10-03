using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// − value + stepper. The layout is <c>UI/Controls/QuantityStepper.uxml</c>, whose root element is
    /// this control, so every instance of that template gets one; it binds its parts (decrease, value,
    /// increase) by name the first time it is used. Clamps <see cref="Value"/> to
    /// <see cref="Min"/>…<see cref="Max"/> and disables a button at its bound. Pressing a button steps
    /// once; holding it keeps stepping after <see cref="RepeatDelay"/>, every <see cref="RepeatInterval"/>,
    /// until it is released, the pointer leaves it, or the value reaches its bound. No game rules: a
    /// presenter sets the range and listens to <see cref="ValueChanged"/>.
    /// </summary>
    [UxmlElement]
    public partial class QuantityStepper : VisualElement
    {
        public const string ClassName = "quantity-stepper";
        public const string DecreaseName = "decrease";
        public const string ValueName = "value";
        public const string IncreaseName = "increase";

        private const int PrimaryButton = 0;

        private int _value;
        private int _min;
        private int _max = int.MaxValue;
        private Button _decrease;
        private Label _valueLabel;
        private Button _increase;
        private bool _isBound;
        private IVisualElementScheduledItem _repeat;
        private int _repeatDelta;

        public QuantityStepper()
        {
            AddToClassList(ClassName);
            RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            RegisterCallback<DetachFromPanelEvent>(_ => StopHold());
        }

        /// <summary>Raised with the new value when it changes through <see cref="Value"/>, the buttons or <see cref="Step"/>.</summary>
        public event Action<int> ValueChanged;

        [UxmlAttribute]
        public int Value
        {
            get => _value;
            set => SetValue(value, notify: true);
        }

        [UxmlAttribute]
        public int Min
        {
            get => _min;
            set
            {
                _min = value;
                if (_max < _min) _max = _min;
                SetValue(_value, notify: true);
            }
        }

        /// <summary>Upper bound; never below <see cref="Min"/>.</summary>
        [UxmlAttribute]
        public int Max
        {
            get => _max;
            set
            {
                _max = Math.Max(value, _min);
                SetValue(_value, notify: true);
            }
        }

        /// <summary>Milliseconds a button must be held before it starts repeating.</summary>
        [UxmlAttribute]
        public long RepeatDelay { get; set; } = 400;

        /// <summary>Milliseconds between steps while a button is held.</summary>
        [UxmlAttribute]
        public long RepeatInterval { get; set; } = 60;

        /// <summary>True while a held button is stepping.</summary>
        public bool IsRepeating => _repeat != null && _repeat.isActive;

        public void SetValueWithoutNotify(int value) => SetValue(value, notify: false);

        /// <summary>Sets range and value together without raising <see cref="ValueChanged"/>, for re-binding a pooled stepper.</summary>
        public void SetRangeAndValueWithoutNotify(int min, int max, int value)
        {
            _min = min;
            _max = Math.Max(max, min);
            SetValue(value, notify: false);
        }

        /// <summary>Adds <paramref name="delta"/> (clamped), as the buttons and the −/+ keys do.</summary>
        public void Step(int delta)
        {
            long next = (long)_value + delta;
            SetValue((int)Math.Max(int.MinValue, Math.Min(int.MaxValue, next)), notify: true);
        }

        private void SetValue(int value, bool notify)
        {
            int clamped = Math.Min(Math.Max(value, _min), _max);
            bool isChanged = clamped != _value;
            _value = clamped;
            Refresh();
            if (isChanged && notify)
            {
                ValueChanged?.Invoke(_value);
            }
        }

        private void Refresh()
        {
            BindParts();
            if (_valueLabel != null) _valueLabel.text = _value.ToString(CultureInfo.InvariantCulture);
            if (_decrease != null) _decrease.SetEnabled(_value > _min);
            if (_increase != null) _increase.SetEnabled(_value < _max);
        }

        // The parts come from the UXML, which adds them after the constructor runs, so bind lazily.
        private void BindParts()
        {
            if (_isBound)
            {
                return;
            }

            _decrease = this.Q<Button>(DecreaseName);
            _valueLabel = this.Q<Label>(ValueName);
            _increase = this.Q<Button>(IncreaseName);
            if (_decrease == null || _valueLabel == null || _increase == null)
            {
                return;
            }

            RegisterHold(_decrease, -1);
            RegisterHold(_increase, 1);
            _isBound = true;
        }

        // The button keeps a Clickable with no action, for its :hover/:active looks; stepping happens on
        // pointer down (trickle-down, before the Clickable captures the pointer) and repeats while held.
        private void RegisterHold(Button button, int delta)
        {
            button.clickable = new Clickable(() => { });
            button.RegisterCallback<PointerDownEvent>(evt => StartHold(button, delta, evt), TrickleDown.TrickleDown);
            button.RegisterCallback<PointerUpEvent>(_ => StopHold(), TrickleDown.TrickleDown);
            button.RegisterCallback<PointerCaptureOutEvent>(_ => StopHold());
            button.RegisterCallback<PointerLeaveEvent>(_ => StopHold());
        }

        private void StartHold(Button button, int delta, PointerDownEvent evt)
        {
            if (evt.button != PrimaryButton || !button.enabledInHierarchy)
            {
                return;
            }

            StopHold();
            Step(delta);
            _repeatDelta = delta;
            _repeat = schedule.Execute(RepeatStep).Every(RepeatInterval).StartingIn(RepeatDelay);
        }

        // Stops by itself at the bound, so a held button can't keep stepping once it is disabled.
        private void RepeatStep()
        {
            int before = _value;
            Step(_repeatDelta);
            if (_value == before)
            {
                StopHold();
            }
        }

        private void StopHold()
        {
            if (_repeat != null)
            {
                _repeat.Pause();
                _repeat = null;
            }
        }
    }
}
