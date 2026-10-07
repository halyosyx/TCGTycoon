using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.Cards;
using Game.Unity.Interaction;
using Game.Unity.UI.PackOpening;
using UnityEngine;

namespace Game.Unity.Hands
{
    /// <summary>
    /// One owned sealed booster pack as an object: in the hand (Core: <see cref="ItemLocation.Held"/>)
    /// or set down loose in the room (<see cref="ItemLocation.Placed"/>). Held, Use (LMB, "Open pack")
    /// hands it to the pack opening screen and Put down sets it on the surface in front of the player,
    /// falling back to the floor; set down, it can be taken again. Taking, putting down and the zoom never
    /// open it: it stays in the hand (Core: Held) until the screen commits the rip. While being opened it
    /// only shows the poses the screen gives it (<see cref="ITearablePack"/>). Built and pooled by
    /// <see cref="HoldableFactory"/>.
    /// </summary>
    public sealed class SealedBoosterPack : MonoBehaviour, IInteractable, IHoldable, ITearablePack
    {
        private const float WallClearance = 0.08f;
        private const float RestLift = 0.002f;

        private HoldableFactory _factory;
        private BoxCollider _collider;
        private RendererTint _tint;
        private BoosterPackView _view;
        private WorldCardView _firstCard;
        private bool _hasFirstCard;
        private bool _areCardsHidden;
        private Transform _socket;
        private int _heldLayer;
        private Vector3 _zoomFromPosition;
        private Quaternion _zoomFromRotation;
        private Vector3 _anchorPosition;
        private Quaternion _anchorRotation;
        private float _anchorScale;
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

        /// <param name="firstCard">A world card inside the model, shown face up on the stack once the rip commits.</param>
        public void Initialize(HoldableFactory factory, BoxCollider box, RendererTint tint, BoosterPackView view, WorldCardView firstCard)
        {
            _factory = factory;
            _collider = box;
            _tint = tint;
            _view = view;
            _firstCard = firstCard;
            if (_firstCard != null && _view != null)
            {
                _view.PlaceRevealCard(_firstCard.transform);
            }

            ClearFirstCard();
        }

        /// <summary>Gives a pooled pack its product. It starts out of the world until held or placed.</summary>
        public void Bind(string productId, string noun, Color colour)
        {
            _productId = productId;
            _noun = noun;
            _colour = colour;
            _isHeld = false;
            _tint.Set(_colour);
            transform.localScale = Vector3.one;
            ClearFirstCard();
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
            _socket = socket;
            _heldLayer = heldLayer;
            _isHeld = true;
            _tint.Set(_colour);
            _collider.enabled = false;
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = _factory.PackHeldRotation;
            HoldableFactory.SetLayer(gameObject, heldLayer);
        }

        /// <summary>
        /// Hands it to the pack opening screen (the zoom). Always false: the pack stays in the hand, and
        /// in Core, until the screen commits the rip and empties the hand itself.
        /// </summary>
        public bool TryUse()
        {
            if (_isHeld)
            {
                _factory.OpenHeldPack(this);
            }

            return false;
        }

        // --- Being opened (the pack opening screen drives all of this) ---

        public void BeginZoom(Transform camera, Vector3 anchorPosition, Quaternion anchorRotation, float anchorScale)
        {
            transform.SetParent(camera, true);
            _zoomFromPosition = transform.localPosition;
            _zoomFromRotation = transform.localRotation;
            _anchorPosition = anchorPosition;
            _anchorRotation = anchorRotation;
            _anchorScale = anchorScale;
        }

        public void ApplyPose(PackTearPose pose)
        {
            transform.localPosition = Vector3.Lerp(_zoomFromPosition, _anchorPosition, pose.ZoomBlend);
            transform.localRotation = Quaternion.Slerp(_zoomFromRotation, _anchorRotation, pose.ZoomBlend);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, _anchorScale, pose.ZoomBlend);
            bool areCardsVisible = pose.AreCardsVisible && !_areCardsHidden;
            if (_view != null)
            {
                _view.ShowOpen(pose.SeamTear, pose.FlapAngle, areCardsVisible);
            }

            SetFirstCardVisible(areCardsVisible && _hasFirstCard);
        }

        public void ShowFirstCard(Card card)
        {
            if (_firstCard == null || card == null)
            {
                return;
            }

            _firstCard.Show(card, _factory.Palette);
            _hasFirstCard = true;
        }

        public bool TryGetFirstCardEdges(out Vector3 topCentre, out Vector3 bottomCentre)
        {
            if (!_hasFirstCard || _firstCard == null)
            {
                topCentre = default;
                bottomCentre = default;
                return false;
            }

            float halfHeight = _factory.CardHeight * 0.5f;
            Transform card = _firstCard.transform;
            topCentre = card.TransformPoint(new Vector3(0f, halfHeight, 0f));
            bottomCentre = card.TransformPoint(new Vector3(0f, -halfHeight, 0f));
            return true;
        }

        public void HideCards()
        {
            _areCardsHidden = true;
            SetFirstCardVisible(false);
            if (_view != null)
            {
                _view.HideCards();
            }
        }

        public void ReturnToHand()
        {
            transform.localScale = Vector3.one;
            ClearFirstCard();
            if (_view != null)
            {
                _view.ResetSealed();
            }

            OnHeld(_socket, _heldLayer);
        }

        public void Discard()
        {
            _isHeld = false;
            transform.localScale = Vector3.one;
            ClearFirstCard();
            if (_view != null)
            {
                _view.ResetSealed();
            }

            _factory.ReleasePack(this);
        }

        private void ClearFirstCard()
        {
            _hasFirstCard = false;
            _areCardsHidden = false;
            SetFirstCardVisible(false);
        }

        private void SetFirstCardVisible(bool isVisible)
        {
            if (_firstCard != null && _firstCard.gameObject.activeSelf != isVisible)
            {
                _firstCard.gameObject.SetActive(isVisible);
            }
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
