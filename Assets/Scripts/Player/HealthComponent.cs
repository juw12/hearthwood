using System;
using Hearthwood.Core;
using UnityEngine;

namespace Hearthwood.Player
{
    /// <summary>
    /// Generic health component. Usable on the player, NPCs, enemies, or any entity
    /// that needs a health bar with damage/heal semantics and a one-shot death event.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1f), Tooltip("Maximum health value.")]
        private float _maxHealth = 100f;

        [SerializeField, Min(0f), Tooltip("Initial current health. Clamped to [0, Max] on Awake.")]
        private float _currentHealth = 100f;

        /// <summary>Current health, clamped to [0, <see cref="Max"/>].</summary>
        public float Current { get; private set; }

        /// <summary>Maximum health. Change via <see cref="SetMax"/>.</summary>
        public float Max { get; private set; }

        /// <summary>True when <see cref="Current"/> is zero.</summary>
        public bool IsDead => Current <= 0f;

        /// <summary>
        /// Raised whenever health changes. Args: (current, max).
        /// Also raised on <see cref="SetMax"/> if the current value is scaled.
        /// </summary>
        public event Action<float, float> OnChanged;

        /// <summary>Raised exactly once when health reaches zero.</summary>
        public event Action OnDeath;

        private bool _deathFired;

        private void Awake()
        {
            Max = _maxHealth;
            Current = Mathf.Clamp(_currentHealth, 0f, Max);
        }

        /// <summary>
        /// Apply damage. Clamps at 0. Raises <see cref="OnChanged"/> on any change
        /// and <see cref="OnDeath"/> exactly once when health reaches zero.
        /// </summary>
        /// <param name="amount">Damage amount (positive value).</param>
        /// <param name="source">Origin of the damage.</param>
        public void TakeDamage(float amount, DamageSource source = DamageSource.Generic)
        {
            if (IsDead || amount <= 0f) return;

            float previous = Current;
            Current = Mathf.Max(0f, Current - amount);

            if (Mathf.Approximately(previous, Current)) return;

            OnChanged?.Invoke(Current, Max);

            if (IsDead && !_deathFired)
            {
                _deathFired = true;
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// Restore health. Clamped to <see cref="Max"/>. No-op if already dead.
        /// </summary>
        /// <param name="amount">Heal amount (positive value).</param>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            float previous = Current;
            Current = Mathf.Min(Max, Current + amount);

            if (Mathf.Approximately(previous, Current)) return;

            OnChanged?.Invoke(Current, Max);
        }

        /// <summary>
        /// Change maximum health. If <paramref name="scaleCurrent"/> is true, current health
        /// is scaled proportionally (useful for level-up buffs). Otherwise current health is
        /// clamped to the new max.
        /// </summary>
        /// <param name="newMax">New maximum health value (must be greater than 0).</param>
        /// <param name="scaleCurrent">If true, scale current health proportionally to the new max.</param>
        public void SetMax(float newMax, bool scaleCurrent)
        {
            if (newMax <= 0f) return;

            float oldMax = Max;
            float previousCurrent = Current;

            Max = newMax;

            if (scaleCurrent && oldMax > 0f)
            {
                Current = Mathf.Clamp(Current * (newMax / oldMax), 0f, Max);
            }
            else
            {
                Current = Mathf.Clamp(Current, 0f, Max);
            }

            if (!Mathf.Approximately(previousCurrent, Current))
            {
                OnChanged?.Invoke(Current, Max);
            }
        }
    }
}
