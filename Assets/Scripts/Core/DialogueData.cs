using System;
using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>One branch option within a <see cref="DialogueNode"/>.</summary>
    [Serializable]
    public struct DialogueChoice
    {
        [Tooltip("Player-facing text, e.g. 'Tell me about the missing shipment.'")]
        public string Text;

        [Tooltip("Node id to jump to when chosen. Empty/null = end dialogue.")]
        public string NextNodeId;

        [Tooltip("If non-empty, the choice is only shown when the quest is in this status.")]
        public QuestStatus RequiredQuestStatus;

        [Tooltip("Quest id to check for RequiredQuestStatus. Empty = ignore.")]
        public string RequiredQuestId;

        [Tooltip("If true, the choice ends the dialogue after being taken.")]
        public bool EndsDialogue;

        [Tooltip("If set, completing this choice fires a QuestEvent on the target NPC.")]
        public bool AdvancesQuestObjective;

        [Tooltip("Quest objective id to mark complete when AdvancesQuestObjective is true.")]
        public string AdvancesObjectiveId;
    }

    /// <summary>One node in a dialogue tree.</summary>
    [Serializable]
    public struct DialogueNode
    {
        [Tooltip("Stable id within the dialogue. The root node id lives on DialogueData.")]
        public string NodeId;

        [Tooltip("Speaker id ('Player' for inner monologue, or an NPC id).")]
        public string SpeakerId;

        [Tooltip("Localized display name of the speaker.")]
        public string SpeakerName;

        [TextArea(2, 6), Tooltip("Line spoken. Supports inline {playerName} substitution.")]
        public string Text;

        [Tooltip("Auto-advance target when no choices are defined. Empty = end.")]
        public string NextNodeId;

        [Tooltip("Branching options. Empty array = use NextNodeId for a single auto-advance.")]
        public DialogueChoice[] Choices;

        [Tooltip("If true, this node triggers an event when displayed.")]
        public string TriggerEventId;
    }

    /// <summary>
    /// Dialogue tree asset. The DialogueRunner walks nodes top-to-bottom; choices create
    /// branches; quest-status requirements gate branches. Create via Hearthwood ▸ Dialogue.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "Hearthwood/Dialogues/Dialogue", order = 300)]
    public sealed class DialogueData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable id used by save data. NEVER change after release.")]
        private string _id = "dialogue_";

        [SerializeField, Tooltip("First node id. Must exist in Nodes.")]
        private string _rootNodeId = "root";

        [SerializeField, Tooltip("All nodes in the tree. Order matters only for the inspector.")]
        private DialogueNode[] _nodes = Array.Empty<DialogueNode>();

        [Header("Metadata")]
        [SerializeField, Tooltip("Default speaker id for nodes that omit one. Usually the NPC's id.")]
        private string _defaultSpeakerId = "";

        public string Id => _id;
        public string RootNodeId => _rootNodeId;
        public DialogueNode[] Nodes => _nodes;
        public string DefaultSpeakerId => _defaultSpeakerId;

        /// <summary>Find a node by id. Returns false if not found.</summary>
        public bool TryGetNode(string nodeId, out DialogueNode node)
        {
            if (_nodes != null)
            {
                for (int i = 0; i < _nodes.Length; i++)
                {
                    if (_nodes[i].NodeId == nodeId) { node = _nodes[i]; return true; }
                }
            }
            node = default;
            return false;
        }
    }
}
