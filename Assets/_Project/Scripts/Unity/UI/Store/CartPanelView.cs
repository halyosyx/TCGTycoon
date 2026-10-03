using System;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Store
{
    /// <summary>
    /// Slides the cart panel in and out, exactly as Docs/UI/STORE_MARKUP_README.md §4 prescribes: the
    /// panel is made visible first (<see cref="ShownClass"/>), the slide starts on the next frame
    /// (<see cref="OpenClass"/>), and it is hidden only after the slide-out's translate transition ends.
    /// It never toggles display or the hierarchy, so it never pops. Holds no statics.
    /// </summary>
    public sealed class CartPanelView
    {
        public const string ShownClass = "store--cart-shown";
        public const string OpenClass = "store--cart-open";

        private readonly VisualElement _site;
        private readonly VisualElement _panel;
        private readonly VisualElement _scrim;

        /// <param name="site">The element named store-site.</param>
        public CartPanelView(VisualElement site)
        {
            _site = site ?? throw new ArgumentNullException(nameof(site));
            _panel = site.Q<VisualElement>("cart-panel");
            _scrim = site.Q<VisualElement>("cart-scrim");
            _scrim.RegisterCallback<ClickEvent>(_ => Close());
            _panel.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        }

        /// <summary>Raised when the cart starts closing, whatever closed it (the scrim, a key, the button).</summary>
        public event Action Closed;

        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _site.AddToClassList(ShownClass);            // 1. visible, still parked off-screen
            _scrim.pickingMode = PickingMode.Position;
            // 2. Start the slide on the NEXT frame. Adding both classes in the same frame lets the style
            //    resolve straight to the end state, and the panel pops instead of sliding.
            _site.schedule.Execute(() => { if (IsOpen) _site.AddToClassList(OpenClass); });
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _site.RemoveFromClassList(OpenClass);         // slides out; Shown stays until it's off-screen
            _scrim.pickingMode = PickingMode.Ignore;
            Closed?.Invoke();
        }

        /// <summary>Puts the panel straight back to closed with no motion, for when the whole screen is leaving.</summary>
        public void Reset()
        {
            IsOpen = false;
            _site.RemoveFromClassList(OpenClass);
            _site.RemoveFromClassList(ShownClass);
            _scrim.pickingMode = PickingMode.Ignore;
        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            // Hide only after the slide-out finished, and only for the translate transition.
            if (!IsOpen && evt.stylePropertyNames.Contains("translate"))
                _site.RemoveFromClassList(ShownClass);
        }
    }
}
