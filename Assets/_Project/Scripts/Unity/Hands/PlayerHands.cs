using System;
using UnityEngine;

namespace Game.Unity.Hands
{
    /// <summary>
    /// The player's one hand slot. Holds at most one <see cref="IHoldable"/>, carried at a socket in
    /// front of the camera on the Held layer, which a dedicated overlay camera draws, so held items never
    /// clip through walls. Use (LMB) and Put down (F) are delegated to the holdable. Core already knows
    /// what is held (<c>ItemLocation.Held</c>); this only presents it. Held items read close because the
    /// overlay camera's field of view is narrower than the player camera's, not because they sit nearer
    /// the lens (that would distort and clip their corners); the socket offset keeps them at the same
    /// place on screen.
    /// </summary>
    public sealed class PlayerHands : MonoBehaviour
    {
        [SerializeField, Tooltip("Child of the player camera where held items sit.")]
        private Transform _socket;

        [SerializeField, Tooltip("The player camera: where a dropped item is placed from.")]
        private Transform _view;

        [SerializeField, Range(0, 31), Tooltip("Layer held items render on (drawn by the held-items overlay camera only).")]
        private int _heldLayer = 7;

        [Header("Apparent size")]
        [SerializeField, Tooltip("The overlay camera that draws held items (PlayerCamera/HeldItemsCamera).")]
        private Camera _heldCamera;

        [SerializeField, Range(10f, 90f), Tooltip("Field of view of the held-items camera. Lower makes held items look bigger without moving them toward the lens.")]
        private float _heldFieldOfView = 45f;

        [SerializeField, Tooltip("Where the hand socket sits in front of the player camera, in metres (x right, y up, z forward). When you change the field of view, scale x and y by tan(new/2) / tan(old/2) to keep the item at the same place on screen.")]
        private Vector3 _socketOffset = new Vector3(0.115f, -0.086f, 0.45f);

        private IHoldable _held;

        /// <summary>Raised with the new held item, or null when the hand empties.</summary>
        public event Action<IHoldable> HeldChanged;

        public bool IsEmpty => _held == null;

        /// <summary>The held item, or null.</summary>
        public IHoldable Held => _held;

        /// <summary>The player camera, which a pack being opened is posed against.</summary>
        public Transform View => _view;

        /// <summary>Field of view of the camera that draws held items (and a pack being opened).</summary>
        public float HeldFieldOfView => _heldFieldOfView;

        /// <summary>The camera that draws held items (and a pack being opened), for projecting them onto the screen.</summary>
        public Camera HeldCamera => _heldCamera;

        private void Awake()
        {
            if (_socket == null || _view == null)
            {
                Debug.LogError($"{name}: {nameof(PlayerHands)} needs a Socket and a View.", this);
            }
            else
            {
                _socket.localPosition = _socketOffset;
            }

            if (_heldCamera == null)
            {
                Debug.LogError($"{name}: {nameof(PlayerHands)} needs the Held Camera to set its field of view.", this);
            }
            else
            {
                _heldCamera.fieldOfView = _heldFieldOfView;
            }

            // Private fields survive between Play sessions when scene reload is disabled.
            _held = null;
        }

        /// <summary>Takes an item into the empty hand. False when the hand is already full.</summary>
        public bool Hold(IHoldable holdable)
        {
            if (holdable == null || _held != null)
            {
                return false;
            }

            _held = holdable;
            holdable.OnHeld(_socket, _heldLayer);
            HeldChanged?.Invoke(_held);
            return true;
        }

        /// <summary>Uses the held item (LMB). False when nothing usable is held.</summary>
        public bool UseHeld()
        {
            if (_held == null || !_held.CanUse)
            {
                return false;
            }

            if (_held.TryUse())
            {
                Release();
            }

            return true;
        }

        /// <summary>Puts the held item down (F), however that item puts itself down.</summary>
        public bool DropHeld()
        {
            if (_held == null)
            {
                return false;
            }

            if (!_held.TryDrop(new DropContext(_view, transform)))
            {
                return false;
            }

            Release();
            return true;
        }

        /// <summary>
        /// Empties the hand when the held item has already left by another way (the last card placed in
        /// the display case or put back in the binder). The caller returns the object to its pool.
        /// </summary>
        public void Clear()
        {
            if (_held != null)
            {
                Release();
            }
        }

        private void Release()
        {
            _held = null;
            HeldChanged?.Invoke(null);
        }
    }
}
