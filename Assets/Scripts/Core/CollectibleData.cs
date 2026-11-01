using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// World pickup variant of an item. References an <see cref="ItemData"/> for the actual
    /// inventory reward and adds world-pickup concerns: prompt, optional respawn delay,
    /// optional quest link.
    /// </summary>
    [CreateAssetMenu(fileName = "Collectible_", menuName = "Hearthwood/Items/Collectible", order = 101)]
    public sealed class CollectibleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable id used by save data. NEVER change after release.")]
        private string _id = "collectible_";

        [SerializeField, Tooltip("Display name for tooltips.")]
        private string _displayName = "Collectible";

        [SerializeField, TextArea(1, 3), Tooltip("Prompt shown when in range (e.g. 'Press E to pick up').")]
        private string _collectPrompt = "Press E to pick up";

        [Header("Reward")]
        [SerializeField, Tooltip("The item added to inventory on pickup.")]
        private ItemData _rewardItem;

        [SerializeField, Min(1), Tooltip("How many of rewardItem to grant.")]
        private int _rewardCount = 1;

        [Header("Quest Link (optional)")]
        [SerializeField, Tooltip("Quest id to advance on pickup (must match a QuestData.Id).")]
        private string _questId = "";

        [SerializeField, Tooltip("Objective id within the quest to mark complete.")]
        private string _objectiveId = "";

        [Header("Visuals")]
        [SerializeField, Tooltip("World icon (or null — the prefab supplies its own).")]
        private Sprite _icon;

        [Header("Spawn")]
        [SerializeField, Tooltip("If true, collectible respawns after despawn seconds. Else one-shot.")]
        private bool _respawnable = false;

        [SerializeField, Min(0f), Tooltip("Respawn delay in seconds. 0 = no respawn.")]
        private float _respawnDelaySeconds = 0f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string CollectPrompt => _collectPrompt;
        public ItemData RewardItem => _rewardItem;
        public int RewardCount => _rewardCount;
        public string QuestId => _questId;
        public string ObjectiveId => _objectiveId;
        public Sprite Icon => _icon;
        public bool Respawnable => _respawnable;
        public float RespawnDelaySeconds => _respawnDelaySeconds;

        /// <summary>True if this pickup should advance a quest on pickup.</summary>
        public bool HasQuestLink => !string.IsNullOrEmpty(_questId) && !string.IsNullOrEmpty(_objectiveId);
    }
}
