using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Inventory;
using Game.Core.Store;
using Game.Unity.Definitions;
using Game.Unity.Hands;
using Game.Unity.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Unity.Props
{
    /// <summary>
    /// Keeps one <see cref="PackStackProp"/> per sealed booster product stored at home (Core:
    /// <see cref="ItemLocation.Binder"/>) on the home table, in purchase order, rebuilt from the
    /// inventory's Changed event: buying 12 Champions packs shows one stack of 12, taking one leaves 11,
    /// an empty stack disappears. Taking from a stack (E) moves one pack into the hand (Binder to Held)
    /// as a <see cref="SealedBoosterPack"/>. Stacks are pooled. Sits on the table, at the centre of its
    /// top surface.
    /// </summary>
    public sealed class PackStackSpawner : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField, Tooltip("The pack model (Data/Generated/Prefabs/BoosterPack). Laid flat, one per layer.")]
        private GameObject _packModel;

        [SerializeField, Min(1), Tooltip("Most pack models a stack shows; the label carries the real count.")]
        private int _maxLayers = 6;

        [SerializeField, Min(0.001f), Tooltip("Height of one pack layer, in metres.")]
        private float _layerHeight = 0.009f;

        [SerializeField, Tooltip("Size of a stack's click box (x, z), in metres.")]
        private Vector2 _footprint = new Vector2(0.09f, 0.14f);

        [SerializeField, Tooltip("First stack's position, relative to this object (the table top's centre).")]
        private Vector3 _firstSlot = new Vector3(-0.5f, 0f, 0f);

        [SerializeField, Tooltip("Offset from one stack to the next.")]
        private Vector3 _slotStep = new Vector3(0.2f, 0f, 0f);

        [SerializeField, Tooltip("Base colour of a stack while hovered.")]
        private Color _highlightColor = new Color(0.55f, 0.45f, 0.85f);

        [Header("Count label")]
        [SerializeField, Min(0.01f)]
        private float _labelFontSize = 0.6f;

        [SerializeField, Min(0f), Tooltip("Gap between the top pack and the count, in metres.")]
        private float _labelGap = 0.03f;

        [SerializeField]
        private Color _labelColor = Color.white;

        [Header("Text")]
        [SerializeField, Tooltip("Count label; {0} = packs left.")]
        private string _countFormat = "×{0}";

        [SerializeField, Tooltip("HUD prompt verb while aiming at a stack.")]
        private string _promptVerb = "Take";

        [SerializeField, Tooltip("HUD prompt object; {0} = set short name, {1} = product type name.")]
        private string _promptObjectFormat = "{0} pack";

        private InventoryService _inventory;
        private StoreConfigDefinition _store;
        private HoldableFactory _holdables;
        private ObjectPool<PackStackProp> _pool;
        private List<PackStackProp> _stacks;

        private void Awake()
        {
            if (_packModel == null)
            {
                Debug.LogError($"{name}: {nameof(PackStackSpawner)} needs a Pack Model.", this);
            }

            // Private fields survive between Play sessions when scene reload is disabled.
            _inventory = null;
            _store = null;
            _holdables = null;
            _stacks = new List<PackStackProp>();
            _pool = new ObjectPool<PackStackProp>(CreateStack, stack => stack.gameObject.SetActive(true), stack => stack.gameObject.SetActive(false), defaultCapacity: 4);
        }

        /// <summary>Starts following the inventory. Called once by <c>GameBootstrap</c>.</summary>
        /// <param name="store">Names and colours each booster product (its card set's colour).</param>
        /// <param name="holdables">Makes the pack object a taken pack becomes in the hand.</param>
        public void Initialize(InventoryService inventory, StoreConfigDefinition store, HoldableFactory holdables)
        {
            if (inventory == null || store == null || holdables == null || _packModel == null)
            {
                Debug.LogError($"{name}: {nameof(PackStackSpawner)} needs the inventory, the store config, the holdables and a Pack Model.", this);
                return;
            }

            _inventory = inventory;
            _store = store;
            _holdables = holdables;
            _inventory.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            foreach (PackStackProp stack in _stacks)
            {
                _pool.Release(stack);
            }

            _stacks.Clear();
            foreach (SealedStack sealedStack in _inventory.SealedStacks)
            {
                ProductDefinition product = sealedStack.Location == ItemLocation.Binder && sealedStack.Count > 0 ? _store.FindProduct(sealedStack.ProductId) : null;
                if (product == null || product.Type != ProductType.BoosterPack)
                {
                    continue;
                }

                PackStackProp stack = _pool.Get();
                stack.transform.localPosition = _firstSlot + _slotStep * _stacks.Count;
                CardSetDefinition set = product.CardSet;
                string objectName = string.Format(CultureInfo.InvariantCulture, _promptObjectFormat, set == null ? string.Empty : set.ShortName, product.TypeDisplayName);
                stack.Bind(product.Id, sealedStack.Count, set == null ? Color.gray : set.Colour, _highlightColor, _promptVerb, objectName, _countFormat);
                _stacks.Add(stack);
            }
        }

        // Core first: the pack moves into the hand location, then its object goes into the hand.
        private void OnTakeRequested(PackStackProp stack, InteractionContext context)
        {
            if (!context.Hands.IsEmpty)
            {
                return;
            }

            ItemRef pack = ItemRef.Sealed(stack.ProductId);
            if (!_inventory.Move(pack, ItemLocation.Binder, ItemLocation.Held).IsSuccess)
            {
                return;
            }

            SealedBoosterPack held = _holdables.GetPack(stack.ProductId);
            if (!context.Hands.Hold(held))
            {
                // Can't happen with an empty hand; undo so Core never says Held without a held object.
                _holdables.ReleasePack(held);
                _inventory.Move(pack, ItemLocation.Held, ItemLocation.Binder);
            }
        }

        // A stack is built once from the pack model and reused: a click box, the layers, the count.
        private PackStackProp CreateStack()
        {
            var root = new GameObject("PackStack");
            root.transform.SetParent(transform, false);
            root.layer = _holdables.InteractableLayer;

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(_footprint.x, _layerHeight, _footprint.y);

            var layers = new List<GameObject>(_maxLayers);
            for (int i = 0; i < _maxLayers; i++)
            {
                // Lying flat, face up; each layer a pack's thickness above the last.
                GameObject layer = Instantiate(_packModel, root.transform);
                layer.transform.localPosition = new Vector3(0f, (i + 0.5f) * _layerHeight, 0f);
                layer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                foreach (Collider modelCollider in layer.GetComponentsInChildren<Collider>(true))
                {
                    modelCollider.enabled = false;   // the stack's own box takes the clicks
                }

                layers.Add(layer);
            }

            var labelObject = new GameObject("Count");
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.AddComponent<TextMeshPro>();
            label.fontSize = _labelFontSize;
            label.color = _labelColor;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(0.3f, 0.1f);

            var stack = root.AddComponent<PackStackProp>();
            stack.Initialize(layers, label, box, _layerHeight, _labelGap);
            stack.TakeRequested += OnTakeRequested;
            return stack;
        }
    }
}
