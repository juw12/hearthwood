using System;
using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// Top-level singleton. The ONLY GameObject with <c>DontDestroyOnLoad</c>; every other
    /// singleton is scene-scoped and rebuilds on scene change. Owns the <see cref="GameState"/>
    /// state machine and routes Save/Load through <see cref="SaveSystem"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>The single live instance. Null until first <c>Awake</c>.</summary>
        public static GameManager Instance { get; private set; }

        [Header("State Machine")]
        [SerializeField, Tooltip("Initial state on first scene load. Reset to Exploring to start a new game.")]
        private GameState _initialState = GameState.Exploring;

        [Header("Save")]
        [SerializeField, Tooltip("PlayerPrefs key for the persisted SaveData JSON blob.")]
        private string _saveKey = "hearthwood.save.v1";

        [SerializeField, Tooltip("If true, AutoSave fires every N seconds while Exploring.")]
        private bool _autoSaveEnabled = true;

        [SerializeField, Range(5f, 300f), Tooltip("Auto-save interval in seconds.")]
        private float _autoSaveIntervalSeconds = 30f;

        /// <summary>Current game state. Listeners subscribe to <see cref="OnStateChanged"/>.</summary>
        public GameState State { get; private set; }

        /// <summary>Fired when state transitions. Args: (from, to).</summary>
        public event Action<GameState, GameState> OnStateChanged;

        private float _autoSaveTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            State = _initialState;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_autoSaveEnabled) return;
            if (State != GameState.Exploring) { _autoSaveTimer = 0f; return; }
            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= _autoSaveIntervalSeconds)
            {
                _autoSaveTimer = 0f;
                Save();
            }
        }

        /// <summary>Set a new game state. Fires <see cref="OnStateChanged"/> only on change.</summary>
        public void SetState(GameState newState)
        {
            if (newState == State) return;
            var previous = State;
            State = newState;
            OnStateChanged?.Invoke(previous, newState);
            ApplyTimeScale(newState);
        }

        /// <summary>Convenience: pause (sets Paused + timeScale 0).</summary>
        public void PauseGame()
        {
            if (State == GameState.Paused) return;
            SetState(GameState.Paused);
        }

        /// <summary>Convenience: resume (sets Exploring + timeScale 1).</summary>
        public void ResumeGame()
        {
            SetState(GameState.Exploring);
        }

        /// <summary>Save current progress through <see cref="SaveSystem"/>.</summary>
        public void Save()
        {
            SaveSystem.Save(_saveKey, SaveSystem.CaptureSnapshot());
        }

        /// <summary>Restore progress from PlayerPrefs.</summary>
        public void Load()
        {
            var snapshot = SaveSystem.Load(_saveKey);
            if (snapshot == null) return;
            SaveSystem.ApplySnapshot(snapshot);
        }

        private static void ApplyTimeScale(GameState state)
        {
            Time.timeScale = state == GameState.Paused || state == GameState.Cutscene ? 0f : 1f;
        }
    }
}
