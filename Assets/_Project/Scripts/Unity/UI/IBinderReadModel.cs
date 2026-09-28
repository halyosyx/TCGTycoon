using System;
using System.Collections.Generic;

namespace Game.Unity.UI
{
    /// <summary>
    /// What a binder screen reads, and all it may read (style guide §8): the tabs with their counts
    /// and each tab's entries in display order. Binder views (the prototype, and whatever replaces it)
    /// depend on this interface only, never on Core services, so a view can be deleted and rewritten
    /// without touching the data side.
    /// </summary>
    public interface IBinderReadModel
    {
        /// <summary>Raised after the owned items change; re-read <see cref="Tabs"/> and entries then.</summary>
        event Action Changed;

        /// <summary>Every tab in display order (Set A, Set B, Sealed, Bulk), with counts.</summary>
        IReadOnlyList<BinderTabInfo> Tabs { get; }

        /// <summary>A tab's entries: tier high to low, then item id. Empty when the tab holds nothing.</summary>
        IReadOnlyList<BinderEntry> GetEntries(BinderTab tab);
    }
}
