using System;
using Game.Core.Content;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>How one rarity tier looks: its display name, colour and the material generated from that colour.</summary>
    [Serializable]
    public sealed class RarityStyle
    {
        public const string TierField = nameof(_tier);
        public const string DisplayNameField = nameof(_displayName);
        public const string ColorField = nameof(_color);
        public const string MaterialField = nameof(_material);

        [SerializeField]
        private RarityTier _tier;

        [SerializeField, Tooltip("Name shown on cards, e.g. \"Full Art\".")]
        private string _displayName;

        [SerializeField, Tooltip("Frame and tier-name colour. Materials and UI read it from here.")]
        private Color _color = Color.white;

        [SerializeField, Tooltip("Generated URP Lit material kept in sync with the colour.")]
        private Material _material;

        public RarityStyle()
        {
        }

        public RarityStyle(RarityTier tier, string displayName, Color color)
        {
            _tier = tier;
            _displayName = displayName;
            _color = color;
        }

        public RarityTier Tier => _tier;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _tier.ToString() : _displayName;

        public Color Color => _color;

        public Material Material => _material;
    }
}
