using System;
using Game.Core.Content;
using Game.Unity.Definitions;
using TMPro;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Shows one card on the world-space card prefab: the tier's frame material plus name, tier and id
    /// labels, all coloured from the palette. Display only; no card rules live here.
    /// </summary>
    public sealed class WorldCardView : MonoBehaviour
    {
        public const string FrameRendererField = nameof(_frameRenderer);
        public const string NameLabelField = nameof(_nameLabel);
        public const string TierLabelField = nameof(_tierLabel);
        public const string IdLabelField = nameof(_idLabel);

        [SerializeField, Tooltip("Quad behind the face that shows the tier colour as a frame.")]
        private Renderer _frameRenderer;

        [SerializeField]
        private TMP_Text _nameLabel;

        [SerializeField]
        private TMP_Text _tierLabel;

        [SerializeField]
        private TMP_Text _idLabel;

        private void Awake()
        {
            if (_frameRenderer == null || _nameLabel == null || _tierLabel == null || _idLabel == null)
            {
                Debug.LogError($"{name}: {nameof(WorldCardView)} is missing a frame renderer or label reference.", this);
            }
        }

        public void Show(Card card, RarityPaletteDefinition palette)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (palette == null) throw new ArgumentNullException(nameof(palette));

            _nameLabel.text = card.DisplayName;
            _nameLabel.color = palette.CardTextColor;
            _tierLabel.text = palette.DisplayNameOf(card.Tier);
            _tierLabel.color = palette.ColorOf(card.Tier);
            _idLabel.text = card.Id;
            _idLabel.color = palette.CardTextColor;

            Material frameMaterial = palette.MaterialOf(card.Tier);
            if (frameMaterial != null)
            {
                _frameRenderer.sharedMaterial = frameMaterial;
            }
        }
    }
}
