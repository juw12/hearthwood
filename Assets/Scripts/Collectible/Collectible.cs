using System.Collections;
using UnityEngine;
using Hearthwood.Core;
using Hearthwood.Quest;
using Hearthwood.Player;

namespace Hearthwood.Collectible
{
    /// <summary>
    /// World pickup. Attach to a GameObject with a trigger Collider. When the player
    /// enters the trigger, the item is added to inventory, the quest system is notified,
    /// and the object deactivates (or respawns after a delay if configured).
    /// </summary>
    public sealed class Collectible : MonoBehaviour, ICollectible
    {
        [Header("Data")]
        [SerializeField, Tooltip("The collectible definition (item reward, prompt, quest link).")]
        private CollectibleData _data;

        [Header("Respawn")]
        [SerializeField, Min(0f), Tooltip("Override respawn delay. 0 = use CollectibleData.RespawnDelaySeconds.")]
        private float _respawnDelayOverride = 0f;

        private bool _isAlive = true;

        /// <summary>The collectible data asset.</summary>
        public CollectibleData Data => _data;

        /// <summary>True if this collectible has not been picked up (or has respawned).</summary>
        public bool IsAlive => _isAlive;

        /// <summary>
        /// Sets the collectible data at runtime. Used by <see cref="CollectibleSpawner"/>
        /// for programmatically-created instances where serialization is not possible.
        /// </summary>
        /// <param name="data">The collectible definition to use.</param>
        public void Configure(CollectibleData data)
        {
            _data = data;
        }

        // ─── ICollectible ───────────────────────────────────────────────────────────

        /// <summary>True if the player in ctx may pick this up right now.</summary>
        /// <param name="ctx">The interacting player's context.</param>
        public bool CanPick(PlayerContext ctx) => _isAlive && ctx.IsAlive;

        /// <summary>
        /// Adds the reward item to inventory, notifies the quest system, plays audio,
        /// and deactivates this object (or schedules a respawn).
        /// </summary>
        /// <param name="inventory">The player's inventory port.</param>
        public void OnPicked(IInventoryPort inventory)
        {
            if (!_isAlive || _data == null) return;
            _isAlive = false;

            // Add to inventory.
            if (_data.RewardItem != null)
                inventory?.TryAdd(_data.RewardItem, _data.RewardCount);

            // Notify quest system.
            QuestSystem.Instance?.NotifyCollectible(_data);
            PlayerController3D.Instance?.NotifyCollected(_data);

            // Deactivate and optionally schedule respawn.
            gameObject.SetActive(false);
            if (_data.Respawnable)
            {
                float delay = _respawnDelayOverride > 0f ? _respawnDelayOverride : _data.RespawnDelaySeconds;
                if (delay > 0f) StartCoroutine(RespawnAfter(delay));
            }
        }

        // ─── Unity messages ─────────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!_isAlive) return;
            if (!other.CompareTag("Player")) return;
            if (_data == null) return;

            var port = other.GetComponent<IInventoryPort>();
            if (port == null)
            {
                Debug.LogWarning($"[Collectible] '{name}': player has no IInventoryPort component.");
                return;
            }

            OnPicked(port);
        }

        // ─── Private ────────────────────────────────────────────────────────────────

        private IEnumerator RespawnAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (this == null) yield break;
            _isAlive = true;
            gameObject.SetActive(true);
        }
    }
}
