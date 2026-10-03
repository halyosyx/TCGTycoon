using System.Collections.Generic;
using Game.Unity.UI.Controls;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI.Controls
{
    public sealed class QuantityStepperTests
    {
        [Test]
        public void Value_OutsideRange_ClampsToMinAndMax()
        {
            var stepper = new QuantityStepper { Min = 1, Max = 12 };

            stepper.Value = 20;
            Assert.That(stepper.Value, Is.EqualTo(12));

            stepper.Value = -3;
            Assert.That(stepper.Value, Is.EqualTo(1));
        }

        [Test]
        public void Max_LoweredBelowValue_ClampsValueAndNotifies()
        {
            var stepper = new QuantityStepper { Min = 1, Max = 99, Value = 30 };
            var changes = new List<int>();
            stepper.ValueChanged += changes.Add;

            stepper.Max = 12;

            Assert.That(stepper.Value, Is.EqualTo(12));
            Assert.That(changes, Is.EqualTo(new[] { 12 }));
        }

        [Test]
        public void Max_BelowMin_BecomesMin()
        {
            var stepper = new QuantityStepper { Min = 1 };

            stepper.Max = 0;

            Assert.That(stepper.Max, Is.EqualTo(1));
            Assert.That(stepper.Value, Is.EqualTo(1));
        }

        [Test]
        public void ValueChanged_RaisedOnlyWhenTheValueChanges()
        {
            var stepper = new QuantityStepper { Min = 1, Max = 10 };
            var changes = new List<int>();
            stepper.ValueChanged += changes.Add;

            stepper.Value = 4;
            stepper.Value = 4;
            stepper.SetValueWithoutNotify(6);
            stepper.Step(1);

            Assert.That(changes, Is.EqualTo(new[] { 4, 7 }));
        }

        [Test]
        public void SetRangeAndValueWithoutNotify_WiderRangeThanBefore_KeepsTheNewValueSilently()
        {
            var stepper = new QuantityStepper { Min = 1, Max = 5, Value = 5 };
            var changes = new List<int>();
            stepper.ValueChanged += changes.Add;

            stepper.SetRangeAndValueWithoutNotify(1, 12, 12);

            Assert.That(stepper.Value, Is.EqualTo(12));
            Assert.That(stepper.Max, Is.EqualTo(12));
            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void Parts_FromMarkup_ShowValueAndDisableButtonsAtTheBounds()
        {
            var stepper = new QuantityStepper();
            var decrease = new Button { name = QuantityStepper.DecreaseName };
            var value = new Label { name = QuantityStepper.ValueName };
            var increase = new Button { name = QuantityStepper.IncreaseName };
            stepper.Add(decrease);
            stepper.Add(value);
            stepper.Add(increase);

            stepper.Min = 1;
            stepper.Max = 3;
            stepper.Value = 1;

            Assert.That(value.text, Is.EqualTo("1"));
            Assert.That(decrease.enabledSelf, Is.False);
            Assert.That(increase.enabledSelf, Is.True);

            stepper.Value = 3;

            Assert.That(value.text, Is.EqualTo("3"));
            Assert.That(decrease.enabledSelf, Is.True);
            Assert.That(increase.enabledSelf, Is.False);
        }
    }
}
