using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>Authoring asset for one card. Converted into a Core <see cref="Card"/> at load; Core never sees this type.</summary>
    [CreateAssetMenu(menuName = "TCG/Card", fileName = "NewCard")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id used by inventory and saves. Don't change it once cards can be owned.")]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private RarityTier _tier;

        [SerializeField, Tooltip("Value in cents, used for Rip EV until market prices exist.")]
        private long _valueCents;

        /// <summary>Converts to the Core card. Throws when the id is missing, naming this asset.</summary>
        public Card ToCard(string setId)
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                throw new System.InvalidOperationException($"Card asset '{name}' has no id.");
            }

            return new Card(_id, _displayName, setId, _tier, _valueCents);
        }
    }
}
