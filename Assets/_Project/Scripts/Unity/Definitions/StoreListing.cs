using System;
using Game.Core.Store;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>One product card on the store (STORE_UI_REQUIREMENTS STR-03). Serialised inside <see cref="StoreConfigDefinition"/>.</summary>
    [Serializable]
    public sealed class StoreListing
    {
        public const string ProductField = nameof(_product);
        public const string AvailabilityField = nameof(_availability);
        public const string StockPerNightField = nameof(_stockPerNight);
        public const string DisplayNameOverrideField = nameof(_displayNameOverride);
        public const string ThumbnailField = nameof(_thumbnail);

        [SerializeField]
        private ProductDefinition _product;

        [SerializeField]
        private ListingAvailability _availability = ListingAvailability.Available;

        [SerializeField, Min(-1), Tooltip("-1 = unlimited; 0 shows \"Sold out tonight\". Restocks every Prep Night.")]
        private int _stockPerNight = StoreCatalogListing.UnlimitedStock;

        [SerializeField, Tooltip("Empty: the product type's name.")]
        private string _displayNameOverride;

        [SerializeField, Tooltip("Empty: a placeholder shape tinted with the set colour.")]
        private Sprite _thumbnail;

        public ProductDefinition Product => _product;

        public ListingAvailability Availability => _availability;

        public int StockPerNight => _stockPerNight;

        /// <summary>The override, or the product type's name.</summary>
        public string DisplayName => !string.IsNullOrEmpty(_displayNameOverride) ? _displayNameOverride : _product == null ? string.Empty : _product.TypeDisplayName;

        public Sprite Thumbnail => _thumbnail;

        public StoreCatalogListing ToCatalogListing()
        {
            if (_product == null)
            {
                throw new InvalidOperationException("A store listing has no product.");
            }

            return new StoreCatalogListing(_product.ToProduct(), _availability, _stockPerNight);
        }
    }
}
