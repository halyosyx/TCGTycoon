using Game.Core.Inventory;
using Game.Unity.Cards;
using Game.Unity.Interaction;
using Game.Unity.UI.PackOpening;
using UnityEngine;

namespace Game.Unity.Hands
{
    /// <summary>
    /// One owned sealed booster pack as an object: in the hand (Core: <see cref="ItemLocation.Held"/>)
    /// or set down loose in the room (<see cref="ItemLocation.Placed"/>). Held, Use (LMB) starts tearing
    /// it open and Put down sets it on the surface in front of the player, falling back to the floor;
    /// set down, it can be taken again. Taking and putting down never open it. While torn it is only a
    /// wrapper the pack opening screen poses (<see cref="ITearablePack"/>): its cards are already owned.
    /// Built and pooled by <see cref="HoldableFactory"/>.
    /// </summary>
    public sealed class SealedBoosterPack : MonoBehaviour, IInteractable, IHoldable, ITearablePack
    {
        private const float WallClearance = 0.08f;
        private const float RestLift = 0.002f;

        private HoldableFactory _factory;
        private BoxCollider _collider;
        private RendererTint _tint;
        private BoosterPackView _view;
        private Vector3 _tearStartPosition;
        private Quaternion _tearStartRotation;
        private Vector3 _tearPosition;
        private Quaternion _tearRotation;
        private string _productId;
        private string _noun;
        private Color _colour;
        private bool _isHeld;

        public string ProductId => _productId;

        public string Noun => _noun;

        public int Count => 1;

        public string UseVerb => _factory.OpenVerb;

        public bool CanUse => _isHeld;

        public string DropVerb => _factory.PutDownVerb;

        public string PromptVerb => _factory.TakeVerb;

        public string PromptObject => _noun;

        public void Initialize(HoldableFactory factory, BoxCollider box, RendererTint tint, BoosterPackView view)
        {
            _factory = factory;
            _collider = box;
            _tint = tint;
            _view = view;
        }

        /// <summary>Gives a pooled pack its product. It starts out of the world until held or placed.</summary>
        public void Bind(string productId, string noun, Color colour)
        {
            _productId = productId;
            _noun = noun;
            _colour = colour;
            _isHeld = false;
            _tint.Set(_colour);
            if (_view != null)
            {
                _view.ResetSealed();
            }
        }

        // --- Set down in the room: E takes it back ---

        public bool CanInteract(InteractionContext context) => !_isHeld && isActiveAndEnabled && context.Hands.IsEmpty;

        public void SetHovered(bool isHovered) => _tint.Set(isHovered ? _factory.HighlightColor : _colour);

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
            {
                return;
            }

            MoveResult result = _factory.Inventory.Move(ItemRef.Sealed(_productId), ItemLocation.Placed, ItemLocation.Held);
            if (result.IsSuccess)
            {
                context.Hands.Hold(this);
            }
        }

        // --- In the hand ---

        public void OnHeld(Transform socket, int heldLayer)
        {
            _isHeld = true;
            _tint.Set(_colour);
            _collider.enabled = false;
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = _factory.PackHeldRotation;
            HoldableFactory.SetLayer(gameObject, heldLayer);
        }

        /// <summary>
        /// Starts tearing it open: Core takes the held pack and adds its cards at once, then the screen
        /// animates this wrapper and returns it to the pool when the tear ends. It leaves the hand.
        /// </summary>
        public bool TryUse()
        {
            if (!_isHeld || !_factory.OpenHeldPack(this))
            {
                return false;
            }

            _isHeld = false;
            return true;
        }

        // --- Being torn open (the pack opening screen drives this) ---

        public void BeginTear(Transform camera, Vector3 tearPosition, Quaternion tearRotation)
        {
            transform.SetParent(camera, true);
            _tearStartPosition = transform.localPosition;
            _tearStartRotation = transform.localRotation;
            _tearPosition = tearPosition;
            _tearRotation = tearRotation;
        }

        public void ApplyTear(PackTearPose pose)
        {
            transform.localPosition = Vector3.Lerp(_tearStartPosition, _tearPosition, pose.PoseBlend);
            transform.localRotation = Quaternion.Slerp(_tearStartRotation, _tearRotation, pose.PoseBlend);
            if (_view != null)
            {
                _view.ShowTear(pose.StripAngle, pose.StripOffset, pose.IsStripVisible, pose.AreCardsVisible, pose.CardsRise);
            }
        }

        public void Discard()
        {
            if (_view != null)
            {
                _view.ResetSealed();
            }

            _factory.ReleasePack(this);
        }

        /// <summary>
        /// Sets it down on the surface in front of the player (never through a wall), or on the floor
        /// under them when there is nothing in front. It stays owned (Core: Placed) and can be taken again.
        /// </summary>
        public bool TryDrop(DropContext context)
        {
            if (!_isHeld || !FindRestingPoint(context, out Vector3 point, out float yaw))
            {
                return false;
            }

            MoveResult result = _factory.Inventory.Move(ItemRef.Sealed(_productId), ItemLocation.Held, ItemLocation.Placed);
            if (!result.IsSuccess)
            {
                return false;
            }

            _isHeld = false;
            transform.SetParent(_factory.transform, true);
            transform.SetPositionAndRotation(point + Vector3.up * (_factory.PackSize.z * 0.5f + RestLift), Quaternion.Euler(90f, yaw, 0f));
            HoldableFactory.SetLayer(gameObject, _factory.InteractableLayer);
            _collider.enabled = true;
            return true;
        }

        private bool FindRestingPoint(DropContext context, out Vector3 point, out float yaw)
        {
            Vector3 forward = Vector3.ProjectOnPlane(context.View.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(context.Body.forward, Vector3.up);
            }

            forward.Normalize();
            yaw = Quaternion.LookRotation(forward).eulerAngles.y;

            // In front, but short of any wall or furniture side the reach would pass through.
            Vector3 eye = context.View.position;
            Vector3 above = eye + forward * _factory.DropReach;
            if (Physics.Raycast(eye, forward, out RaycastHit blocker, _factory.DropReach + WallClearance, _factory.Surfaces, QueryTriggerInteraction.Ignore))
            {
                above = blocker.point - forward * WallClearance;
            }

            if (Physics.Raycast(above, Vector3.down, out RaycastHit surface, _factory.DropSearchDepth, _factory.Surfaces, QueryTriggerInteraction.Ignore))
            {
                point = surface.point;
                return true;
            }

            // Fallback: the floor under the player.
            Vector3 overFeet = context.Body.position + Vector3.up * 0.5f;
            if (Physics.Raycast(overFeet, Vector3.down, out RaycastHit floor, _factory.DropSearchDepth, _factory.Surfaces, QueryTriggerInteraction.Ignore))
            {
                point = floor.point;
                return true;
            }

            point = default;
            return false;
        }
    }
}
