using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hearthwood.Core;
using Hearthwood.Dialogue;
using Hearthwood.NPC;

namespace Hearthwood.UI
{
    /// <summary>
    /// Dialogue panel. Renders the portrait, speaker name, current line, and up to four
    /// choice buttons. Rendering is driven by <see cref="DialogueRunner"/> events; input is
    /// routed here by <see cref="UIInputBinder"/>. Attach to the dialogue canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField, Tooltip("The dialogue panel root. Shown while a line is active, hidden on end.")]
        private GameObject _panelRoot;

        [Header("Line")]
        [SerializeField, Tooltip("Portrait image (hidden when the speaker has no portrait).")]
        private Image _portraitImage;

        [SerializeField, Tooltip("Speaker display name.")]
        private TextMeshProUGUI _speakerNameText;

        [SerializeField, Tooltip("Current line of dialogue.")]
        private TextMeshProUGUI _lineText;

        [SerializeField, Tooltip("Hint shown when the current line has no choices (e.g. 'Press Space to continue').")]
        private TextMeshProUGUI _continueHintText;

        [Header("Choices")]
        [SerializeField, Tooltip("Container (ideally a VerticalLayoutGroup) that holds choice buttons.")]
        private Transform _choicesContainer;

        [SerializeField, Tooltip("Template choice button (must contain a TextMeshProUGUI). Cloned up to 4 times.")]
        private Button _choiceButtonTemplate;

        private readonly List<Button> _choiceButtons = new List<Button>();
        private readonly Dictionary<string, Sprite> _portraits = new Dictionary<string, Sprite>();
        private GameObject _panel;

        private void Start()
        {
            _panel = _panelRoot != null ? _panelRoot : transform.gameObject;
            var runner = DialogueRunner.Instance;
            if (runner != null)
            {
                runner.OnLineShown += HandleLineShown;
                runner.OnChoicesShown += HandleChoicesShown;
                runner.OnDialogueEnded += HandleDialogueEnded;
            }
            SetPanelActive(false);
        }

        private void OnDestroy()
        {
            var runner = DialogueRunner.Instance;
            if (runner != null)
            {
                runner.OnLineShown -= HandleLineShown;
                runner.OnChoicesShown -= HandleChoicesShown;
                runner.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        // ─── Public input routing (called by UIInputBinder) ───────────────────────────────
        /// <summary>Advance the dialogue (no-op while a choice is awaiting selection).</summary>
        public void Advance() => DialogueRunner.Instance?.Advance();

        /// <summary>Select the choice at <paramref name="index"/>.</summary>
        /// <param name="index">Zero-based index into the current choice set.</param>
        public void Choose(int index) => DialogueRunner.Instance?.Choose(index);

        /// <summary>Confirm: advances the dialogue if no choice is awaiting (used for Submit).</summary>
        public void Confirm() => Advance();

        // ─── Event handlers ──────────────────────────────────────────────────────────────
        private void HandleLineShown(DialogueLine line)
        {
            SetPanelActive(true);
            if (_speakerNameText != null) _speakerNameText.text = line.SpeakerName;
            if (_lineText != null) _lineText.text = line.Text;
            ApplyPortrait(line.SpeakerId);
            ClearChoices();
            if (_continueHintText != null) _continueHintText.gameObject.SetActive(true);
        }

        private void HandleChoicesShown(DialogueChoice[] choices)
        {
            if (_continueHintText != null) _continueHintText.gameObject.SetActive(false);
            BuildChoices(choices);
        }

        private void HandleDialogueEnded()
        {
            ClearChoices();
            SetPanelActive(false);
        }

        // ─── Private ──────────────────────────────────────────────────────────────────────
        private void SetPanelActive(bool active)
        {
            if (_panel != null) _panel.SetActive(active);
        }

        private void ApplyPortrait(string speakerId)
        {
            if (_portraitImage == null) return;
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(speakerId))
            {
                if (!_portraits.TryGetValue(speakerId, out sprite))
                {
                    RebuildPortraits();
                    _portraits.TryGetValue(speakerId, out sprite);
                }
            }
            _portraitImage.sprite = sprite;
            _portraitImage.enabled = sprite != null;
        }

        private void RebuildPortraits()
        {
            _portraits.Clear();
            var speakers = FindObjectsOfType<NPCController>();
            for (int i = 0; i < speakers.Length; i++)
            {
                if (speakers[i] != null && !string.IsNullOrEmpty(speakers[i].SpeakerId))
                    _portraits[speakers[i].SpeakerId] = speakers[i].Portrait;
            }
        }

        private void BuildChoices(DialogueChoice[] choices)
        {
            ClearChoices();
            if (choices == null || choices.Length == 0) return;
            if (_choiceButtonTemplate == null || _choicesContainer == null)
            {
                Debug.LogWarning("[DialogueUI] Choice template or container not assigned; cannot show choices.");
                return;
            }

            int count = Mathf.Min(choices.Length, 4);
            for (int i = 0; i < count; i++)
            {
                var button = Instantiate(_choiceButtonTemplate, _choicesContainer);
                button.gameObject.SetActive(true);
                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = choices[i].Text;
                int index = i;
                button.onClick.AddListener(() => Choose(index));
                _choiceButtons.Add(button);
            }
        }

        private void ClearChoices()
        {
            for (int i = 0; i < _choiceButtons.Count; i++)
                if (_choiceButtons[i] != null) Destroy(_choiceButtons[i].gameObject);
            _choiceButtons.Clear();
        }
    }
}
