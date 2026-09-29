using System;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>Reads transition timing from an element's resolved USS, so durations live only in the tokens.</summary>
    internal static class UiTransitions
    {
        private const float MillisecondsPerSecond = 1000f;

        /// <summary>
        /// The element's longest transition duration in whole milliseconds; 0 when it has none (for
        /// example outside a styled panel), so callers can act at once.
        /// </summary>
        public static long LongestMilliseconds(VisualElement element)
        {
            float longest = 0f;
            foreach (TimeValue duration in element.resolvedStyle.transitionDuration)
            {
                float milliseconds = duration.unit == TimeUnit.Second ? duration.value * MillisecondsPerSecond : duration.value;
                longest = Math.Max(longest, milliseconds);
            }

            return (long)Math.Ceiling(longest);
        }
    }
}
