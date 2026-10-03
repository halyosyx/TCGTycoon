using System;
using System.Globalization;
using Game.Core.Common;
using Game.Core.Store;
using Game.Unity.Definitions;
using Game.Unity.UI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Store
{
    /// <summary>
    /// One cart line (StoreCartLine.uxml), pooled. Quantity changes and Remove are reported to the
    /// screen, which calls <c>StoreService</c>; the line re-binds from the cart afterwards.
    /// </summary>
    public sealed class StoreCartLineView
    {
        public const string FocusedClass = "store-cart-line--focused";

        private readonly VisualElement _art;
        private readonly Label _name;
        private readonly QuantityStepper _stepper;
        private readonly Label _total;

        /// <param name="template">StoreCartLine.uxml; its root element (cart-line) becomes <see cref="Root"/>.</param>
        public StoreCartLineView(VisualTreeAsset template)
        {
            Root = template.Instantiate().Q<VisualElement>("cart-line");
            Root.RemoveFromHierarchy();

            _art = Root.Q<VisualElement>("line-thumb-art");
            _name = Root.Q<Label>("line-name");
            _stepper = Root.Q<QuantityStepper>();
            _total = Root.Q<Label>("line-total");

            _stepper.ValueChanged += value => QuantityChanged?.Invoke(this, value);
            Root.Q<Button>("line-remove").clicked += () => RemoveRequested?.Invoke(this);
            Root.RegisterCallback<PointerDownEvent>(_ => Pressed?.Invoke(this));
        }

        public event Action<StoreCartLineView, int> QuantityChanged;

        public event Action<StoreCartLineView> RemoveRequested;

        public event Action<StoreCartLineView> Pressed;

        public VisualElement Root { get; }

        public string ListingId { get; private set; }

        public int Quantity => _stepper.Value;

        /// <param name="maxQuantity">The line's stepper cap: stock remaining, or the screen's cap when unlimited.</param>
        public void Bind(StoreListingInfo info, CartLine line, int maxQuantity, StoreStrings strings)
        {
            ListingId = line.ListingId;
            _name.text = string.Format(CultureInfo.InvariantCulture, strings.CartLineFormat, info.SetName, info.Name);
            _total.text = Money.FormatDisplay(line.LineTotalCents);
            if (info.Thumbnail != null)
            {
                _art.style.backgroundImage = new StyleBackground(info.Thumbnail);
                _art.style.backgroundColor = new StyleColor(Color.clear);
            }
            else
            {
                _art.style.backgroundImage = StyleKeyword.Null;
                _art.style.backgroundColor = new StyleColor(info.Colour);
            }

            // Without notify: binding mustn't echo back into the cart.
            _stepper.SetRangeAndValueWithoutNotify(1, Math.Max(maxQuantity, line.Quantity), line.Quantity);
        }

        public void StepQuantity(int delta) => _stepper.Step(delta);

        public void SetFocused(bool isFocused) => Root.EnableInClassList(FocusedClass, isFocused);
    }
}
