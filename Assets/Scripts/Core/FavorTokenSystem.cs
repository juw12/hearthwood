using System;
using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// Tracks <c>Favor Tokens</c> — the unique progression element for Hearthwood. Earned
    /// by completing villager quests and dialogue branches; spent at the shop for discounts
    /// or to unlock new quest lines. Persisted through <see cref="SaveSystem"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FavorTokenSystem : MonoBehaviour
    {
        public static FavorTokenSystem Instance { get; private set; }

        [SerializeField, Min(0), Tooltip("Starting favor tokens for a new game.")]
        private int _startingTokens = 0;

        /// <summary>Current favor token count.</summary>
        public int Tokens { get; private set; }

        /// <summary>Fired when <see cref="Tokens"/> changes. Args: (newCount, delta).</summary>
        public event Action<int, int> OnFavorChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Tokens = _startingTokens;
            SnapShotFillerRegistry.Register(Fill, Apply);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SnapShotFillerRegistry.Unregister(Fill, Apply);
        }

        /// <summary>Award tokens (quest rewards, dialogue choices). Clamped at 0.</summary>
        public void Award(int amount)
        {
            if (amount <= 0) return;
            Tokens += amount;
            OnFavorChanged?.Invoke(Tokens, amount);
        }

        /// <summary>
        /// Spend tokens. Returns false if insufficient balance — caller must check and
        /// present "not enough tokens" UI rather than relying on this to no-op silently.
        /// </summary>
        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (Tokens < amount) return false;
            Tokens -= amount;
            OnFavorChanged?.Invoke(Tokens, -amount);
            return true;
        }

        private void Fill(SaveSystem.Snapshot snap)
        {
            if (snap == null) return;
            snap.favorTokens = Tokens;
        }

        private void Apply(SaveSystem.Snapshot snap)
        {
            if (snap == null) return;
            Tokens = Mathf.Max(0, snap.favorTokens);
            OnFavorChanged?.Invoke(Tokens, 0);
        }
    }
}
