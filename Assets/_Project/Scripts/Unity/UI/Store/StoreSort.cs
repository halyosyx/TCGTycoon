namespace Game.Unity.UI.Store
{
    /// <summary>The store's Sort choices, in dropdown order.</summary>
    public enum StoreSort
    {
        /// <summary>Cheapest unit price first; ties keep catalog order.</summary>
        Price = 0,

        /// <summary>Product name A to Z; ties keep catalog order.</summary>
        Name = 1,
    }
}
