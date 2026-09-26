using UnityEngine;

namespace Game.Unity.Player
{
    /// <summary>
    /// First-person mouse look: yaw turns the player root, pitch tilts the camera.
    /// Also places the camera at eye height. Knows nothing about input devices.
    /// </summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField, Tooltip("Camera that tilts up and down; a child of this object.")]
        private Transform _cameraTransform;

        [SerializeField, Min(0f), Tooltip("Degrees of rotation per pixel of mouse movement.")]
        private float _mouseSensitivity = 0.1f;

        [SerializeField, Range(0f, 89f), Tooltip("How far the camera can tilt up or down, in degrees.")]
        private float _maxPitch = 85f;

        [SerializeField, Min(0f), Tooltip("Camera height above the player's feet, in metres.")]
        private float _eyeHeight = 1.6f;

        private float _pitch;

        private void Awake()
        {
            if (_cameraTransform == null)
            {
                Debug.LogError($"{name}: Camera Transform is not assigned on {nameof(PlayerLook)}.", this);
                enabled = false;
                return;
            }

            // Private fields keep their values between Play sessions when scene reload is
            // disabled, so the view is reset explicitly.
            _pitch = 0f;
            _cameraTransform.localPosition = Vector3.up * _eyeHeight;
            _cameraTransform.localRotation = Quaternion.identity;
        }

        /// <summary>Rotates the view by one frame of mouse movement.</summary>
        /// <param name="lookDelta">Mouse movement in pixels since the previous frame.</param>
        public void Look(Vector2 lookDelta)
        {
            // Mouse delta is already the distance moved this frame, so it isn't scaled by deltaTime.
            transform.Rotate(0f, lookDelta.x * _mouseSensitivity, 0f);

            _pitch = Mathf.Clamp(_pitch - lookDelta.y * _mouseSensitivity, -_maxPitch, _maxPitch);
            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
