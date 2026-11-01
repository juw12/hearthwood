using System;
using System.Collections.Generic;
using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.Dialogue
{
    /// <summary>
    /// Scene-scoped dialogue state machine. Walks <see cref="DialogueData"/> nodes,
    /// fires events for the UI to display lines and choices, and integrates with the
    /// quest system via delegate hooks (set by QuestSystem at startup).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueRunner : MonoBehaviour
    {
        /// <summary>The single live instance. Null until first Awake.</summary>
        public static DialogueRunner Instance { get; private set; }

        [Header("Debug")]
        [SerializeField, Tooltip("If true, log dialogue state transitions to the console.")]
        private bool _verboseLogging = true;

        // ─── UI-facing events ───────────────────────────────────────────────────────

        /// <summary>Fired when a new line of dialogue is shown. The UI displays this.</summary>
        public event Action<DialogueLine> OnLineShown;

        /// <summary>Fired when choices are available. The UI renders these as buttons.</summary>
        public event Action<DialogueChoice[]> OnChoicesShown;

        /// <summary>Fired when the dialogue session ends.</summary>
        public event Action OnDialogueEnded;

        // ─── Quest integration delegates (set by QuestSystem) ──────────────────────

        /// <summary>
        /// Resolves the quest status for a given quest id. Set by QuestSystem in Start().
        /// Returns <see cref="QuestStatus.Available"/> if the quest is unknown.
        /// </summary>
        public Func<string, QuestStatus> QuestStatusResolver { get; set; }

        /// <summary>
        /// Fired when a dialogue choice advances a quest objective.
        /// Args: (questId, objectiveId). Set by QuestSystem.
        /// </summary>
        public Action<string, string> QuestObjectiveAdvanced { get; set; }

        /// <summary>
        /// Fired when a dialogue ends. Args: (speakerId). Used by QuestSystem
        /// to advance TalkToNpc objectives. Set by QuestSystem.
        /// </summary>
        public Action<string> DialogueEndedWithSpeaker { get; set; }

        // ─── Public state ───────────────────────────────────────────────────────────

        /// <summary>True while a dialogue session is in progress.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// The quest id associated with the current dialogue session.
        /// Set by QuestGiver before starting dialogue so that choices can
        /// reference the correct quest for advancement.
        /// </summary>
        public string ActiveQuestId { get; set; }

        /// <summary>
        /// Name substituted for {playerName} tokens in dialogue text.
        /// Set by the player bootstrap or UI at startup. Defaults to "Traveler".
        /// </summary>
        public string PlayerName { get; set; } = "Traveler";

        // ─── Internal state ─────────────────────────────────────────────────────────

        private IDialogueSpeaker _speaker;
        private DialogueData _currentDialogue;
        private DialogueNode _currentNode;
        private PlayerContext _ctx;
        private GameState _previousState;
        private bool _awaitingChoice;
        private DialogueChoice[] _visibleChoices;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Begin a dialogue session. Sets game state to Dialogue, finds the root node,
        /// and displays the first line. No-ops if already running.
        /// </summary>
        /// <param name="speaker">The entity speaking (NPC, signpost, etc.).</param>
        /// <param name="dialogue">The dialogue tree to walk.</param>
        /// <param name="ctx">Snapshot of the interacting player.</param>
        public void StartDialogue(IDialogueSpeaker speaker, DialogueData dialogue, PlayerContext ctx)
        {
            if (IsRunning)
            {
                Log("StartDialogue called while already running. Ignoring.");
                return;
            }
            if (speaker == null || dialogue == null)
            {
                Debug.LogWarning("[DialogueRunner] StartDialogue: speaker or dialogue is null.");
                return;
            }

            _speaker = speaker;
            _currentDialogue = dialogue;
            _ctx = ctx;
            _awaitingChoice = false;
            _visibleChoices = null;
            IsRunning = true;

            _previousState = GameManager.Instance != null ? GameManager.Instance.State : GameState.Exploring;
            GameManager.Instance?.SetState(GameState.Dialogue);

            Log($"Dialogue started with '{speaker.SpeakerId}'.");
            ShowNode(dialogue.RootNodeId);
        }

        /// <summary>
        /// Advance to the next node. Only valid when not awaiting a choice (i.e., the
        /// current node has no choices). If the current node has no NextNodeId, the
        /// dialogue ends.
        /// </summary>
        public void Advance()
        {
            if (!IsRunning) return;
            if (_awaitingChoice)
            {
                Log("Advance called while awaiting choice. Ignoring.");
                return;
            }

            if (string.IsNullOrEmpty(_currentNode.NextNodeId))
            {
                EndDialogue();
                return;
            }

            ShowNode(_currentNode.NextNodeId);
        }

        /// <summary>
        /// Select a visible choice by index. Applies quest effects if the choice
        /// advances an objective, then navigates to the choice's target node or ends.
        /// </summary>
        /// <param name="choiceIndex">Index into the array passed to OnChoicesShown.</param>
        public void Choose(int choiceIndex)
        {
            if (!IsRunning) return;
            if (!_awaitingChoice || _visibleChoices == null)
            {
                Log("Choose called without active choices. Ignoring.");
                return;
            }
            if (choiceIndex < 0 || choiceIndex >= _visibleChoices.Length)
            {
                Debug.LogWarning($"[DialogueRunner] Choose: index {choiceIndex} out of range.");
                return;
            }

            var choice = _visibleChoices[choiceIndex];
            _awaitingChoice = false;
            _visibleChoices = null;

            // Quest advancement.
            if (choice.AdvancesQuestObjective && !string.IsNullOrEmpty(choice.AdvancesObjectiveId))
            {
                string questId = !string.IsNullOrEmpty(ActiveQuestId) ? ActiveQuestId : "";
                if (!string.IsNullOrEmpty(questId))
                    QuestObjectiveAdvanced?.Invoke(questId, choice.AdvancesObjectiveId);
            }

            // Navigate or end.
            if (choice.EndsDialogue || string.IsNullOrEmpty(choice.NextNodeId))
            {
                EndDialogue();
                return;
            }

            ShowNode(choice.NextNodeId);
        }

        /// <summary>
        /// Force-end the current dialogue session. Restores game state, fires
        /// <see cref="OnDialogueEnded"/>, and notifies the quest system.
        /// </summary>
        public void EndDialogue()
        {
            if (!IsRunning) return;

            IsRunning = false;
            _awaitingChoice = false;
            _visibleChoices = null;

            GameManager.Instance?.SetState(_previousState);
            OnDialogueEnded?.Invoke();
            DialogueEndedWithSpeaker?.Invoke(_speaker != null ? _speaker.SpeakerId : "");

            Log($"Dialogue ended (speaker: {_speaker?.SpeakerId}).");
        }

        // ─── Private ────────────────────────────────────────────────────────────────

        private void ShowNode(string nodeId)
        {
            if (!IsRunning) return;

            if (!_currentDialogue.TryGetNode(nodeId, out var node))
            {
                Debug.LogWarning($"[DialogueRunner] Node '{nodeId}' not found in dialogue '{_currentDialogue.Id}'. Ending.");
                EndDialogue();
                return;
            }

            _currentNode = node;

            // Substitute {playerName} in text.
            string text = SubstituteText(node.Text);

            // Determine speaker info from the node or fall back to the dialogue speaker.
            string speakerId = !string.IsNullOrEmpty(node.SpeakerId) ? node.SpeakerId : (_speaker != null ? _speaker.SpeakerId : "");
            string speakerName = !string.IsNullOrEmpty(node.SpeakerName) ? node.SpeakerName : (_speaker != null ? _speaker.SpeakerName : "");

            var line = new DialogueLine(speakerId, speakerName, text, -1);
            OnLineShown?.Invoke(line);

            // Check for choices.
            var choices = node.Choices;
            if (choices != null && choices.Length > 0)
            {
                _visibleChoices = FilterChoices(choices);
                _awaitingChoice = true;
                OnChoicesShown?.Invoke(_visibleChoices);
            }
            else
            {
                _visibleChoices = null;
                _awaitingChoice = false;
            }
        }

        private DialogueChoice[] FilterChoices(DialogueChoice[] all)
        {
            // Fast path: no quest gating needed.
            bool needsFilter = false;
            for (int i = 0; i < all.Length; i++)
            {
                if (!string.IsNullOrEmpty(all[i].RequiredQuestId))
                {
                    needsFilter = true;
                    break;
                }
            }

            if (!needsFilter) return all;

            var visible = new List<DialogueChoice>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (!string.IsNullOrEmpty(c.RequiredQuestId))
                {
                    QuestStatus status = QuestStatusResolver != null
                        ? QuestStatusResolver(c.RequiredQuestId)
                        : QuestStatus.Available;
                    if (status != c.RequiredQuestStatus) continue;
                }
                visible.Add(c);
            }
            return visible.ToArray();
        }

        private string SubstituteText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!text.Contains("{playerName}")) return text;
            return text.Replace("{playerName}", PlayerName);
        }

        private void Log(string msg)
        {
            if (_verboseLogging) Debug.Log($"[DialogueRunner] {msg}");
        }
    }
}
