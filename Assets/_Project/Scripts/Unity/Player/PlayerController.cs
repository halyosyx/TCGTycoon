using System;
using Game.Unity.Interaction;
using UnityEngine;

namespace Game.Unity.Player
{
    /// <summary>
    /// Reads the player's input each frame and forwards it to <see cref="PlayerMotor"/>,
    /// <see cref="PlayerLook"/> and <see cref="PlayerInteractor"/>. Owns the cursor: locked while
    /// playing, freed by Cancel (Escape), locked again by Click. Screens switch gameplay input off
    /// while they are open (<see cref="SetGameplayInput"/>).
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerLook))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("Finds what the player is aiming at. Optional: without it the player can't interact.")]
        private PlayerInteractor _interactor;

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

        private static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        private void Awake()
        {
            _controls = new PlayerControls();
            _motor = GetComponent<PlayerMotor>();
            _look = GetComponent<PlayerLook>();

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
                // would pick the pack straight back up, and Escape would free the cursor again.
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
            if (_isWaitingForClickRelease && !actions.Click.IsPressed())
            {
                _isWaitingForClickRelease = false;
            }

            if (actions.Cancel.WasPressedThisFrame())
            {
                SetCursorLocked(false);
                ClearHover();
            }
            else if (!IsCursorLocked && actions.Click.WasPressedThisFrame())
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

            if (_interactor != null)
            {
                _interactor.UpdateHover();
                if (!_isWaitingForClickRelease && actions.Click.WasPressedThisFrame())
                {
                    _interactor.TryInteract();
                }
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
