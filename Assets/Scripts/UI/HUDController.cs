using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hearthwood.Core;
using Hearthwood.Player;
using Hearthwood.Quest;

namespace Hearthwood.UI
{
    /// <summary>
    /// Heads-up display. Shows the current quest, favor tokens, player health, and the
    /// interaction prompt. Driven by cross-system events (no per-frame polling). Attach to
    /// the HUD canvas root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Text Fields")]
        [SerializeField, Tooltip("Current quest title.")]
        private TextMeshProUGUI _questTitleText;

        [SerializeField, Tooltip("Current objective progress, e.g. 'Gather herbs: 2 / 5'.")]
        private TextMeshProUGUI _questObjectiveText;

        [SerializeField, Tooltip("Favor token count.")]
        private TextMeshProUGUI _favorText;

        [SerializeField, Tooltip("Player health, e.g. '80 / 100'.")]
        private TextMeshProUGUI _healthText;

        [SerializeField, Tooltip("Interaction prompt (visible only while Exploring and a target is focused).")]
        private TextMeshProUGUI _interactionPromptText;

        [Header("Bars")]
        [SerializeField, Tooltip("Optional health bar fill (fillAmount = current / max).")]
        private Image _hpBarFill;

        // Cached cross-system references (scene-scoped singletons, ready by Start).
        private HealthComponent _health;
        private FavorTokenSystem _favor;
        private QuestSystem _quests;
        private IInteractable _focused;

        // Local objective-progress mirror. QuestSystem does not expose per-objective counts
        // publicly, so we maintain them from its events (key = "<questId>|<objectiveId>").
        private readonly Dictionary<string, int> _objectiveProgress = new Dictionary<string, int>();

        private void Start()
        {
            var player = PlayerController3D.Instance;
            _health = player != null ? player.GetComponent<HealthComponent>() : null;
            _favor = FavorTokenSystem.Instance;
            _quests = QuestSystem.Instance;

            var gm = GameManager.Instance;
            if (gm != null) gm.OnStateChanged += HandleStateChanged;
            if (_quests != null)
            {
                _quests.OnQuestStarted += HandleQuestStarted;
                _quests.OnObjectiveCompleted += HandleObjectiveCompleted;
                _quests.OnQuestCompleted += HandleQuestCompleted;
            }
            if (_favor != null) _favor.OnFavorChanged += HandleFavorChanged;
            if (player != null)
            {
                player.OnHealthChanged += HandleHealthChanged;
                player.OnInteractableFocusChanged += HandleFocusChanged;
                player.OnItemCollected += HandleItemCollected;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnStateChanged -= HandleStateChanged;
            if (_quests != null)
            {
                _quests.OnQuestStarted -= HandleQuestStarted;
                _quests.OnObjectiveCompleted -= HandleObjectiveCompleted;
                _quests.OnQuestCompleted -= HandleQuestCompleted;
            }
            if (_favor != null) _favor.OnFavorChanged -= HandleFavorChanged;
            var player = PlayerController3D.Instance;
            if (player != null)
            {
                player.OnHealthChanged -= HandleHealthChanged;
                player.OnInteractableFocusChanged -= HandleFocusChanged;
                player.OnItemCollected -= HandleItemCollected;
            }
        }

        // ─── Event handlers ──────────────────────────────────────────────────────────────
        private void HandleStateChanged(GameState from, GameState to) { Refresh(); }
        private void HandleHealthChanged(float current, float max) { Refresh(); }
        private void HandleFavorChanged(int total, int delta) { Refresh(); }
        private void HandleFocusChanged(IInteractable focused) { _focused = focused; Refresh(); }

        private void HandleQuestStarted(QuestData quest)
        {
            ResetProgress(quest);
            Refresh();
        }

        private void HandleObjectiveCompleted(QuestData quest, QuestObjective objective)
        {
            if (quest != null && !string.IsNullOrEmpty(objective.ObjectiveId))
                _objectiveProgress[ProgressKey(quest.Id, objective.ObjectiveId)] = objective.TargetCount;
            Refresh();
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            MarkAllComplete(quest);
            Refresh();
        }

        private void HandleItemCollected(CollectibleData collectible)
        {
            if (collectible == null || !collectible.HasQuestLink) return;
            string key = ProgressKey(collectible.QuestId, collectible.ObjectiveId);
            int current = _objectiveProgress.TryGetValue(key, out int value) ? value : 0;
            int target = LookUpTargetCount(collectible.QuestId, collectible.ObjectiveId);
            int next = current + 1;
            if (target > 0) next = Mathf.Min(next, target);
            _objectiveProgress[key] = next;
            Refresh();
        }

        // ─── Rendering ────────────────────────────────────────────────────────────────────
        private void Refresh()
        {
            if (_health != null)
            {
                if (_healthText != null)
                    _healthText.text = Mathf.RoundToInt(_health.Current) + " / " + Mathf.RoundToInt(_health.Max);
                if (_hpBarFill != null)
                    _hpBarFill.fillAmount = _health.Max > 0f ? _health.Current / _health.Max : 0f;
            }

            if (_favor != null && _favorText != null)
                _favorText.text = _favor.Tokens.ToString();

            RefreshQuest();
            RefreshPrompt();
        }

        private void RefreshQuest()
        {
            var active = _quests != null ? _quests.ActiveQuests : null;
            if (active == null || active.Count == 0)
            {
                if (_questTitleText != null) _questTitleText.text = string.Empty;
                if (_questObjectiveText != null) _questObjectiveText.text = string.Empty;
                return;
            }

            var quest = active[0];
            if (_questTitleText != null) _questTitleText.text = quest.DisplayName;

            string objectiveLine = string.Empty;
            var objectives = quest.Objectives;
            if (objectives != null)
            {
                for (int i = 0; i < objectives.Length; i++)
                {
                    int current = GetProgress(quest.Id, objectives[i].ObjectiveId);
                    if (current < objectives[i].TargetCount)
                    {
                        objectiveLine = objectives[i].Description + ": " + current + " / " + objectives[i].TargetCount;
                        break;
                    }
                }
            }
            if (_questObjectiveText != null) _questObjectiveText.text = objectiveLine;
        }

        private void RefreshPrompt()
        {
            if (_interactionPromptText == null) return;
            var gm = GameManager.Instance;
            bool show = gm != null && gm.State == GameState.Exploring && _focused != null;
            _interactionPromptText.gameObject.SetActive(show);
            if (show) _interactionPromptText.text = _focused.InteractPrompt;
        }

        // ─── Progress tracking helpers ────────────────────────────────────────────────────
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

        private int LookUpTargetCount(string questId, string objectiveId)
        {
            if (_quests == null) return 0;
            var active = _quests.ActiveQuests;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Id != questId) continue;
                var objective = active[i].GetObjective(objectiveId);
                return string.IsNullOrEmpty(objective.ObjectiveId) ? 0 : objective.TargetCount;
            }
            return 0;
        }
    }
}
