using UnityEngine;

namespace Game.Unity.Interaction
{
    /// <summary>
    /// Finds the <see cref="IInteractable"/> under the centre of the view with one raycast per frame
    /// and tracks hover. Knows nothing about input devices: <c>PlayerController</c> decides when to
    /// update and when a click interacts.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Tooltip("Camera the ray is cast from, through the centre of the view.")]
        private Transform _viewTransform;

        [SerializeField, Min(0f), Tooltip("How far away, in metres, the player can reach.")]
        private float _maxDistance = 2.5f;

        [SerializeField, Tooltip("Layers that can block or receive the ray.")]
        private LayerMask _layers = ~0;

        private IInteractable _hovered;
        private Collider _lastHitCollider;
        private IInteractable _lastHitInteractable;

        /// <summary>The object currently aimed at, or null.</summary>
        public IInteractable Hovered => _hovered;

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
        public void UpdateHover()
        {
            if (!enabled)
            {
                return;
            }

            IInteractable target = FindTarget();
            if (target != null && !target.CanInteract)
            {
                target = null;
            }

            SetHovered(target);
        }

        /// <summary>Interacts with the hovered object. Returns false when nothing usable is aimed at.</summary>
        public bool TryInteract()
        {
            if (_hovered == null || !_hovered.CanInteract)
            {
                return false;
            }

            IInteractable target = _hovered;
            ClearHover();
            target.Interact();
            return true;
        }

        public void ClearHover() => SetHovered(null);

        private IInteractable FindTarget()
        {
            var ray = new Ray(_viewTransform.position, _viewTransform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _layers, QueryTriggerInteraction.Ignore))
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
        }

        // Interactables are MonoBehaviours; a destroyed one only looks null to Unity's == operator.
        private static bool IsAlive(IInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            return !(interactable is Object unityObject) || unityObject != null;
        }
    }
}
