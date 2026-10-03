using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Store;
using Game.Unity.Definitions;
using UnityEngine;

namespace Game.Unity.UI.Store
{
    /// <summary>
    /// What the store shows about one listing that Core doesn't carry: names, the set's colour and
    /// lifecycle, the thumbnail. Read from the store config (and the product's card set, STR-05) once,
    /// when the screen is built. Prices and stock always come from <c>StoreService</c>.
    /// </summary>
    public sealed class StoreListingInfo
    {
        private StoreListingInfo(StoreListing listing, int order)
        {
            ProductDefinition product = listing.Product;
            CardSetDefinition set = product.CardSet;
            Id = product.Id;
            Name = listing.DisplayName;
            Type = product.Type;
            TypeName = product.TypeDisplayName;
            SetId = set == null ? string.Empty : set.Id;
            SetName = set == null ? string.Empty : set.DisplayName;
            SetShortName = set == null ? string.Empty : set.ShortName;
            IsInPrint = set == null || set.Lifecycle == SetLifecycle.InPrint;
            Colour = set == null ? Color.gray : set.Colour;
            Thumbnail = listing.Thumbnail;
            Order = order;
        }

        public string Id { get; }

        public string Name { get; }

        public ProductType Type { get; }

        public string TypeName { get; }

        public string SetId { get; }

        public string SetName { get; }

        public string SetShortName { get; }

        public bool IsInPrint { get; }

        public Color Colour { get; }

        /// <summary>Null: show the placeholder shape in <see cref="Colour"/>.</summary>
        public Sprite Thumbnail { get; }

        /// <summary>Position in the store config's listing list.</summary>
        public int Order { get; }

        /// <summary>One entry per listing with a product, keyed by listing (product) id.</summary>
        public static Dictionary<string, StoreListingInfo> FromConfig(StoreConfigDefinition config)
        {
            var infos = new Dictionary<string, StoreListingInfo>(System.StringComparer.Ordinal);
            for (int i = 0; i < config.Listings.Count; i++)
            {
                StoreListing listing = config.Listings[i];
                if (listing != null && listing.Product != null && !infos.ContainsKey(listing.Product.Id))
                {
                    infos.Add(listing.Product.Id, new StoreListingInfo(listing, i));
                }
            }

            return infos;
        }
    }
}
