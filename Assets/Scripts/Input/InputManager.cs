using System;
using Hearthwood.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthwood.Input
{
    /// <summary>
    /// Centralized wrapper around Unity's Input System <c>PlayerInput</c>. Holds cached
    /// movement/look vectors and exposes button events. Subscribes to <see cref="GameManager"/>
    /// state changes and switches action maps accordingly.
    /// </summary>
    /// <remarks>
    /// Place on the same GameObject as a <c>PlayerInput</c> component. Assign the InputActions
    /// asset via the inspector. The default action asset path is
    /// <c>Assets/Settings/HearthwoodInputActions.inputactions</c>.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Action Map Names")]
        [SerializeField, Tooltip("Player action map name. Must match the .inputactions asset.")]
        private string _playerMapName = "Player";

        [SerializeField, Tooltip("UI action map name.")]
        private string _uiMapName = "UI";

        [SerializeField, Tooltip("Dialogue action map name.")]
        private string _dialogueMapName = "Dialogue";

        // --- Cached values ---
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AttackHeld { get; private set; }

        // --- Button events ---
        public event Action OnJump;
        public event Action OnInteract;
        public event Action OnAttack;
        public event Action OnAttackReleased;
        public event Action OnDodge;
        public event Action OnInventoryPressed;
        public event Action OnPausePressed;
        public event Action OnQuestLogPressed;
        public event Action OnSubmit;
        public event Action OnCancel;
        public event Action<int> OnDialogueChoice;
        public event Action OnDialogueAdvance;

        private PlayerInput _playerInput;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("[InputManager] PlayerInput component missing.");
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            _playerInput.onActionTriggered += HandleActionTriggered;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            _playerInput.onActionTriggered -= HandleActionTriggered;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Update cached per-frame values (continuous actions).
            if (_playerInput.actions == null) return;
            Move = _playerInput.actions.FindAction(_playerMapName + "/Move")?.ReadValue<Vector2>() ?? Vector2.zero;
            Look = _playerInput.actions.FindAction(_playerMapName + "/Look")?.ReadValue<Vector2>() ?? Vector2.zero;
            JumpHeld = _playerInput.actions.FindAction(_playerMapName + "/Jump")?.IsPressed() ?? false;
            SprintHeld = _playerInput.actions.FindAction(_playerMapName + "/Sprint")?.IsPressed() ?? false;
            AttackHeld = _playerInput.actions.FindAction(_playerMapName + "/Attack")?.IsPressed() ?? false;
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Exploring: SwitchMap(_playerMapName); break;
                case GameState.Inventory:
                case GameState.QuestLog:
                case GameState.Paused:    SwitchMap(_uiMapName); break;
                case GameState.Dialogue:  SwitchMap(_dialogueMapName); break;
                case GameState.Cutscene:  SwitchMap(_uiMapName); break;
            }
        }

        /// <summary>Force a specific action map (used by UI when opening modals).</summary>
        public void SwitchMap(string mapName)
        {
            if (string.IsNullOrEmpty(mapName)) return;
            if (_playerInput.currentActionMap != null && _playerInput.currentActionMap.name == mapName) return;
            _playerInput.SwitchCurrentActionMap(mapName);
        }

        private void HandleActionTriggered(InputAction.CallbackContext ctx)
        {
            var actionName = ctx.action.name;
            var phase = ctx.phase;

            // Player map
            if (actionName == "Jump" && phase == InputActionPhase.Performed) OnJump?.Invoke();
            else if (actionName == "Interact" && phase == InputActionPhase.Performed) OnInteract?.Invoke();
            else if (actionName == "Attack" && phase == InputActionPhase.Performed) { OnAttack?.Invoke(); }
            else if (actionName == "Attack" && phase == InputActionPhase.Canceled) { OnAttackReleased?.Invoke(); }
            else if (actionName == "Dodge" && phase == InputActionPhase.Performed) OnDodge?.Invoke();
            else if (actionName == "Inventory" && phase == InputActionPhase.Performed) OnInventoryPressed?.Invoke();
            else if (actionName == "Pause" && phase == InputActionPhase.Performed) OnPausePressed?.Invoke();
            // UI map
            else if (actionName == "QuestLog" && phase == InputActionPhase.Performed) OnQuestLogPressed?.Invoke();
            else if (actionName == "Submit" && phase == InputActionPhase.Performed) OnSubmit?.Invoke();
            else if (actionName == "Cancel" && phase == InputActionPhase.Performed) OnCancel?.Invoke();
            else if (actionName == "PauseBack" && phase == InputActionPhase.Performed) OnPausePressed?.Invoke();
            // Dialogue map
            else if (actionName == "Advance" && phase == InputActionPhase.Performed) OnDialogueAdvance?.Invoke();
            else if (actionName == "Choice1" && phase == InputActionPhase.Performed) OnDialogueChoice?.Invoke(0);
            else if (actionName == "Choice2" && phase == InputActionPhase.Performed) OnDialogueChoice?.Invoke(1);
            else if (actionName == "Choice3" && phase == InputActionPhase.Performed) OnDialogueChoice?.Invoke(2);
            else if (actionName == "Choice4" && phase == InputActionPhase.Performed) OnDialogueChoice?.Invoke(3);
            else if (actionName == "Skip" && phase == InputActionPhase.Performed) OnDialogueAdvance?.Invoke();
        }

        /// <summary>Convenience: turn a flag like "should I move?" from the player controller.</summary>
        public bool MovementEnabled => Move.sqrMagnitude > 0.01f;
    }
}
