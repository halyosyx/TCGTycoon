using System;
using Game.Unity.Interaction;
using UnityEngine;

namespace Game.Unity.Props
{
    /// <summary>
    /// A sealed pack sitting in the room. Tints on hover and raises <see cref="PickedUp"/> on click;
    /// whoever listens decides what opening means. Holds no pack rules.
    /// </summary>
    public sealed class PackProp : MonoBehaviour, IInteractable
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Visible pack model; hidden while the pack is open.")]
        private GameObject _visualRoot;

        [SerializeField, Tooltip("Renderer tinted while the player aims at the pack.")]
        private Renderer _highlightRenderer;

        [SerializeField, Tooltip("Base colour of the pack while hovered.")]
        private Color _highlightColor = new Color(0.55f, 0.45f, 0.85f);

        private MaterialPropertyBlock _propertyBlock;
        private bool _isAvailable;

        /// <summary>Raised when the player clicks the pack.</summary>
        public event Action<PackProp> PickedUp;

        public bool CanInteract => _isAvailable && isActiveAndEnabled;

        private void Awake()
        {
            if (_visualRoot == null || _highlightRenderer == null)
            {
                Debug.LogError($"{name}: {nameof(PackProp)} needs a Visual Root and a Highlight Renderer.", this);
            }

            _propertyBlock = new MaterialPropertyBlock();

            // Private fields survive between Play sessions when scene reload is disabled.
            _isAvailable = true;
            SetHovered(false);
        }

        public void SetHovered(bool isHovered)
        {
            if (_highlightRenderer == null)
            {
                return;
            }

            if (isHovered)
            {
                _propertyBlock.SetColor(s_baseColorId, _highlightColor);
                _highlightRenderer.SetPropertyBlock(_propertyBlock);
            }
            else
            {
                // An empty block restores the shared material's own colour.
                _highlightRenderer.SetPropertyBlock(null);
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            PickedUp?.Invoke(this);
        }

        /// <summary>Hides the pack while its contents are on screen.</summary>
        public void Hide()
        {
            _isAvailable = false;
            SetHovered(false);
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(false);
            }
        }

        /// <summary>Puts a fresh pack back on the surface.</summary>
        public void Show()
        {
            _isAvailable = true;
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(true);
            }
        }
    }
}
