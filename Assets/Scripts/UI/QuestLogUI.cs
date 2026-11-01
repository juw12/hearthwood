using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Hearthwood.Core;
using Hearthwood.Quest;

namespace Hearthwood.UI
{
    /// <summary>
    /// Quest log modal (Tab). Lists active and completed quests with their objectives and
    /// progress. Uses a ScrollRect + VerticalLayoutGroup content area, rebuilt from
    /// <see cref="QuestSystem"/> state on every relevant event.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestLogUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField, Tooltip("The quest log panel root (hidden unless state is QuestLog).")]
        private GameObject _panelRoot;

        [Header("Content")]
        [SerializeField, Tooltip("Content transform inside the ScrollRect (has a VerticalLayoutGroup).")]
        private Transform _content;

        [Header("Templates (optional headers / entries)")]
        [SerializeField, Tooltip("Header template shown above the active-quest list.")]
        private GameObject _activeHeaderTemplate;

        [SerializeField, Tooltip("Header template shown above the completed-quest list.")]
        private GameObject _completedHeaderTemplate;

        [SerializeField, Tooltip(
            "Quest entry template. Must contain a 'Title' child (TextMeshProUGUI) and an " +
            "'Objectives' child transform (VerticalLayoutGroup).")]
        private GameObject _questEntryTemplate;

        [SerializeField, Tooltip("Objective row template (a single TextMeshProUGUI).")]
        private GameObject _objectiveTemplate;

        private QuestSystem _quests;
        private GameObject _panel;

        // Local objective-progress mirror (same rationale as HUDController).
        private readonly Dictionary<string, int> _objectiveProgress = new Dictionary<string, int>();

        private void Start()
        {
            _panel = _panelRoot != null ? _panelRoot : transform.gameObject;
            _quests = QuestSystem.Instance;
            if (_quests != null)
            {
                _quests.OnQuestStarted += HandleQuestStarted;
                _quests.OnObjectiveCompleted += HandleObjectiveCompleted;
                _quests.OnQuestCompleted += HandleQuestCompleted;
                _quests.OnQuestFailed += HandleQuestFailed;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            SetPanelActive(false);
        }

        private void OnDestroy()
        {
            if (_quests != null)
            {
                _quests.OnQuestStarted -= HandleQuestStarted;
                _quests.OnObjectiveCompleted -= HandleObjectiveCompleted;
                _quests.OnQuestCompleted -= HandleQuestCompleted;
                _quests.OnQuestFailed -= HandleQuestFailed;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        /// <summary>Toggle the quest log open/closed (drives the game state machine).</summary>
        public void Toggle()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.QuestLog) { gm.SetState(GameState.Exploring); return; }
            if (gm.State != GameState.Exploring) return;
            gm.SetState(GameState.QuestLog);
            Refresh();
        }

        // ─── Event handlers ──────────────────────────────────────────────────────────────
        private void HandleStateChanged(GameState from, GameState to)
        {
            SetPanelActive(to == GameState.QuestLog);
            if (to == GameState.QuestLog) Refresh();
        }

        private void HandleQuestStarted(QuestData quest) { ResetProgress(quest); Refresh(); }
        private void HandleQuestFailed(QuestData quest) { Refresh(); }
        private void HandleQuestCompleted(QuestData quest) { MarkAllComplete(quest); Refresh(); }

        private void HandleObjectiveCompleted(QuestData quest, QuestObjective objective)
        {
            if (quest != null && !string.IsNullOrEmpty(objective.ObjectiveId))
                _objectiveProgress[ProgressKey(quest.Id, objective.ObjectiveId)] = objective.TargetCount;
            Refresh();
        }

        // ─── Rendering ────────────────────────────────────────────────────────────────────
        private void SetPanelActive(bool active)
        {
            if (_panel != null) _panel.SetActive(active);
        }

        private void Refresh()
        {
            if (_content == null || _quests == null) return;

            ClearContent();

            var active = _quests.ActiveQuests;
            if (active.Count > 0)
            {
                AddHeader(_activeHeaderTemplate);
                for (int i = 0; i < active.Count; i++)
                    AddQuestEntry(active[i]);
            }

            var completed = _quests.CompletedQuests;
            if (completed.Count > 0)
            {
                AddHeader(_completedHeaderTemplate);
                for (int i = 0; i < completed.Count; i++)
                    AddQuestEntry(completed[i]);
            }
        }

        private void ClearContent()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);
        }

        private void AddHeader(GameObject template)
        {
            if (template == null || _content == null) return;
            var header = Instantiate(template, _content);
            header.SetActive(true);
        }

        private void AddQuestEntry(QuestData quest)
        {
            if (quest == null || _questEntryTemplate == null || _content == null) return;

            var entry = Instantiate(_questEntryTemplate, _content);
            entry.SetActive(true);

            var titleTf = entry.transform.Find("Title");
            if (titleTf != null)
            {
                var title = titleTf.GetComponent<TextMeshProUGUI>();
                if (title != null) title.text = quest.DisplayName;
            }

            var objectivesTf = entry.transform.Find("Objectives");
            if (objectivesTf == null) return;

            var objectives = quest.Objectives;
            if (objectives == null) return;
            for (int i = 0; i < objectives.Length; i++)
            {
                var row = Instantiate(_objectiveTemplate, objectivesTf);
                row.SetActive(true);
                var label = row.GetComponent<TextMeshProUGUI>();
                if (label != null) label.text = FormatObjective(quest.Id, objectives[i]);
            }
        }

        private string FormatObjective(string questId, QuestObjective objective)
        {
            int current = GetProgress(questId, objective.ObjectiveId);
            bool done = current >= objective.TargetCount;
            string progress = done ? "done" : (current + " / " + objective.TargetCount);
            return "[" + progress + "] " + objective.Description;
        }

        // ─── Progress helpers ─────────────────────────────────────────────────────────────
        private static string ProgressKey(string questId, string objectiveId) => questId + "|" + objectiveId;

        private int GetProgress(string questId, string objectiveId)
            => _objectiveProgress.TryGetValue(ProgressKey(questId, objectiveId), out int value) ? value : 0;

        private void ResetProgress(QuestData quest)
        {
            if (quest == null || quest.Objectives == null) return;
            for (int i = 0; i < quest.Objectives.Length; i++)
                _objectiveProgress[ProgressKey(quest.Id, quest.Objectives[i].ObjectiveId)] = 0;
        }

        private void MarkAllComplete(QuestData quest)
        {
            if (quest == null || quest.Objectives == null) return;
            for (int i = 0; i < quest.Objectives.Length; i++)
                _objectiveProgress[ProgressKey(quest.Id, quest.Objectives[i].ObjectiveId)] = quest.Objectives[i].TargetCount;
        }
    }
}
