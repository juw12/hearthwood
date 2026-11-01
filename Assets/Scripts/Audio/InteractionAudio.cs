using Hearthwood.Core;
using UnityEngine;

namespace Hearthwood.Audio
{
    /// <summary>
    /// Plays one-shot audio cues for interactions: interact success/fail, pickup, dialogue
    /// advance, quest complete. Subscribes to relevant system events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionAudio : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip _interactSuccess;
        [SerializeField] private AudioClip _interactFail;
        [SerializeField] private AudioClip _pickup;
        [SerializeField] private AudioClip _dialogueAdvance;
        [SerializeField] private AudioClip _dialogueChoice;
        [SerializeField] private AudioClip _questComplete;
        [SerializeField] private AudioClip _favorAwarded;
        [SerializeField] private AudioClip _pause;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] private float _interactVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float _pickupVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _dialogueVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _questVolume = 1f;

        private void OnEnable()
        {
            // These subscriptions are loose — if QuestSystem/DialogueRunner don't exist yet, no-op.
            TrySubscribeQuest();
            TrySubscribeDialogue();
            TrySubscribeFavor();
            TrySubscribeGame();
        }

        private void OnDisable()
        {
            TryUnsubscribe();
        }

        private void TrySubscribeQuest()
        {
            if (Hearthwood.Quest.QuestSystem.Instance != null)
            {
                Hearthwood.Quest.QuestSystem.Instance.OnQuestCompleted += HandleQuestCompleted;
                Hearthwood.Quest.QuestSystem.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
            }
        }

        private void TrySubscribeDialogue()
        {
            if (Hearthwood.Dialogue.DialogueRunner.Instance != null)
            {
                Hearthwood.Dialogue.DialogueRunner.Instance.OnLineShown += HandleDialogueAdvance;
                Hearthwood.Dialogue.DialogueRunner.Instance.OnChoicesShown += HandleDialogueChoices;
            }
        }

        private void TrySubscribeFavor()
        {
            if (FavorTokenSystem.Instance != null) FavorTokenSystem.Instance.OnFavorChanged += HandleFavorChanged;
        }

        private void TrySubscribeGame()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleGameStateChanged;
        }

        private void TryUnsubscribe()
        {
            if (Hearthwood.Quest.QuestSystem.Instance != null)
            {
                Hearthwood.Quest.QuestSystem.Instance.OnQuestCompleted -= HandleQuestCompleted;
                Hearthwood.Quest.QuestSystem.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
            }
            if (Hearthwood.Dialogue.DialogueRunner.Instance != null)
            {
                Hearthwood.Dialogue.DialogueRunner.Instance.OnLineShown -= HandleDialogueAdvance;
                Hearthwood.Dialogue.DialogueRunner.Instance.OnChoicesShown -= HandleDialogueChoices;
            }
            if (FavorTokenSystem.Instance != null) FavorTokenSystem.Instance.OnFavorChanged -= HandleFavorChanged;
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
        }

        public void PlayInteractSuccess() => Play2D(_interactSuccess, _interactVolume);
        public void PlayInteractFail() => Play2D(_interactFail, _interactVolume);
        public void PlayPickup() => Play2D(_pickup, _pickupVolume);
        public void PlayQuestComplete() => Play2D(_questComplete, _questVolume);
        public void PlayFavorAwarded() => Play2D(_favorAwarded, _questVolume);
        public void PlayPause() => Play2D(_pause, 0.5f);

        private void HandleQuestCompleted(QuestData q) => PlayQuestComplete();
        private void HandleObjectiveCompleted(QuestData q, QuestObjective o) => Play2D(_pickup, _dialogueVolume);
        private void HandleDialogueAdvance(DialogueLine l) => Play2D(_dialogueAdvance, _dialogueVolume);
        private void HandleDialogueChoices(DialogueChoice[] c) => Play2D(_dialogueChoice, _dialogueVolume);
        private void HandleFavorChanged(int total, int delta) { if (delta > 0) PlayFavorAwarded(); }
        private void HandleGameStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Paused) PlayPause();
        }

        private static void Play2D(AudioClip clip, float vol)
        {
            if (clip == null || AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySFX2D(clip, vol);
        }
    }
}
