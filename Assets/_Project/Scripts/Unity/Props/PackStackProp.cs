using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Unity.Interaction;
using TMPro;
using UnityEngine;

namespace Game.Unity.Props
{
    /// <summary>
    /// A stack of owned sealed packs of one product on the home table (BUYING_SELLING_SYSTEM §4): shows
    /// up to a few pack models in the set's colour and the count, tints on hover, and raises
    /// <see cref="PickedUp"/> on click; whoever listens opens the top pack. Built and pooled by
    /// <see cref="PackStackSpawner"/>; holds no pack rules (the inventory owns the count).
    /// </summary>
    public sealed class PackStackProp : MonoBehaviour, IInteractable
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        private List<GameObject> _layers;
        private List<Renderer> _renderers;
        private TextMeshPro _countLabel;
        private BoxCollider _collider;
        private MaterialPropertyBlock _propertyBlock;
        private float _layerHeight;
        private float _labelGap;
        private Color _colour;
        private Color _highlightColour;
        private string _promptVerb;
        private string _promptObject;

        /// <summary>Raised when the player clicks the stack.</summary>
        public event Action<PackStackProp> PickedUp;

        public string ProductId { get; private set; }

        public int Count { get; private set; }

        public bool CanInteract => Count > 0 && isActiveAndEnabled;

        public string PromptVerb => _promptVerb;

        public string PromptObject => _promptObject;

        /// <summary>Hands the stack its parts. Called once by the spawner that built it.</summary>
        /// <param name="layers">Pack models, bottom first; as many show as the count allows.</param>
        public void Initialize(List<GameObject> layers, TextMeshPro countLabel, BoxCollider stackCollider, float layerHeight, float labelGap)
        {
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _countLabel = countLabel;
            _collider = stackCollider;
            _layerHeight = layerHeight;
            _labelGap = labelGap;
            _propertyBlock = new MaterialPropertyBlock();
            _renderers = new List<Renderer>();
            foreach (GameObject layer in _layers)
            {
                // The pack's box is tinted; its printed label (TextMeshPro) keeps its own material.
                foreach (MeshRenderer meshRenderer in layer.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!meshRenderer.TryGetComponent(out TextMeshPro _))
                    {
                        _renderers.Add(meshRenderer);
                    }
                }
            }
        }

        /// <param name="countFormat">Count label, {0} = count (e.g. "×{0}").</param>
        public void Bind(string productId, int count, Color colour, Color highlightColour, string promptVerb, string promptObject, string countFormat)
        {
            ProductId = productId;
            Count = count;
            _colour = colour;
            _highlightColour = highlightColour;
            _promptVerb = promptVerb;
            _promptObject = promptObject;

            int visibleLayers = Mathf.Clamp(count, 1, _layers.Count);
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].SetActive(i < visibleLayers);
            }

            float height = visibleLayers * _layerHeight;
            if (_collider != null)
            {
                _collider.center = new Vector3(0f, height * 0.5f, 0f);
                _collider.size = new Vector3(_collider.size.x, Mathf.Max(height, _layerHeight), _collider.size.z);
            }

            if (_countLabel != null)
            {
                _countLabel.text = string.Format(CultureInfo.InvariantCulture, countFormat, count);
                _countLabel.transform.localPosition = new Vector3(0f, height + _labelGap, 0f);
            }

            Tint(_colour);
        }

        public void SetHovered(bool isHovered) => Tint(isHovered ? _highlightColour : _colour);

        public void Interact()
        {
            if (CanInteract)
            {
                PickedUp?.Invoke(this);
            }
        }

        private void Tint(Color colour)
        {
            if (_renderers == null)
            {
                return;
            }

            _propertyBlock.SetColor(s_baseColorId, colour);
            foreach (Renderer meshRenderer in _renderers)
            {
                meshRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
