using System;
using System.Collections.Generic;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>Authoring form of one pack slot: the tiers it can roll and their weights.</summary>
    [Serializable]
    public sealed class PackSlotData
    {
        public const string EntriesField = nameof(_entries);

        [SerializeField, Tooltip("Tiers this slot can roll. Any slot may hold any tiers.")]
        private List<TierWeightData> _entries = new List<TierWeightData>();

        public PackSlot ToPackSlot()
        {
            var entries = new List<TierWeight>(_entries.Count);
            foreach (TierWeightData entry in _entries)
            {
                entries.Add(entry.ToTierWeight());
            }

            return new PackSlot(entries);
        }
    }
}
