using UnityEngine;
using Hearthwood.Core;
using Hearthwood.Input;

namespace Hearthwood.UI
{
    /// <summary>
    /// Single entry point for UI input. Subscribes to <see cref="InputManager"/> button
    /// events once and routes them to the modal UI components and the dialogue panel, so no
    /// modal subscribes to input independently. Place on a GameObject alongside the canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIInputBinder : MonoBehaviour
    {
        [Header("Modal References")]
        [SerializeField, Tooltip("Inventory modal.")]
        private InventoryUI _inventoryUI;

        [SerializeField, Tooltip("Pause menu modal.")]
        private PauseMenuUI _pauseMenuUI;

        [SerializeField, Tooltip("Quest log modal.")]
        private QuestLogUI _questLogUI;

        [SerializeField, Tooltip("Dialogue panel (for Advance / Choose / Confirm).")]
        private DialogueUI _dialogueUI;

        private InputManager _input;

        private void Start()
        {
            _input = InputManager.Instance;
            if (_input == null)
            {
                Debug.LogWarning("[UIInputBinder] InputManager.Instance is null; UI input will not be bound.");
                return;
            }

            _input.OnInventoryPressed += HandleInventoryPressed;
            _input.OnPausePressed += HandlePausePressed;
            _input.OnQuestLogPressed += HandleQuestLogPressed;
            _input.OnSubmit += HandleSubmit;
            _input.OnCancel += HandleCancel;
            _input.OnDialogueAdvance += HandleDialogueAdvance;
            _input.OnDialogueChoice += HandleDialogueChoice;
        }

        private void OnDestroy()
        {
            if (_input == null) return;
            _input.OnInventoryPressed -= HandleInventoryPressed;
            _input.OnPausePressed -= HandlePausePressed;
            _input.OnQuestLogPressed -= HandleQuestLogPressed;
            _input.OnSubmit -= HandleSubmit;
            _input.OnCancel -= HandleCancel;
            _input.OnDialogueAdvance -= HandleDialogueAdvance;
            _input.OnDialogueChoice -= HandleDialogueChoice;
        }

        // ─── Input routing ───────────────────────────────────────────────────────────────
        private void HandleInventoryPressed() { _inventoryUI?.Toggle(); }
        private void HandlePausePressed() { _pauseMenuUI?.Toggle(); }
        private void HandleQuestLogPressed() { _questLogUI?.Toggle(); }

        private void HandleSubmit()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Dialogue)
            {
                if (_dialogueUI != null) _dialogueUI.Confirm();
                return;
            }
            CloseActiveModal();
        }

        private void HandleCancel()
        {
            CloseActiveModal();
        }

        private void HandleDialogueAdvance()
        {
            if (_dialogueUI != null) _dialogueUI.Advance();
        }

        private void HandleDialogueChoice(int index)
        {
            if (_dialogueUI != null) _dialogueUI.Choose(index);
        }

        private void CloseActiveModal()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            switch (gm.State)
            {
                case GameState.Inventory:
                case GameState.QuestLog:
                case GameState.Paused:
                    gm.SetState(GameState.Exploring);
                    break;
            }
        }
    }
}
