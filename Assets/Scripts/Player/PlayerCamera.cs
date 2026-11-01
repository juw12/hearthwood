using System;
using Hearthwood.Core;
using Hearthwood.Input;
using UnityEngine;

namespace Hearthwood.Player
{
    /// <summary>
    /// Third-person orbit camera. Attaches to a <see cref="Camera"/> GameObject and follows
    /// <see cref="Target"/> (the player) with smoothed position updates in <c>LateUpdate</c>.
    /// Mouse orbit via <see cref="InputManager.Look"/> controls yaw/pitch. Look input is
    /// ignored when the game state is not <see cref="GameState.Exploring"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField, Tooltip("The player transform to follow. Assign in the inspector or via code.")]
        private Transform _target;

        [Header("Camera Offset")]
        [SerializeField, Tooltip("Base offset from target (rotated by orbit angles). Default: behind and above.")]
        private Vector3 _offset = new Vector3(0f, 2f, -4f);

        [Header("Mouse Orbit")]
        [SerializeField, Tooltip("Horizontal mouse sensitivity (degrees per input unit).")]
        private float _mouseSensitivityX = 2.5f;

        [SerializeField, Tooltip("Vertical mouse sensitivity (degrees per input unit).")]
        private float _mouseSensitivityY = 2.5f;

        [SerializeField, Tooltip("Minimum pitch angle in degrees (negative = below horizon).")]
        private float _minPitch = -30f;

        [SerializeField, Tooltip("Maximum pitch angle in degrees (positive = above horizon).")]
        private float _maxPitch = 60f;

        [Header("Smoothing")]
        [SerializeField, Min(0.01f), Tooltip("Follow smoothing factor. Lower = smoother/slower.")]
        private float _smoothTime = 0.1f;

        // --- Public properties ---

        /// <summary>The player transform this camera follows. Assign in inspector or at runtime.</summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>Horizontal mouse sensitivity (degrees per input unit).</summary>
        public float MouseSensitivityX
        {
            get => _mouseSensitivityX;
            set => _mouseSensitivityX = Mathf.Max(0.01f, value);
        }

        /// <summary>Vertical mouse sensitivity (degrees per input unit).</summary>
        public float MouseSensitivityY
        {
            get => _mouseSensitivityY;
            set => _mouseSensitivityY = Mathf.Max(0.01f, value);
        }

        /// <summary>Minimum allowed pitch angle (degrees).</summary>
        public float MinPitch
        {
            get => _minPitch;
            set => _minPitch = value;
        }

        /// <summary>Maximum allowed pitch angle (degrees).</summary>
        public float MaxPitch
        {
            get => _maxPitch;
            set => _maxPitch = value;
        }

        /// <summary>Base camera offset from target (before orbit rotation).</summary>
        public Vector3 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        private float _yaw;
        private float _pitch;
        private Vector3 _velocity;
        private bool _lookEnabled = true;

        private void Awake()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            // Snap camera to initial position on first frame.
            if (_target != null)
            {
                Vector3 initialPos = _target.position + _offset;
                transform.position = initialPos;
                transform.LookAt(_target);
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Update orbit angles from look input (only when look is enabled).
            if (_lookEnabled && InputManager.Instance != null)
            {
                Vector2 look = InputManager.Instance.Look;
                _yaw += look.x * _mouseSensitivityX;
                _pitch += look.y * _mouseSensitivityY;
                _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            }

            // Calculate desired camera position.
            Quaternion orbitRotation = Quaternion.Euler(-_pitch, _yaw, 0f);
            Vector3 desiredPosition = _target.position + orbitRotation * _offset;

            // Smoothly interpolate to desired position.
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _smoothTime);

            transform.position = smoothedPosition;
            transform.LookAt(_target);
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            _lookEnabled = to == GameState.Exploring;
        }
    }
}
