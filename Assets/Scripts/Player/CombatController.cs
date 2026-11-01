using System;
using System.Collections.Generic;
using Hearthwood.Core;
using Hearthwood.Input;
using UnityEngine;

namespace Hearthwood.Player
{
    /// <summary>
    /// Implemented by any entity that can receive damage from the player's melee attack
    /// (enemies, destructible objects, etc.). Combat-specific; do not confuse with
    /// <see cref="HealthComponent"/> which tracks the internal health value.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Apply damage from the player's melee attack.</summary>
        /// <param name="amount">Damage amount.</param>
        /// <param name="source">Damage origin (always <see cref="DamageSource.PlayerMelee"/> from this controller).</param>
        void TakeDamage(float amount, DamageSource source);
    }

    /// <summary>
    /// Melee attack controller. Sits on a child GameObject of the player. Uses
    /// <see cref="Physics.OverlapSphere"/> to detect <see cref="IDamageable"/> targets
    /// within range on each attack. A cooldown equal to <see cref="AttackDurationSeconds"/>
    /// prevents re-attacking during the animation window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatController : MonoBehaviour
    {
        [Header("Melee Stats")]
        [SerializeField, Min(0f), Tooltip("Damage dealt per melee hit.")]
        private float _damage = 15f;

        [SerializeField, Min(0.1f), Tooltip("Radius of the hit sphere.")]
        private float _range = 1.8f;

        [SerializeField, Min(0.05f), Tooltip("Duration of the attack animation (cooldown before next hit).")]
        private float _attackDurationSeconds = 0.3f;

        [Header("Layers")]
        [SerializeField, Tooltip("Layer mask for targets. Defaults to Everything.")]
        private int _targetLayerMask = -1;

        /// <summary>Damage per melee hit.</summary>
        public float Damage => _damage;

        /// <summary>Hit sphere radius.</summary>
        public float Range => _range;

        /// <summary>Attack animation / cooldown duration in seconds.</summary>
        public float AttackDurationSeconds => _attackDurationSeconds;

        private float _cooldownTimer;
        private readonly List<Collider> _overlapResults = new List<Collider>(8);
        private bool _subscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || InputManager.Instance == null) return;
            InputManager.Instance.OnAttack += HandleAttack;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (InputManager.Instance != null)
                InputManager.Instance.OnAttack -= HandleAttack;
            _subscribed = false;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        private void HandleAttack()
        {
            if (_cooldownTimer > 0f) return;

            _cooldownTimer = _attackDurationSeconds;

            _overlapResults.Clear();
            Physics.OverlapSphereNonAlloc(transform.position, _range, _overlapResults, _targetLayerMask);

            for (int i = 0; i < _overlapResults.Count; i++)
            {
                var target = _overlapResults[i].GetComponent<IDamageable>();
                if (target == null)
                    target = _overlapResults[i].GetComponentInParent<IDamageable>();
                target?.TakeDamage(_damage, DamageSource.PlayerMelee);
            }
        }
    }
}
