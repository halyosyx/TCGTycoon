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
    /// One product card (StoreProductCard.uxml), pooled and re-bound to a listing whenever the grid
    /// changes. Binds by element name and switches state with the markup's classes only. No rules:
    /// Add to cart is reported to the screen, which calls <c>StoreService</c>.
    /// </summary>
    public sealed class StoreProductCardView
    {
        public const string FocusedClass = "store-product--focused";
        public const string SoonClass = "store-product--soon";
        public const string SoldOutClass = "store-product--sold-out";

        private const string ArtPackClass = "store-product__art--pack";
        private const string ArtBundleClass = "store-product__art--bundle";
        private const string ArtBoxClass = "store-product__art--box";

        private readonly VisualElement _art;
        private readonly Label _name;
        private readonly Label _subtitle;
        private readonly Label _unitPriceLabel;
        private readonly Label _unitPrice;
        private readonly Label _amountLabel;
        private readonly QuantityStepper _stepper;
        private readonly Label _totalLabel;
        private readonly Label _lineTotal;
        private readonly Button _addToCart;
        private readonly Label _addToCartLabel;
        private readonly Label _statusTitle;
        private readonly Label _statusDetail;
        private long _unitPriceCents;

        /// <param name="template">StoreProductCard.uxml; its root element (product-card) becomes <see cref="Root"/>.</param>
        public StoreProductCardView(VisualTreeAsset template)
        {
            // The grid wraps its direct children two per row, so the card root goes in, not the
            // TemplateContainer. Its styles come from the site's sheets, which sit on an ancestor.
            Root = template.Instantiate().Q<VisualElement>("product-card");
            Root.RemoveFromHierarchy();

            _art = Root.Q<VisualElement>("thumbnail-art");
            _name = Root.Q<Label>("product-name");
            _subtitle = Root.Q<Label>("product-subtitle");
            _unitPriceLabel = Root.Q<Label>("unit-price-label");
            _unitPrice = Root.Q<Label>("unit-price");
            _amountLabel = Root.Q<Label>("amount-label");
            _stepper = Root.Q<QuantityStepper>();
            _totalLabel = Root.Q<Label>("total-label");
            _lineTotal = Root.Q<Label>("line-total");
            _addToCart = Root.Q<Button>("add-to-cart");
            _addToCartLabel = Root.Q<Label>("add-to-cart-label");
            _statusTitle = Root.Q<Label>("status-title");
            _statusDetail = Root.Q<Label>("status-detail");

            _stepper.ValueChanged += value =>
            {
                RefreshTotal();
                RefreshAddButton();
                AmountChanged?.Invoke(this, value);
            };
            _addToCart.clicked += () => AddRequested?.Invoke(this);
            Root.RegisterCallback<PointerDownEvent>(_ => Pressed?.Invoke(this));
        }

        /// <summary>Add to cart was clicked.</summary>
        public event Action<StoreProductCardView> AddRequested;

        /// <summary>The amount stepper changed (by click or key). The screen remembers it per listing.</summary>
        public event Action<StoreProductCardView, int> AmountChanged;

        /// <summary>The pointer went down anywhere on the card (it takes keyboard focus).</summary>
        public event Action<StoreProductCardView> Pressed;

        public VisualElement Root { get; }

        public string ListingId { get; private set; }

        /// <summary>True when the listing can be bought: on sale and in stock (the amount may still be 0).</summary>
        public bool CanAdd { get; private set; }

        public int Amount => _stepper.Value;

        /// <param name="maxUnlimitedAmount">Stepper cap when stock is unlimited.</param>
        /// <param name="amount">The amount this listing had (0 the first time); clamped to the stock.</param>
        public void Bind(StoreListingInfo info, StoreListingState listing, StoreStrings strings, int maxUnlimitedAmount, int amount)
        {
            ListingId = listing.Id;
            _unitPriceCents = listing.UnitPriceCents;
            bool isSoon = listing.Availability != ListingAvailability.Available;
            bool isSoldOut = !isSoon && !listing.IsUnlimited && listing.StockRemaining == 0;
            CanAdd = !isSoon && !isSoldOut;

            Root.EnableInClassList(SoonClass, isSoon);
            Root.EnableInClassList(SoldOutClass, isSoldOut);
            BindArt(info);

            _name.text = info.Name;
            _subtitle.text = isSoon ? info.SetName : string.Format(CultureInfo.InvariantCulture, strings.SubtitleFormat, info.SetName, Status(info, listing, strings));
            _unitPriceLabel.text = strings.UnitPrice;
            _unitPrice.text = Money.FormatDisplay(listing.UnitPriceCents);
            _amountLabel.text = strings.Amount;
            _totalLabel.text = strings.Total;
            _addToCartLabel.text = strings.AddToCart;
            _statusTitle.text = isSoon ? strings.ComingSoon : strings.SoldOutTonight;
            _statusDetail.text = isSoon ? strings.NotStockedYet : strings.RestocksNextPrepNight;

            // The amount starts at 0: nothing is added until the player picks how many.
            int max = listing.IsUnlimited ? maxUnlimitedAmount : Math.Max(0, listing.StockRemaining);
            _stepper.SetRangeAndValueWithoutNotify(0, max, amount);
            RefreshTotal();
            RefreshAddButton();
        }

        public void StepAmount(int delta) => _stepper.Step(delta);

        public void SetFocused(bool isFocused) => Root.EnableInClassList(FocusedClass, isFocused);

        private static string Status(StoreListingInfo info, StoreListingState listing, StoreStrings strings)
        {
            if (!listing.IsUnlimited && listing.StockRemaining > 0)
            {
                return string.Format(CultureInfo.InvariantCulture, strings.LeftTonightFormat, listing.StockRemaining);
            }

            return info.IsInPrint ? strings.InPrint : strings.OutOfPrint;
        }

        // A sprite if the listing has one; otherwise the markup's placeholder shape for the product type,
        // filled with the set's colour.
        private void BindArt(StoreListingInfo info)
        {
            _art.EnableInClassList(ArtPackClass, info.Type == ProductType.BoosterPack);
            _art.EnableInClassList(ArtBundleClass, info.Type == ProductType.Bundle);
            _art.EnableInClassList(ArtBoxClass, info.Type == ProductType.Box);
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
        }

        // Disabled (the markup's :disabled look) at an amount of 0.
        private void RefreshAddButton() => _addToCart.SetEnabled(CanAdd && _stepper.Value > 0);

        private void RefreshTotal()
        {
            _lineTotal.text = Money.FormatDisplay(_unitPriceCents * _stepper.Value);
        }
    }
}
