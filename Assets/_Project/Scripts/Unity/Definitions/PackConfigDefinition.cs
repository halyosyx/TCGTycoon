using System.Collections.Generic;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Authoring asset for a pack product: price, card set and an ordered list of slots. Converted
    /// into a Core <see cref="PackConfig"/> at load, so changing the asset changes the game with no
    /// code change or recompile.
    /// </summary>
    [CreateAssetMenu(menuName = "TCG/Pack Configuration", fileName = "NewPackConfiguration")]
    public sealed class PackConfigDefinition : ScriptableObject
    {
        public const string DisplayNameField = nameof(_displayName);
        public const string PriceField = nameof(_priceCents);
        public const string CardSetField = nameof(_cardSet);
        public const string SlotsField = nameof(_slots);

        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField, Tooltip("Market price in cents. Fixed until market prices exist (F4).")]
        private long _priceCents;

        [SerializeField, Tooltip("The set whose cards this pack contains.")]
        private CardSetDefinition _cardSet;

        [SerializeField, Tooltip("Slots in reveal order. Any number of slots; any tiers per slot.")]
        private List<PackSlotData> _slots = new List<PackSlotData>();

        public CardSetDefinition CardSet => _cardSet;

        /// <summary>
        /// Slot entries holding a removed or unknown tier (stale seven-tier data), each naming this
        /// asset and the slot. The full rule check is <see cref="Game.Core.Packs.PackConfigValidator"/>.
        /// </summary>
        public List<string> FindTierProblems()
        {
            var problems = new List<string>();
            PackConfig config = ToPackConfig();
            for (int slotIndex = 0; slotIndex < config.Slots.Count; slotIndex++)
            {
                foreach (TierWeight entry in config.Slots[slotIndex].Entries)
                {
                    if (!RarityTiers.IsDefined(entry.Tier))
                    {
                        problems.Add($"Pack '{name}': slot {slotIndex + 1} uses {RarityTiers.Describe(entry.Tier)}.");
                    }
                }
            }

            return problems;
        }

        public PackConfig ToPackConfig()
        {
            var slots = new List<PackSlot>(_slots.Count);
            foreach (PackSlotData slot in _slots)
            {
                slots.Add(slot != null ? slot.ToPackSlot() : new PackSlot(System.Array.Empty<TierWeight>()));
            }

            return new PackConfig(string.IsNullOrWhiteSpace(_id) ? name : _id, _displayName, _priceCents, slots);
        }
    }
}
