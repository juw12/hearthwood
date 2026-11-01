using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.Quest
{
    /// <summary>
    /// Attach to an NPC GameObject. Offers a quest to the player on first interaction.
    /// Call <see cref="TryStartQuest"/> when the player begins dialogue with this NPC.
    /// </summary>
    public sealed class QuestGiver : MonoBehaviour
    {
        [Header("Quest")]
        [SerializeField, Tooltip("The quest to offer when the player talks to this NPC.")]
        private QuestData _questToOffer;

        [Header("Debug")]
        [SerializeField, Tooltip("If true, log quest offer attempts to the console.")]
        private bool _verboseLogging = false;

        private bool _hasOffered;

        /// <summary>The quest this giver offers. Null if unconfigured.</summary>
        public QuestData QuestToOffer => _questToOffer;

        /// <summary>True if this giver has already offered its quest in this session.</summary>
        public bool HasOffered => _hasOffered;

        /// <summary>
        /// Attempt to start the offered quest. No-ops if the quest is null, already offered,
        /// not in Available status, or QuestSystem is not present.
        /// Call this from the NPC controller or dialogue system when the player starts talking.
        /// </summary>
        public void TryStartQuest()
        {
            if (_questToOffer == null)
            {
                Log("TryStartQuest: no quest assigned.");
                return;
            }
            if (_hasOffered)
            {
                Log($"TryStartQuest('{_questToOffer.Id}'): already offered.");
                return;
            }
            if (QuestSystem.Instance == null)
            {
                Log("TryStartQuest: QuestSystem not available.");
                return;
            }

            var status = QuestSystem.Instance.GetQuestStatus(_questToOffer.Id);
            if (status != QuestStatus.Available)
            {
                Log($"TryStartQuest('{_questToOffer.Id}'): status is {status}, not Available.");
                return;
            }

            QuestSystem.Instance.StartQuest(_questToOffer);
            _hasOffered = true;

            // Set the active quest id on the DialogueRunner so choices can reference it.
            if (DialogueRunner.Instance != null)
                DialogueRunner.Instance.ActiveQuestId = _questToOffer.Id;

            Log($"Quest offered and started: {_questToOffer.Id}");
        }

        private void Log(string msg)
        {
            if (_verboseLogging) Debug.Log($"[QuestGiver] {msg}");
        }
    }
}
