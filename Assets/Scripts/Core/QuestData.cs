using System;
using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>What kind of event completes this objective.</summary>
    public enum ObjectiveKind
    {
        /// <summary>Player must pick up a specific Collectible N times.</summary>
        CollectItem,

        /// <summary>Player must talk to an NPC until dialogue completes.</summary>
        TalkToNpc,

        /// <summary>Player must reach a trigger area.</summary>
        ReachArea,

        /// <summary>Player must defeat N enemies with a given tag.</summary>
        KillEnemy,

        /// <summary>Player must deliver an item to an NPC.</summary>
        DeliverItem,

        /// <summary>Custom — driven by game code via QuestSystem.Notify().</summary>
        Custom,
    }

    /// <summary>Single objective within a <see cref="QuestData"/>.</summary>
    [Serializable]
    public struct QuestObjective
    {
        [Tooltip("Stable id within the quest. Matches CollectibleData.ObjectiveId / etc.")]
        public string ObjectiveId;

        [TextArea(1, 3), Tooltip("Player-facing description, e.g. 'Gather 5 wild herbs'.")]
        public string Description;

        [Tooltip("How this objective is satisfied.")]
        public ObjectiveKind Kind;

        [Min(1), Tooltip("How many completions are needed.")]
        public int TargetCount;

        [Tooltip("CollectibleData id when Kind = CollectItem / DeliverItem.")]
        public string TargetCollectibleId;

        [Tooltip("NPC id when Kind = TalkToNpc.")]
        public string TargetNpcId;

        [Tooltip("Area trigger id when Kind = ReachArea.")]
        public string TargetAreaId;

        [Tooltip("Enemy tag when Kind = KillEnemy.")]
        public string TargetEnemyTag;
    }

    /// <summary>
    /// Reward granted when all objectives complete. Pure data — QuestSystem applies the
    /// reward (calls FavorTokenSystem.Award / InventoryComponent.TryAdd).
    /// </summary>
    [Serializable]
    public struct QuestReward
    {
        [Min(0), Tooltip("Favor tokens awarded.")]
        public int FavorTokens;

        [Tooltip("Item id awarded.")]
        public string ItemId;

        [Min(1), Tooltip("Item count awarded.")]
        public int ItemCount;

        [Tooltip("If set, this quest unlocks another quest with this id.")]
        public string UnlocksQuestId;
    }

    /// <summary>
    /// Quest definition. Create via Assets ▸ Create ▸ Hearthwood ▸ Quests ▸ Quest.
    /// The QuestSystem owns runtime state (status, objective progress); this asset is immutable.
    /// </summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "Hearthwood/Quests/Quest", order = 200)]
    public sealed class QuestData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable id used by save data. NEVER change after release.")]
        private string _id = "quest_";

        [SerializeField, Tooltip("Localized display name in quest log.")]
        private string _displayName = "Quest";

        [SerializeField, TextArea(2, 5), Tooltip("Quest summary shown in quest log.")]
        private string _description = "";

        [Header("Gating")]
        [SerializeField, Tooltip("Quests that must be completed before this one becomes available.")]
        private string[] _prerequisiteQuestIds = Array.Empty<string>();

        [SerializeField, Tooltip("Faction tag required (e.g. 'Villager'). Empty = no requirement.")]
        private string _factionRequirement = "";

        [Header("Objectives")]
        [SerializeField, Tooltip("Ordered list of objectives. All must complete before reward.")]
        private QuestObjective[] _objectives = Array.Empty<QuestObjective>();

        [Header("Rewards")]
        [SerializeField, Tooltip("Rewards granted on completion.")]
        private QuestReward[] _rewards = Array.Empty<QuestReward>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public string[] PrerequisiteQuestIds => _prerequisiteQuestIds;
        public string FactionRequirement => _factionRequirement;
        public QuestObjective[] Objectives => _objectives;
        public QuestReward[] Rewards => _rewards;

        public int ObjectiveCount => _objectives == null ? 0 : _objectives.Length;

        public QuestObjective GetObjective(string objectiveId)
        {
            if (_objectives == null) return default;
            for (int i = 0; i < _objectives.Length; i++)
                if (_objectives[i].ObjectiveId == objectiveId) return _objectives[i];
            return default;
        }
    }
}
