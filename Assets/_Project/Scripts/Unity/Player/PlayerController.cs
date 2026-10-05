using System;
using Game.Unity.Hands;
using Game.Unity.Interaction;
using UnityEngine;

namespace Game.Unity.Player
{
    /// <summary>
    /// Reads the player's input each frame and forwards it to <see cref="PlayerMotor"/>,
    /// <see cref="PlayerLook"/>, <see cref="PlayerInteractor"/> and <see cref="PlayerHands"/>. The verbs
    /// are fixed: Interact (E) acts on what is aimed at, Use (LMB) uses what is held, Drop (F) puts it
    /// down. Owns the cursor: locked while playing, freed by Cancel (Escape), locked again by Use.
    /// Screens switch gameplay input off while they are open (<see cref="SetGameplayInput"/>); that one
    /// flag is what suppresses all world interaction while any full-screen UI is up.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerLook))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("Finds what the player is aiming at. Optional: without it the player can't interact.")]
        private PlayerInteractor _interactor;

        [SerializeField, Tooltip("The player's hand slot. Interaction and holding need it.")]
        private PlayerHands _hands;

        private InteractionContext _context;
        private PlayerControls _controls;
        private PlayerMotor _motor;
        private PlayerLook _look;
        private bool _isInGameplay;
        private bool _isWaitingForClickRelease;
        private int _gameplayResumedFrame;

        /// <summary>Raised with the new value whenever <see cref="IsInGameplay"/> changes.</summary>
        public event Action<bool> GameplayInputChanged;

        /// <summary>
        /// True while the player walks and interacts; false while a screen has taken over input.
        /// Screens open only while this is true, so two screens can never be open at once.
        /// </summary>
        public bool IsInGameplay => _isInGameplay;

        /// <summary>The interactor this controller drives, or null when the player can't interact.</summary>
        public PlayerInteractor Interactor => _interactor;

        /// <summary>The player's hand slot, or null when the player can't hold anything.</summary>
        public PlayerHands Hands => _hands;

        private static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        private void Awake()
        {
            _controls = new PlayerControls();
            _motor = GetComponent<PlayerMotor>();
            _look = GetComponent<PlayerLook>();
            if (_hands == null)
            {
                Debug.LogError($"{name}: {nameof(PlayerController)} needs Player Hands; the player can't interact or hold anything.", this);
            }

            _context = _hands == null ? null : new InteractionContext(_hands);

            // Private fields survive between Play sessions when scene reload is disabled.
            _isInGameplay = true;
            _isWaitingForClickRelease = false;
            _gameplayResumedFrame = -1;
        }

        private void OnEnable()
        {
            if (_isInGameplay)
            {
                _controls.Player.Enable();
                SetCursorLocked(true);
            }
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
            ClearHover();

            // Also runs when leaving Play Mode, so the cursor is never left captured.
            SetCursorLocked(false);
        }

        private void OnDestroy()
        {
            _controls.Dispose();
        }

        /// <summary>
        /// Switches walking, looking and interacting on or off. Off frees the cursor for a screen;
        /// on captures it again.
        /// </summary>
        public void SetGameplayInput(bool isEnabled)
        {
            if (_isInGameplay == isEnabled)
            {
                return;
            }

            _isInGameplay = isEnabled;
            if (isEnabled)
            {
                _controls.Player.Enable();
                SetCursorLocked(true);

                // The click or key that closed the screen must not also act in gameplay: a held click
                // would use whatever is in the hand, and Escape would free the cursor again.
                _isWaitingForClickRelease = true;
                _gameplayResumedFrame = Time.frameCount;
            }
            else
            {
                _controls.Player.Disable();
                ClearHover();
                SetCursorLocked(false);
            }

            GameplayInputChanged?.Invoke(isEnabled);
        }

        private void Update()
        {
            if (!_isInGameplay || Time.frameCount == _gameplayResumedFrame)
            {
                return;
            }

            PlayerControls.PlayerActions actions = _controls.Player;
            if (_isWaitingForClickRelease && !actions.Use.IsPressed())
            {
                _isWaitingForClickRelease = false;
            }

            if (actions.Cancel.WasPressedThisFrame())
            {
                SetCursorLocked(false);
                ClearHover();
            }
            else if (!IsCursorLocked && actions.Use.WasPressedThisFrame())
            {
                SetCursorLocked(true);
                _isWaitingForClickRelease = true;
            }

            _motor.Move(actions.Move.ReadValue<Vector2>(), Time.deltaTime);

            if (!IsCursorLocked)
            {
                return;
            }

            // PlayerLook disables itself when misconfigured; skip it rather than throw every frame.
            if (_look.enabled)
            {
                _look.Look(actions.Look.ReadValue<Vector2>());
            }

            if (_context == null)
            {
                return;
            }

            if (_interactor != null)
            {
                _interactor.UpdateHover(_context);
                if (actions.Interact.WasPressedThisFrame())
                {
                    _interactor.TryInteract(_context);
                }
            }

            // E and LMB stay separate: you can hold a pack while looking at something else.
            if (!_isWaitingForClickRelease && actions.Use.WasPressedThisFrame())
            {
                _hands.UseHeld();
            }
            else if (actions.Drop.WasPressedThisFrame())
            {
                _hands.DropHeld();
            }
        }

        private void ClearHover()
        {
            if (_interactor != null)
            {
                _interactor.ClearHover();
            }
        }

        private static void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }
    }
}
