using UnityEngine;

namespace Game.Unity.Player
{
    /// <summary>
    /// Reads the player's input each frame and forwards it to <see cref="PlayerMotor"/> and
    /// <see cref="PlayerLook"/>. Owns the cursor: locked while playing, freed by Cancel
    /// (Escape), locked again by Click.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerLook))]
    public sealed class PlayerController : MonoBehaviour
    {
        private PlayerControls _controls;
        private PlayerMotor _motor;
        private PlayerLook _look;

        private static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        private void Awake()
        {
            _controls = new PlayerControls();
            _motor = GetComponent<PlayerMotor>();
            _look = GetComponent<PlayerLook>();
        }

        private void OnEnable()
        {
            _controls.Player.Enable();
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            _controls.Player.Disable();

            // Also runs when leaving Play Mode, so the cursor is never left captured.
            SetCursorLocked(false);
        }

        private void OnDestroy()
        {
            _controls.Dispose();
        }

        private void Update()
        {
            PlayerControls.PlayerActions actions = _controls.Player;

            if (actions.Cancel.WasPressedThisFrame())
            {
                SetCursorLocked(false);
            }
            else if (!IsCursorLocked && actions.Click.WasPressedThisFrame())
            {
                SetCursorLocked(true);
            }

            _motor.Move(actions.Move.ReadValue<Vector2>(), Time.deltaTime);

            // PlayerLook disables itself when misconfigured; skip it rather than throw every frame.
            if (IsCursorLocked && _look.enabled)
            {
                _look.Look(actions.Look.ReadValue<Vector2>());
            }
        }

        private static void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }
    }
}
