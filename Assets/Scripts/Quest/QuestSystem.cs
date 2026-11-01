using System;
using System.Collections.Generic;
using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.Quest
{
    /// <summary>
    /// Scene-scoped quest state machine. Tracks active and completed quests,
    /// objective progress, and applies rewards on completion. Registers with
    /// <see cref="SnapShotFillerRegistry"/> for save/load persistence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestSystem : MonoBehaviour
    {
        /// <summary>The single live instance. Null until first Awake.</summary>
        public static QuestSystem Instance { get; private set; }

        [Header("Debug")]
        [SerializeField, Tooltip("If true, log quest state transitions to the console.")]
        private bool _verboseLogging = true;

        /// <summary>Fired when a quest transitions to Active.</summary>
        public event Action<QuestData> OnQuestStarted;

        /// <summary>Fired when all objectives are complete and rewards are delivered.</summary>
        public event Action<QuestData> OnQuestCompleted;

        /// <summary>Fired when a single objective reaches its target count.</summary>
        public event Action<QuestData, QuestObjective> OnObjectiveCompleted;

        /// <summary>Fired when a quest fails (timeout, mutual exclusion).</summary>
        public event Action<QuestData> OnQuestFailed;

        /// <summary>
        /// Delegate to grant an item to the player's inventory. Set by the Player
        /// bootstrap or a wiring script. Args: (item, count).
        /// </summary>
        public Action<ItemData, int> GrantItem { get; set; }

        /// <summary>All currently active quests.</summary>
        public IReadOnlyList<QuestData> ActiveQuests => _activeQuests;

        /// <summary>All completed quests (rewards already delivered).</summary>
        public IReadOnlyList<QuestData> CompletedQuests => _completedQuests;

        private readonly List<QuestData> _activeQuests = new();
        private readonly List<QuestData> _completedQuests = new();
        private readonly Dictionary<string, Dictionary<string, int>> _objectiveProgress = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SnapShotFillerRegistry.Register(Fill, Apply);
        }

        private void Start()
        {
            // Wire DialogueRunner hooks (Quest can reference Dialogue per asmdef).
            if (DialogueRunner.Instance != null)
            {
                var runner = DialogueRunner.Instance;
                runner.QuestStatusResolver = GetQuestStatus;
                runner.QuestObjectiveAdvanced = (questId, objectiveId) => CompleteObjective(questId, objectiveId);
                runner.DialogueEndedWithSpeaker = NotifyDialogueEnded;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SnapShotFillerRegistry.Unregister(Fill, Apply);
        }

        /// <summary>
        /// Start a quest. No-ops if any prerequisite quest is not completed,
        /// or if the quest is already active/completed.
        /// </summary>
        /// <param name="quest">The quest definition to start.</param>
        public void StartQuest(QuestData quest)
        {
            if (quest == null) return;
            if (_activeQuests.Contains(quest) || _completedQuests.Contains(quest)) return;

            // Prerequisite check.
            foreach (string prereqId in quest.PrerequisiteQuestIds)
            {
                if (!IsCompleted(prereqId))
                {
                    Log($"StartQuest('{quest.Id}') blocked: prerequisite '{prereqId}' not completed.");
                    return;
                }
            }

            // Initialize objective progress.
            var objectives = new Dictionary<string, int>();
            foreach (var obj in quest.Objectives)
                objectives[obj.ObjectiveId] = 0;
            _objectiveProgress[quest.Id] = objectives;

            _activeQuests.Add(quest);
            Log($"Quest started: {quest.Id}");
            OnQuestStarted?.Invoke(quest);
        }

        /// <summary>
        /// Increment progress on a specific objective. When the objective reaches its
        /// target count, fires <see cref="OnObjectiveCompleted"/>. When all objectives
        /// of the quest are complete, completes the quest and delivers rewards.
        /// </summary>
        /// <param name="questId">Stable quest id.</param>
        /// <param name="objectiveId">Stable objective id within the quest.</param>
        public void CompleteObjective(string questId, string objectiveId)
        {
            if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(objectiveId)) return;

            var quest = FindActiveQuest(questId);
            if (quest == null) return;

            var objective = quest.GetObjective(objectiveId);
            if (string.IsNullOrEmpty(objective.ObjectiveId)) return;

            if (!_objectiveProgress.TryGetValue(questId, out var progress))
            {
                progress = new Dictionary<string, int>();
                _objectiveProgress[questId] = progress;
            }

            int current = progress.TryGetValue(objectiveId, out int val) ? val : 0;
            current++;
            progress[objectiveId] = current;

            if (current >= objective.TargetCount)
            {
                Log($"Objective complete: {questId}/{objectiveId} ({current}/{objective.TargetCount})");
                OnObjectiveCompleted?.Invoke(quest, objective);
            }

            if (AreAllObjectivesComplete(quest))
                CompleteQuest(quest);
        }

        /// <summary>
        /// Notify the quest system that a collectible was picked up.
        /// If the collectible has a quest link, advances the linked objective.
        /// </summary>
        /// <param name="c">The collected item data.</param>
        public void NotifyCollectible(CollectibleData c)
        {
            if (c == null) return;
            if (c.HasQuestLink)
                CompleteObjective(c.QuestId, c.ObjectiveId);
        }

        /// <summary>
        /// Notify the quest system that a dialogue ended with a specific NPC.
        /// Advances all active TalkToNpc objectives targeting that NPC.
        /// </summary>
        /// <param name="npcId">The NPC's stable id.</param>
        public void NotifyDialogueEnded(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return;

            for (int i = _activeQuests.Count - 1; i >= 0; i--)
            {
                var quest = _activeQuests[i];
                foreach (var obj in quest.Objectives)
                {
                    if (obj.Kind == ObjectiveKind.TalkToNpc && obj.TargetNpcId == npcId)
                    {
                        CompleteObjective(quest.Id, obj.ObjectiveId);
                        break; // Only advance one objective per quest per dialogue end.
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current status of a quest. Returns <see cref="QuestStatus.Available"/>
        /// if the quest has never been started.
        /// </summary>
        /// <param name="questId">The quest's stable id.</param>
        public QuestStatus GetQuestStatus(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return QuestStatus.Available;

            if (_completedQuests.Exists(q => q.Id == questId))
                return QuestStatus.Completed;

            if (_activeQuests.Exists(q => q.Id == questId))
            {
                // Check if all objectives are complete (ReadyToComplete vs Active).
                var quest = _activeQuests.Find(q => q.Id == questId);
                if (quest != null && AreAllObjectivesComplete(quest))
                    return QuestStatus.ReadyToComplete;
                return QuestStatus.Active;
            }

            return QuestStatus.Available;
        }

        // ─── Private ────────────────────────────────────────────────────────────────

        private void CompleteQuest(QuestData quest)
        {
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);
            ApplyRewards(quest);
            Log($"Quest completed: {quest.Id}");
            OnQuestCompleted?.Invoke(quest);
        }

        private void ApplyRewards(QuestData quest)
        {
            foreach (var reward in quest.Rewards)
            {
                if (reward.FavorTokens > 0)
                    FavorTokenSystem.Instance?.Award(reward.FavorTokens);

                if (!string.IsNullOrEmpty(reward.ItemId) && reward.ItemCount > 0)
                {
                    var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetById(reward.ItemId) : null;
                    if (item != null)
                        GrantItem?.Invoke(item, reward.ItemCount);
                    else
                        Debug.LogWarning($"[QuestSystem] Reward item '{reward.ItemId}' not found in ItemDatabase.");
                }
            }
        }

        private bool AreAllObjectivesComplete(QuestData quest)
        {
            if (!_objectiveProgress.TryGetValue(quest.Id, out var progress))
                return false;

            foreach (var obj in quest.Objectives)
            {
                int current = progress.TryGetValue(obj.ObjectiveId, out int val) ? val : 0;
                if (current < obj.TargetCount) return false;
            }
            return true;
        }

        private QuestData FindActiveQuest(string questId)
        {
            for (int i = 0; i < _activeQuests.Count; i++)
                if (_activeQuests[i].Id == questId) return _activeQuests[i];
            return null;
        }

        private bool IsCompleted(string questId)
        {
            for (int i = 0; i < _completedQuests.Count; i++)
                if (_completedQuests[i].Id == questId) return true;
            return false;
        }

        private void Fill(SaveSystem.Snapshot snap)
        {
            if (snap == null) return;

            snap.activeQuestIds = new string[_activeQuests.Count];
            for (int i = 0; i < _activeQuests.Count; i++)
                snap.activeQuestIds[i] = _activeQuests[i].Id;

            snap.completedQuestIds = new string[_completedQuests.Count];
            for (int i = 0; i < _completedQuests.Count; i++)
                snap.completedQuestIds[i] = _completedQuests[i].Id;

            var list = new List<SaveSystem.ObjectiveProgress>();
            foreach (var (questId, objectives) in _objectiveProgress)
            {
                foreach (var (objId, count) in objectives)
                    list.Add(new SaveSystem.ObjectiveProgress { questId = questId, objectiveId = objId, currentCount = count });
            }
            snap.objectiveProgress = list.ToArray();
        }

        private void Apply(SaveSystem.Snapshot snap)
        {
            if (snap == null) return;

            _activeQuests.Clear();
            _completedQuests.Clear();
            _objectiveProgress.Clear();

            if (ItemDatabase.Instance == null)
            {
                Debug.LogWarning("[QuestSystem] Apply: ItemDatabase not ready. Quest state will be empty.");
                return;
            }

            if (snap.activeQuestIds != null)
            {
                foreach (var id in snap.activeQuestIds)
                {
                    var q = ItemDatabase.Instance.GetQuestById(id);
                    if (q != null) _activeQuests.Add(q);
                }
            }

            if (snap.completedQuestIds != null)
            {
                foreach (var id in snap.completedQuestIds)
                {
                    var q = ItemDatabase.Instance.GetQuestById(id);
                    if (q != null) _completedQuests.Add(q);
                }
            }

            if (snap.objectiveProgress != null)
            {
                foreach (var p in snap.objectiveProgress)
                {
                    if (!_objectiveProgress.TryGetValue(p.questId, out var objectives))
                    {
                        objectives = new Dictionary<string, int>();
                        _objectiveProgress[p.questId] = objectives;
                    }
                    objectives[p.objectiveId] = p.currentCount;
                }
            }
        }

        private void Log(string msg)
        {
            if (_verboseLogging) Debug.Log($"[QuestSystem] {msg}");
        }
    }
}
