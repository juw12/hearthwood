using System;
using System.Collections;
using System.Collections.Generic;
using Hearthwood.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthwood.Scene
{
    /// <summary>
    /// Loads <see cref="SceneData"/> by id, single or additive, with fade transitions.
    /// Persists across scenes (DontDestroyOnLoad) since GameManager and global systems rely
    /// on a single routing instance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneController : MonoBehaviour
    {
        public static SceneController Instance { get; private set; }

        [Header("Catalog")]
        [SerializeField, Tooltip("All SceneData assets the game can load. Drag every SceneData here.")]
        private List<SceneData> _catalog = new();

        [Header("Transition")]
        [SerializeField, Tooltip("Optional CanvasGroup used for fade-to-black. May be null.")]
        private CanvasGroup _fadeOverlay;

        [SerializeField, Range(0f, 2f), Tooltip("Fade out duration in seconds.")]
        private float _fadeOutSeconds = 0.4f;

        [SerializeField, Range(0f, 2f), Tooltip("Fade in duration in seconds.")]
        private float _fadeInSeconds = 0.4f;

        private readonly Dictionary<string, SceneData> _byId = new();
        private Coroutine _activeTransition;

        public string CurrentSceneId { get; private set; }
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RebuildCatalog();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterScene(SceneData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id)) return;
            _byId[data.Id] = data;
            if (!_catalog.Contains(data)) _catalog.Add(data);
        }

        private void RebuildCatalog()
        {
            _byId.Clear();
            foreach (var s in _catalog) if (s != null && !string.IsNullOrEmpty(s.Id)) _byId[s.Id] = s;
        }

        /// <summary>Load a scene by SceneData asset.</summary>
        public void LoadScene(SceneData data, string spawnPointId = null)
        {
            if (data == null) { Debug.LogWarning("[SceneController] LoadScene called with null data."); return; }
            LoadById(data.Id, spawnPointId ?? data.DefaultSpawnPointId);
        }

        /// <summary>Load a scene by id.</summary>
        public void LoadById(string sceneId, string spawnPointId = null)
        {
            if (!_byId.TryGetValue(sceneId, out var data))
            {
                Debug.LogError($"[SceneController] Scene id '{sceneId}' not in catalog. Use RegisterScene at startup.");
                return;
            }
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(TransitionRoutine(data, spawnPointId));
        }

        private IEnumerator TransitionRoutine(SceneData data, string spawnPointId)
        {
            OnSceneLoadStarted?.Invoke(data.Id);
            yield return Fade(1f);
            // Unload previous single scene (skip additive scenes).
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.IsValid() && active.name != data.BuildName)
            {
                var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(active);
                while (op != null && !op.isDone) yield return null;
            }
            // Load primary scene.
            var loadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(data.BuildName, LoadSceneMode.Single);
            while (loadOp != null && !loadOp.isDone) yield return null;
            CurrentSceneId = data.Id;
            // Load additive.
            if (data.AdditiveSceneBuildNames != null)
            {
                foreach (var add in data.AdditiveSceneBuildNames)
                {
                    if (string.IsNullOrEmpty(add)) continue;
                    var addOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(add, LoadSceneMode.Additive);
                    while (addOp != null && !addOp.isDone) yield return null;
                }
            }
            yield return Fade(0f);
            OnSceneLoadCompleted?.Invoke(data.Id);
            _activeTransition = null;
        }

        private IEnumerator Fade(float target)
        {
            if (_fadeOverlay == null) yield break;
            _fadeOverlay.blocksRaycasts = true;
            float start = _fadeOverlay.alpha;
            float t = 0f;
            float dur = target > start ? _fadeInSeconds : _fadeOutSeconds;
            if (dur <= 0f) { _fadeOverlay.alpha = target; yield break; }
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _fadeOverlay.alpha = Mathf.Lerp(start, target, t / dur);
                yield return null;
            }
            _fadeOverlay.alpha = target;
            _fadeOverlay.blocksRaycasts = target > 0.01f;
        }
    }
}
