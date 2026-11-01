using Hearthwood.Player;
using UnityEngine;

namespace Hearthwood.Audio
{
    /// <summary>
    /// Plays footstep audio when the player is moving on the ground. Reads
    /// <c>PlayerController3D.IsGrounded / IsSprinting / IsRunning</c> and steps at a
    /// cadence scaled by sprint speed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FootstepPlayer : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField, Tooltip("Footstep clip bank. One is picked at random each step.")]
        private AudioClip[] _footstepClips = System.Array.Empty<AudioClip>();

        [Header("Cadence")]
        [SerializeField, Range(0.1f, 1.5f), Tooltip("Seconds between steps while walking.")]
        private float _walkStepInterval = 0.55f;

        [SerializeField, Range(0.1f, 1.0f), Tooltip("Seconds between steps while sprinting.")]
        private float _sprintStepInterval = 0.32f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume scale per step.")]
        private float _stepVolume = 0.8f;

        private PlayerController3D _player;
        private float _stepTimer;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController3D>();
            if (_player == null)
                Debug.LogWarning("[FootstepPlayer] No PlayerController3D found in parents — footsteps disabled.");
        }

        private void Update()
        {
            if (_player == null || AudioManager.Instance == null) return;
            if (!_player.IsGrounded || !_player.IsRunning) { _stepTimer = 0f; return; }

            float interval = _player.IsSprinting ? _sprintStepInterval : _walkStepInterval;
            // Scale by current speed to keep cadence tight under speed modifiers.
            float speedScale = Mathf.Clamp(_player.Velocity.magnitude / 6f, 0.6f, 1.8f);
            interval /= speedScale;

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= interval)
            {
                _stepTimer = 0f;
                PlayStep();
            }
        }

        private void PlayStep()
        {
            if (_footstepClips == null || _footstepClips.Length == 0) return;
            var clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
            if (clip == null) return;
            AudioManager.Instance.PlaySFX(clip, _player != null ? _player.Transform.position : transform.position, _stepVolume);
        }
    }
}
