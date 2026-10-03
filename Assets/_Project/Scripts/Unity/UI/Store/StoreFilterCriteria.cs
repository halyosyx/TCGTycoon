using Game.Core.Store;

namespace Game.Unity.UI.Store
{
    /// <summary>The store's tab, search text, Type and Sort choices. Defaults: every set, any type, by price.</summary>
    public sealed class StoreFilterCriteria
    {
        /// <summary>The selected set tab's set id; null or empty for "All sets".</summary>
        public string SetId;

        public string Search;

        /// <summary>Null for "Any".</summary>
        public ProductType? Type;

        public StoreSort Sort = StoreSort.Price;
    }
}
