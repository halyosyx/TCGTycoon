using UnityEngine;

namespace Game.Unity.Player
{
    /// <summary>
    /// Moves the player with a <see cref="CharacterController"/>: walking relative to the
    /// direction the player faces, plus gravity. Knows nothing about input devices.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        private const float MaxInputMagnitude = 1f;

        // A small constant downward speed while grounded keeps CharacterController.isGrounded
        // reliable; with zero vertical movement the controller loses contact with the floor.
        private const float GroundedVerticalSpeed = -2f;

        [SerializeField, Min(0f), Tooltip("Walking speed in metres per second.")]
        private float _walkSpeed = 4f;

        [SerializeField, Tooltip("Gravity in metres per second squared (negative is down).")]
        private float _gravity = -9.81f;

        private CharacterController _characterController;
        private float _verticalSpeed;

        /// <summary>True when the controller touched the ground during the last move.</summary>
        public bool IsGrounded => _characterController.isGrounded;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Private fields keep their values between Play sessions when scene reload is
            // disabled, so runtime state is reset explicitly.
            _verticalSpeed = 0f;
        }

        /// <summary>Moves the player for one frame.</summary>
        /// <param name="moveInput">Strafe (x) and forward (y), each from -1 to 1.</param>
        /// <param name="deltaTime">Seconds since the previous move.</param>
        public void Move(Vector2 moveInput, float deltaTime)
        {
            Vector2 clampedInput = Vector2.ClampMagnitude(moveInput, MaxInputMagnitude);
            Vector3 horizontalVelocity =
                (transform.right * clampedInput.x + transform.forward * clampedInput.y) * _walkSpeed;

            _verticalSpeed = _characterController.isGrounded && _verticalSpeed < 0f
                ? GroundedVerticalSpeed
                : _verticalSpeed + _gravity * deltaTime;

            // One Move call per frame: separate horizontal and vertical moves make isGrounded flicker.
            _characterController.Move((horizontalVelocity + Vector3.up * _verticalSpeed) * deltaTime);
        }
    }
}
