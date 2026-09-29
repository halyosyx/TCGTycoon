using System;

namespace Game.Unity.UI
{
    /// <summary>A card set the binder gives a tab: its id (matches <c>Card.SetId</c>) and the tab label.</summary>
    public sealed class BinderSet
    {
        public BinderSet(string setId, string title)
        {
            if (string.IsNullOrEmpty(setId)) throw new ArgumentException("A binder set needs a set id.", nameof(setId));

            SetId = setId;
            Title = string.IsNullOrEmpty(title) ? setId : title;
        }

        public string SetId { get; }

        public string Title { get; }
    }
}
