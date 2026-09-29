using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Controls
{
    /// <summary>
    /// A column of toasts, newest on top. Each toast fades in, stays for <see cref="ToastMilliseconds"/>
    /// and fades out; beyond <see cref="MaxToasts"/> the oldest goes at once. Timing runs on the
    /// element's scheduler, not coroutines, so it needs no MonoBehaviour.
    /// </summary>
    [UxmlElement]
    public partial class ToastStack : VisualElement
    {
        public const string ClassName = "toast-stack";
        public const string SpacedToastClassName = "toast--spaced";

        /// <summary>How long a toast stays up, in milliseconds.</summary>
        public const long ToastMilliseconds = 4000;

        private const int DefaultMaxToasts = 4;

        private readonly List<Toast> _toasts = new List<Toast>();

        public ToastStack()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
        }

        [UxmlAttribute]
        public int MaxToasts { get; set; } = DefaultMaxToasts;

        /// <summary>Toasts on screen, newest first.</summary>
        public IReadOnlyList<Toast> Toasts => _toasts;

        public Toast Show(ToastKind kind, string title, string detail, long? amountCents)
        {
            var toast = new Toast();
            toast.Set(kind, title, detail, amountCents);
            _toasts.Insert(0, toast);
            Insert(0, toast);
            while (_toasts.Count > MaxToasts && _toasts.Count > 0)
            {
                Remove(_toasts[_toasts.Count - 1], isImmediate: true);
            }

            RefreshSpacing();

            // Added without the shown class first, so the next frame's class change runs the fade-in transition.
            toast.schedule.Execute(() => toast.AddToClassList(Toast.ShownClassName));
            toast.schedule.Execute(() => Remove(toast, isImmediate: false)).StartingIn(ToastMilliseconds);
            return toast;
        }

        public void ClearToasts()
        {
            while (_toasts.Count > 0)
            {
                Remove(_toasts[0], isImmediate: true);
            }
        }

        private void Remove(Toast toast, bool isImmediate)
        {
            if (!_toasts.Remove(toast))
            {
                return;
            }

            // The fade is the toast's USS transition (--duration-medium), so the token alone sets it.
            long fadeMilliseconds = isImmediate ? 0 : UiTransitions.LongestMilliseconds(toast);
            if (fadeMilliseconds <= 0)
            {
                toast.RemoveFromHierarchy();
            }
            else
            {
                toast.RemoveFromClassList(Toast.ShownClassName);
                toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(fadeMilliseconds);
            }

            RefreshSpacing();
        }

        // USS has no gap: every toast after the first carries the spacing.
        private void RefreshSpacing()
        {
            for (int i = 0; i < _toasts.Count; i++)
            {
                _toasts[i].EnableInClassList(SpacedToastClassName, i > 0);
            }
        }
    }
}
