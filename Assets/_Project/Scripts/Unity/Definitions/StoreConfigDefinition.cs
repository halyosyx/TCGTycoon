using System.Collections.Generic;
using Game.Core.Store;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Authoring asset for the supplier website (STORE_UI_REQUIREMENTS STR-02): its names, listings in
    /// display order and every label. Renaming the shop, reordering listings or flipping one to
    /// Coming soon is an Inspector edit with no recompile. Converted into a Core
    /// <see cref="StoreCatalog"/> at load.
    /// </summary>
    [CreateAssetMenu(menuName = "TCG/Store/Store Config", fileName = "StoreConfig")]
    public sealed class StoreConfigDefinition : ScriptableObject
    {
        public const string SiteTitleField = nameof(_siteTitle);
        public const string SiteTaglineField = nameof(_siteTagline);
        public const string BrowserTabTitleField = nameof(_browserTabTitle);
        public const string AddressTextField = nameof(_addressText);
        public const string ListingsField = nameof(_listings);
        public const string StringsField = nameof(_strings);

        [SerializeField, Tooltip("Header wordmark.")]
        private string _siteTitle = "VendorSupply";

        [SerializeField, Tooltip("Under the title.")]
        private string _siteTagline = "Wholesale sealed product · delivered tonight";

        [SerializeField]
        private string _browserTabTitle = "VendorSupply · Sealed";

        [SerializeField, Tooltip("Address bar text (display only).")]
        private string _addressText = "vendorsupply.mb/sealed";

        [SerializeField, Tooltip("One product card each; list order is the default display order.")]
        private List<StoreListing> _listings = new List<StoreListing>();

        [SerializeField]
        private StoreStrings _strings = new StoreStrings();

        public string SiteTitle => _siteTitle;

        public string SiteTagline => _siteTagline;

        public string BrowserTabTitle => _browserTabTitle;

        public string AddressText => _addressText;

        public IReadOnlyList<StoreListing> Listings => _listings;

        public StoreStrings Strings => _strings;

        /// <summary>The listed product with this id, or null. Names and colours owned things (stacks, held packs).</summary>
        public ProductDefinition FindProduct(string productId)
        {
            foreach (StoreListing listing in _listings)
            {
                if (listing != null && listing.Product != null && string.Equals(listing.Product.Id, productId, System.StringComparison.Ordinal))
                {
                    return listing.Product;
                }
            }

            return null;
        }

        /// <exception cref="System.InvalidOperationException">A listing has no product, or a booster pack has no pack configuration.</exception>
        /// <exception cref="System.ArgumentException">Two listings share a product id.</exception>
        public StoreCatalog ToCatalog()
        {
            var listings = new List<StoreCatalogListing>(_listings.Count);
            foreach (StoreListing listing in _listings)
            {
                if (listing == null)
                {
                    continue;
                }

                listings.Add(listing.ToCatalogListing());
            }

            return new StoreCatalog(listings);
        }
    }
}
