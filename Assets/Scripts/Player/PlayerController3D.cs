using System;
using Hearthwood.Core;
using Hearthwood.Input;
using UnityEngine;

namespace Hearthwood.Player
{
    /// <summary>
    /// Third-person player controller. Handles movement (WASD + sprint + jump),
    /// health forwarding, interaction raycast, and event re-broadcast for
    /// cross-subsystem consumers (FootstepPlayer, HUD, QuestSystem, etc.).
    /// Requires a <see cref="CharacterController"/> on the same GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class PlayerController3D : MonoBehaviour
    {
        /// <summary>Scene-scoped singleton. Null until <c>Awake</c> or after <c>OnDestroy</c>.</summary>
        public static PlayerController3D Instance { get; private set; }

        [Header("Movement")]
        [SerializeField, Min(0.1f), Tooltip("Walk speed in units per second.")]
        private float _walkSpeed = 4f;

        [SerializeField, Min(0.1f), Tooltip("Sprint speed in units per second.")]
        private float _sprintSpeed = 7f;

        [SerializeField, Min(0f), Tooltip("Jump velocity in units per second.")]
        private float _jumpVelocity = 5f;

        [SerializeField, Min(1f), Tooltip("Gravity strength (positive value, applied as -Y acceleration).")]
        private float _gravity = 9.81f;

        [Header("Interaction")]
        [SerializeField, Tooltip("Camera used for the interaction raycast. Falls back to Camera.main if null.")]
        private Transform _interactionCamera;

        [SerializeField, Min(0.1f), Tooltip("Maximum range for the interaction raycast.")]
        private float _interactRange = 3f;

        private CharacterController _cc;
        private HealthComponent _health;
        private Vector3 _velocity;
        private IInteractable _focusedInteractable;
        private bool _inputSubscribed;

        // --- Public read-only properties (ARCHITECTURE.md §4) ---

        /// <summary>True while the CharacterController is touching the ground.</summary>
        public bool IsGrounded => _cc != null && _cc.isGrounded;

        /// <summary>True while the player is actively moving and holding sprint.</summary>
        public bool IsSprinting => IsRunning && InputManager.Instance != null && InputManager.Instance.SprintHeld;

        /// <summary>True while the player has non-zero movement input (walk or sprint).</summary>
        public bool IsRunning
        {
            get
            {
                if (InputManager.Instance == null) return false;
                return InputManager.Instance.Move.sqrMagnitude > 0.01f;
            }
        }

        /// <summary>Current velocity vector (includes gravity). Read by FootstepPlayer for cadence.</summary>
        public Vector3 Velocity => _velocity;

        /// <summary>Shortcut to the player's Transform for camera follow and ray origin.</summary>
        public Transform Transform => transform;

        // --- Public events (ARCHITECTURE.md §4) ---

        /// <summary>Raised on health change. Args: (current, max). Forwarded from <see cref="HealthComponent"/>.</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Raised exactly once when the player dies. Forwarded from <see cref="HealthComponent"/>.</summary>
        public event Action OnDeath;

        /// <summary>
        /// Raised when the interaction raycast enters or exits an <see cref="IInteractable"/>.
        /// Arg: the newly focused target (null when focus is lost).
        /// </summary>
        public event Action<IInteractable> OnInteractableFocusChanged;

        /// <summary>Raised when the Interact input fires (regardless of whether a target is focused).</summary>
        public event Action OnInteractPressed;

        /// <summary>Raised after a collectible is successfully added to inventory. For HUD + QuestSystem.</summary>
        public event Action<CollectibleData> OnItemCollected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cc = GetComponent<CharacterController>();
            _health = GetComponent<HealthComponent>();

            _health.OnChanged += (cur, max) => OnHealthChanged?.Invoke(cur, max);
            _health.OnDeath += () => OnDeath?.Invoke();

            if (_interactionCamera == null)
                _interactionCamera = Camera.main != null ? Camera.main.transform : null;

            PlayerPersistence.Register();
        }

        private void OnEnable()
        {
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void Start()
        {
            SubscribeInput();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnsubscribeInput();
        }

        private void SubscribeInput()
        {
            if (_inputSubscribed || InputManager.Instance == null) return;
            InputManager.Instance.OnJump += HandleJump;
            InputManager.Instance.OnInteract += HandleInteract;
            _inputSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_inputSubscribed) return;
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnJump -= HandleJump;
                InputManager.Instance.OnInteract -= HandleInteract;
            }
            _inputSubscribed = false;
        }

        private void Update()
        {
            if (InputManager.Instance == null || _cc == null) return;

            // Only process movement while in Exploring state.
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Exploring) return;

            HandleMovement();
            HandleInteractionRaycast();
        }

        /// <summary>
        /// Apply damage to the player. Forwards to <see cref="HealthComponent"/>.
        /// </summary>
        /// <param name="amount">Damage amount (positive value).</param>
        /// <param name="source">Origin of the damage.</param>
        public void TakeDamage(float amount, DamageSource source)
        {
            _health?.TakeDamage(amount, source);
        }

        /// <summary>
        /// Bridge method called by <c>Collectible.OnPicked</c> after the item is added to inventory.
        /// Raises <see cref="OnItemCollected"/> for HUD and QuestSystem listeners.
        /// </summary>
        /// <param name="data">The collectible that was picked up.</param>
        public void NotifyCollected(CollectibleData data)
        {
            if (data == null) return;
            OnItemCollected?.Invoke(data);
        }

        // --- Private helpers ---

        private void HandleMovement()
        {
            Vector2 moveInput = InputManager.Instance.Move;

            // Camera-relative movement basis.
            Vector3 forward = (_interactionCamera != null ? _interactionCamera.forward : transform.forward);
            Vector3 right = (_interactionCamera != null ? _interactionCamera.right : transform.right);
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            float speed = IsSprinting ? _sprintSpeed : _walkSpeed;

            // Gravity.
            _velocity.y -= _gravity * Time.deltaTime;

            Vector3 move = moveDir * speed;
            move.y = _velocity.y;

            _cc.Move(move * Time.deltaTime);

            // Store horizontal velocity for external readers (FootstepPlayer cadence).
            _velocity = new Vector3(_cc.velocity.x, _velocity.y, _cc.velocity.z);
        }

        private void HandleJump()
        {
            if (!IsGrounded) return;
            _velocity.y = _jumpVelocity;
        }

        private void HandleInteractionRaycast()
        {
            if (_interactionCamera == null)
            {
                if (_focusedInteractable != null)
                {
                    _focusedInteractable = null;
                    OnInteractableFocusChanged?.Invoke(null);
                }
                return;
            }

            if (!Physics.Raycast(
                    _interactionCamera.position,
                    _interactionCamera.forward,
                    out RaycastHit hit,
                    _interactRange))
            {
                if (_focusedInteractable != null)
                {
                    _focusedInteractable = null;
                    OnInteractableFocusChanged?.Invoke(null);
                }
                return;
            }

            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != _focusedInteractable)
            {
                _focusedInteractable = interactable;
                OnInteractableFocusChanged?.Invoke(interactable);
            }
        }

        private void HandleInteract()
        {
            OnInteractPressed?.Invoke();

            if (_focusedInteractable == null) return;

            var ctx = BuildPlayerContext();
            if (!_focusedInteractable.CanInteract(ctx)) return;
            _focusedInteractable.Interact(ctx);
        }

        private PlayerContext BuildPlayerContext()
        {
            float currentHealth = _health != null ? _health.Current : 0f;
            float maxHealth = _health != null ? _health.Max : 1f;
            int tokens = FavorTokenSystem.Instance != null ? FavorTokenSystem.Instance.Tokens : 0;
            return new PlayerContext(transform, currentHealth, maxHealth, "Player", tokens);
        }
    }
}
