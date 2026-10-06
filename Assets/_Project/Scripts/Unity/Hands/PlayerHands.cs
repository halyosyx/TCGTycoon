using System;
using UnityEngine;

namespace Game.Unity.Hands
{
    /// <summary>
    /// The player's one hand slot. Holds at most one <see cref="IHoldable"/>, carried at a socket in
    /// front of the camera on the Held layer, which a dedicated overlay camera draws, so held items never
    /// clip through walls. Use (LMB) and Put down (F) are delegated to the holdable. Core already knows
    /// what is held (<c>ItemLocation.Held</c>); this only presents it.
    /// </summary>
    public sealed class PlayerHands : MonoBehaviour
    {
        [SerializeField, Tooltip("Child of the player camera where held items sit.")]
        private Transform _socket;

        [SerializeField, Tooltip("The player camera: where a dropped item is placed from.")]
        private Transform _view;

        [SerializeField, Range(0, 31), Tooltip("Layer held items render on (drawn by the held-items overlay camera only).")]
        private int _heldLayer = 7;

        private IHoldable _held;

        /// <summary>Raised with the new held item, or null when the hand empties.</summary>
        public event Action<IHoldable> HeldChanged;

        public bool IsEmpty => _held == null;

        /// <summary>The held item, or null.</summary>
        public IHoldable Held => _held;

        /// <summary>The player camera, which a pack being torn open is posed against.</summary>
        public Transform View => _view;

        private void Awake()
        {
            if (_socket == null || _view == null)
            {
                Debug.LogError($"{name}: {nameof(PlayerHands)} needs a Socket and a View.", this);
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

        private void Release()
        {
            _held = null;
            HeldChanged?.Invoke(null);
        }
    }
}
