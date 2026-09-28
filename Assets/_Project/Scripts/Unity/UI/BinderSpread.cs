using System.Collections.Generic;

namespace Game.Unity.UI
{
    /// <summary>
    /// Two facing binder pages of <see cref="BinderPaging.SlotsPerPage"/> pockets each. Empty pockets
    /// are null. Page numbers are 1-based, as printed under each page ("Pages 3–4 of 4").
    /// </summary>
    public sealed class BinderSpread
    {
        public BinderSpread(IReadOnlyList<BinderEntry> leftPage, IReadOnlyList<BinderEntry> rightPage, int spreadIndex, int spreadCount)
        {
            LeftPage = leftPage;
            RightPage = rightPage;
            SpreadIndex = spreadIndex;
            SpreadCount = spreadCount;
        }

        public IReadOnlyList<BinderEntry> LeftPage { get; }

        public IReadOnlyList<BinderEntry> RightPage { get; }

        public int SpreadIndex { get; }

        public int SpreadCount { get; }

        public int LeftPageNumber => SpreadIndex * BinderPaging.PagesPerSpread + 1;

        public int RightPageNumber => LeftPageNumber + 1;

        public int PageCount => SpreadCount * BinderPaging.PagesPerSpread;
    }
}
