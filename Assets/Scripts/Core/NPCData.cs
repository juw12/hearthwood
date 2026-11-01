using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// NPC definition. NPCController MonoBehaviour in Hearthwood.NPC binds to this asset
    /// to drive dialogue, quest offers, and basic AI states. Create via Hearthwood ▸ NPCs.
    /// </summary>
    [CreateAssetMenu(fileName = "NPC_", menuName = "Hearthwood/NPCs/NPC", order = 400)]
    public sealed class NPCData : ScriptableObject, IDialogueSpeaker
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable id used by save data. NEVER change after release.")]
        private string _id = "npc_";

        [SerializeField, Tooltip("Localized display name shown in dialogue and quest UI.")]
        private string _displayName = "Villager";

        [Header("Dialogue")]
        [SerializeField, Tooltip("Default dialogue opened when the player presses Interact.")]
        private DialogueData _defaultDialogue;

        [SerializeField, Tooltip("Optional quest to offer when the player first speaks to this NPC.")]
        private QuestData _offeredQuest;

        [Header("AI")]
        [SerializeField, Tooltip("Patrol path asset. Empty = stationary.")]
        private string _patrolPathId = "";

        [SerializeField, Range(0f, 30f), Tooltip("Wander radius from spawn. 0 = no wander.")]
        private float _wanderRadius = 0f;

        [SerializeField, Tooltip("Faction tag. Used for dialogue gating and combat.")]
        private string _faction = "Villager";

        [Header("Visuals")]
        [SerializeField, Tooltip("Portrait sprite shown in dialogue panel.")]
        private Sprite _portrait;

        string IDialogueSpeaker.SpeakerId => _id;
        string IDialogueSpeaker.SpeakerName => _displayName;

        public string Id => _id;
        public string DisplayName => _displayName;
        public DialogueData DefaultDialogue => _defaultDialogue;
        public QuestData OfferedQuest => _offeredQuest;
        public string PatrolPathId => _patrolPathId;
        public float WanderRadius => _wanderRadius;
        public string Faction => _faction;
        public Sprite Portrait => _portrait;

        public DialogueData GetDefaultDialogue() => _defaultDialogue;

        public DialogueData GetDialogueForContext(PlayerContext ctx)
        {
            // If the player already has the offered quest, return default. If not, return default
            // (NPC will offer the quest via QuestGiver hook). Extend here for quest-gated dialogue.
            return _defaultDialogue;
        }
    }
}
