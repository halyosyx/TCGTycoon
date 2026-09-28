namespace Game.Unity.UI
{
    /// <summary>One binder tab: which it is, its label and how many distinct items (pockets) it holds.</summary>
    public readonly struct BinderTabInfo
    {
        public BinderTabInfo(BinderTab tab, string title, int count)
        {
            Tab = tab;
            Title = title;
            Count = count;
        }

        public BinderTab Tab { get; }

        public string Title { get; }

        /// <summary>Distinct items in the tab, i.e. filled pockets; copies of one card count once.</summary>
        public int Count { get; }
    }
}
