using UnityEngine;
using UnityEngine.UI;
using Hearthwood.Core;

namespace Hearthwood.UI
{
    /// <summary>
    /// Pause menu modal (Esc). Shows Resume / Save / Load / Quit. Driven by the
    /// <see cref="GameState"/> machine; input is routed here by <see cref="UIInputBinder"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField, Tooltip("The pause menu panel root.")]
        private GameObject _panelRoot;

        [Header("Buttons")]
        [SerializeField, Tooltip("Resume button (returns to Exploring).")]
        private Button _resumeButton;

        [SerializeField, Tooltip("Save button (GameManager.Save).")]
        private Button _saveButton;

        [SerializeField, Tooltip("Load button (GameManager.Load).")]
        private Button _loadButton;

        [SerializeField, Tooltip("Quit button (Application.Quit on Standalone; no-op elsewhere).")]
        private Button _quitButton;

        private GameObject _panel;

        private void Start()
        {
            _panel = _panelRoot != null ? _panelRoot : transform.gameObject;
            if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
            if (_saveButton != null) _saveButton.onClick.AddListener(Save);
            if (_loadButton != null) _loadButton.onClick.AddListener(Load);
            if (_quitButton != null) _quitButton.onClick.AddListener(Quit);
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            SetPanelActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        /// <summary>Toggle the pause menu (drives the game state machine).</summary>
        public void Toggle()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Paused) { gm.SetState(GameState.Exploring); return; }
            if (gm.State != GameState.Exploring) return;
            gm.SetState(GameState.Paused);
        }

        // ─── Button actions ──────────────────────────────────────────────────────────────
        private void Resume() => GameManager.Instance?.SetState(GameState.Exploring);
        private void Save() => GameManager.Instance?.Save();
        private void Load() => GameManager.Instance?.Load();

        private void Quit()
        {
#if UNITY_STANDALONE
            Application.Quit();
#else
            Debug.Log("[PauseMenuUI] Quit requested on a non-Standalone platform (no-op).");
#endif
        }

        // ─── State sync ──────────────────────────────────────────────────────────────────
        private void HandleStateChanged(GameState from, GameState to)
        {
            SetPanelActive(to == GameState.Paused);
        }

        private void SetPanelActive(bool active)
        {
            if (_panel != null) _panel.SetActive(active);
        }
    }
}
