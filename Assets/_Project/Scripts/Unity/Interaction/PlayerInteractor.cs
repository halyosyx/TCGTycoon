using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Unity.Interaction
{
    /// <summary>
    /// Finds the <see cref="IInteractable"/> under the centre of the view with one raycast per frame
    /// and tracks hover. The ray stops at the first collider on an occluder layer (walls, the table), so
    /// nothing is reachable through them, and the hit counts only when that collider is on an
    /// interactable layer. Knows nothing about input devices: <c>PlayerController</c> decides when to
    /// update and when Interact fires; it is the only raycast path for world interaction.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Tooltip("Camera the ray is cast from, through the centre of the view.")]
        private Transform _viewTransform;

        [SerializeField, Min(0f), Tooltip("How far away, in metres, the player can reach.")]
        private float _maxDistance = 2.5f;

        [SerializeField, FormerlySerializedAs("_layers"), Tooltip("Layers that stop the ray: world geometry plus the interactable layer.")]
        private LayerMask _occluders = ~0;

        [SerializeField, Tooltip("Layers whose colliders can be interacted with (the Interactable layer).")]
        private LayerMask _interactable;

        private IInteractable _hovered;
        private Collider _lastHitCollider;
        private IInteractable _lastHitInteractable;

        /// <summary>The object currently aimed at, or null.</summary>
        public IInteractable Hovered => _hovered;

        /// <summary>Raised with the newly aimed-at object, or null when the aim leaves it.</summary>
        public event Action<IInteractable> HoveredChanged;

        private void Awake()
        {
            if (_viewTransform == null)
            {
                Debug.LogError($"{name}: View Transform is not assigned on {nameof(PlayerInteractor)}.", this);
                enabled = false;
            }

            // Private fields survive between Play sessions when scene reload is disabled.
            _hovered = null;
            _lastHitCollider = null;
            _lastHitInteractable = null;
        }

        private void OnDisable() => ClearHover();

        /// <summary>Re-aims and updates hover. Call once per frame while the player can interact.</summary>
        public void UpdateHover(InteractionContext context)
        {
            if (!enabled)
            {
                return;
            }

            IInteractable target = FindTarget();
            if (target != null && !target.CanInteract(context))
            {
                target = null;
            }

            SetHovered(target);
        }

        /// <summary>Interacts with the hovered object. Returns false when nothing usable is aimed at.</summary>
        public bool TryInteract(InteractionContext context)
        {
            if (!IsAlive(_hovered) || !_hovered.CanInteract(context))
            {
                return false;
            }

            IInteractable target = _hovered;
            ClearHover();
            target.Interact(context);
            return true;
        }

        public void ClearHover() => SetHovered(null);

        private IInteractable FindTarget()
        {
            var ray = new Ray(_viewTransform.position, _viewTransform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _occluders | _interactable, QueryTriggerInteraction.Ignore)
                || (_interactable.value & (1 << hit.collider.gameObject.layer)) == 0)
            {
                return null;
            }

            // The component lookup runs only when the aim moves onto a different collider.
            if (hit.collider != _lastHitCollider)
            {
                _lastHitCollider = hit.collider;
                _lastHitInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }

            return _lastHitInteractable;
        }

        private void SetHovered(IInteractable target)
        {
            if (ReferenceEquals(target, _hovered))
            {
                return;
            }

            IInteractable previous = _hovered;
            _hovered = target;
            if (IsAlive(previous))
            {
                previous.SetHovered(false);
            }

            if (target != null)
            {
                target.SetHovered(true);
            }

            HoveredChanged?.Invoke(target);
        }

        // Interactables are MonoBehaviours; a destroyed one only looks null to Unity's == operator.
        private static bool IsAlive(IInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            return !(interactable is UnityEngine.Object unityObject) || unityObject != null;
        }
    }
}
