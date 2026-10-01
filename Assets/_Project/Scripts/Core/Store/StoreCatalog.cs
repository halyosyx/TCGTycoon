using System;
using System.Collections.Generic;

namespace Game.Core.Store
{
    /// <summary>
    /// The store's listings in display order, converted from the store config asset at bootstrap.
    /// Plain data with no Unity types; listing ids are unique.
    /// </summary>
    public sealed class StoreCatalog
    {
        public static readonly StoreCatalog Empty = new StoreCatalog(Array.Empty<StoreCatalogListing>());

        /// <exception cref="ArgumentException">An entry is empty or two listings share an id.</exception>
        public StoreCatalog(IEnumerable<StoreCatalogListing> listings)
        {
            if (listings == null) throw new ArgumentNullException(nameof(listings));

            var ordered = new List<StoreCatalogListing>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (StoreCatalogListing listing in listings)
            {
                if (listing == null)
                {
                    throw new ArgumentException("The listing list contains an empty entry.", nameof(listings));
                }

                if (!ids.Add(listing.Id))
                {
                    throw new ArgumentException($"Two listings share the id '{listing.Id}'.", nameof(listings));
                }

                ordered.Add(listing);
            }

            Listings = ordered.AsReadOnly();
        }

        public IReadOnlyList<StoreCatalogListing> Listings { get; }
    }
}
