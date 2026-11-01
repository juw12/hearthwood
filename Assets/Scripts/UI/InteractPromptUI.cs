using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hearthwood.Core;
using Hearthwood.Player;

namespace Hearthwood.UI
{
    /// <summary>
    /// World-space prompt that floats above the focused <see cref="IInteractable"/> and
    /// billboards to the main camera, with a smooth CanvasGroup fade. Attach to a world-space
    /// canvas root (with the prompt text and a CanvasGroup on it or a child).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractPromptUI : MonoBehaviour
    {
        [Header("Follower")]
        [SerializeField, Min(0.1f), Tooltip("Fade speed (alpha units per second).")]
        private float _fadeSpeed = 8f;

        [SerializeField, Tooltip("World-space offset above the focused target.")]
        private Vector3 _offset = new Vector3(0f, 1.8f, 0f);

        [Header("References")]
        [SerializeField, Tooltip("CanvasGroup used for the fade.")]
        private CanvasGroup _canvasGroup;

        [SerializeField, Tooltip("The prompt text.")]
        private TextMeshProUGUI _promptText;

        private IInteractable _focused;
        private bool _visible;
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
            _visible = false;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            var player = PlayerController3D.Instance;
            if (player != null)
                player.OnInteractableFocusChanged += HandleFocusChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            var player = PlayerController3D.Instance;
            if (player != null)
                player.OnInteractableFocusChanged -= HandleFocusChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        // ─── Event handlers ──────────────────────────────────────────────────────────────
        private void HandleFocusChanged(IInteractable interactable)
        {
            _focused = interactable;
            if (interactable != null)
            {
                _visible = true;
                if (_promptText != null) _promptText.text = interactable.InteractPrompt;
            }
            else
            {
                _visible = false;
            }
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            if (to != GameState.Exploring)
                _visible = false;
        }

        // ─── Per-frame follow + billboard + fade ─────────────────────────────────────────
        private void Update()
        {
            if (_visible && _focused is Component focusedComponent)
            {
                transform.position = focusedComponent.transform.position + _offset;
                if (_camera != null)
                    transform.rotation = _camera.transform.rotation;
            }

            if (_canvasGroup != null)
            {
                float targetAlpha = _visible ? 1f : 0f;
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, _fadeSpeed * Time.unscaledDeltaTime);
                _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0.5f;
                _canvasGroup.interactable = false;
            }
        }
    }
}
