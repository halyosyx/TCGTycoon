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

        /// <summary>
        /// Every tab in display order, with counts: one per card set the binder was given, then Sealed.
        /// The list of tabs is fixed for the model's lifetime; only the counts change.
        /// </summary>
        IReadOnlyList<BinderTabInfo> Tabs { get; }

        /// <summary>
        /// The entries of the tab at <paramref name="tabIndex"/> (an index into <see cref="Tabs"/>):
        /// tier high to low, then item id. Empty when the tab holds nothing.
        /// </summary>
        IReadOnlyList<BinderEntry> GetEntries(int tabIndex);
    }
}
