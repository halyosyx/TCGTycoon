namespace Game.Unity.UI
{
    /// <summary>One binder tab: what it holds, its id and label, and how many distinct items (pockets) it has.</summary>
    public readonly struct BinderTabInfo
    {
        public BinderTabInfo(BinderTabKind kind, string id, string title, int count)
        {
            Kind = kind;
            Id = id;
            Title = title;
            Count = count;
        }

        public BinderTabKind Kind { get; }

        /// <summary>The card set id for a set tab; <see cref="InventoryBinderReadModel.SealedTabId"/> for sealed.</summary>
        public string Id { get; }

        public string Title { get; }

        /// <summary>Distinct items in the tab, i.e. filled pockets; copies of one card count once.</summary>
        public int Count { get; }
    }
}
