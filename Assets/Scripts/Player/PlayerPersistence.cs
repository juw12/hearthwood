using System;
using Hearthwood.Core;
using UnityEngine;

namespace Hearthwood.Player
{
    /// <summary>
    /// Static persistence bridge for the player subsystem. Registers Fill/Apply delegates
    /// with <see cref="SnapShotFillerRegistry"/> so that <see cref="SaveSystem"/> can
    /// capture and restore player position and health without depending on the Player assembly.
    /// Called once by <c>PlayerController3D.Awake</c> after initialization.
    /// </summary>
    public static class PlayerPersistence
    {
        private static bool _registered;

        /// <summary>
        /// Register the player persistence fillers with <see cref="SnapShotFillerRegistry"/>.
        /// Safe to call multiple times (no-op if already registered).
        /// </summary>
        public static void Register()
        {
            if (_registered) return;
            _registered = true;
            SnapShotFillerRegistry.Register(Fill, Apply);
        }

        private static void Fill(SaveSystem.Snapshot snap)
        {
            if (snap == null) return;

            var player = PlayerController3D.Instance;
            if (player == null)
            {
                Debug.LogWarning("[PlayerPersistence] Fill: PlayerController3D.Instance is null — skipping player data.");
                return;
            }

            Vector3 pos = player.Transform.position;
            snap.playerX = pos.x;
            snap.playerY = pos.y;
            snap.playerZ = pos.z;

            var health = player.GetComponent<HealthComponent>();
            if (health != null)
            {
                snap.playerHealth = health.Current;
                snap.playerMaxHealth = health.Max;
            }
            else
            {
                Debug.LogWarning("[PlayerPersistence] Fill: HealthComponent not found on player.");
            }

            snap.playerFaction = "Player";
        }

        private static void Apply(SaveSystem.Snapshot snap)
        {
            if (snap == null) return;

            var player = PlayerController3D.Instance;
            if (player == null)
            {
                // Fallback: try to find the player in the scene (e.g. loading before player spawns).
                player = FindAnyObjectByType<PlayerController3D>();
            }

            if (player == null)
            {
                Debug.LogWarning("[PlayerPersistence] Apply: No PlayerController3D found — skipping player restore.");
                return;
            }

            // Restore position.
            player.Transform.position = new Vector3(snap.playerX, snap.playerY, snap.playerZ);

            // Restore health via public API.
            var health = player.GetComponent<HealthComponent>();
            if (health != null)
            {
                // Set max first (handles level changes between saves).
                if (Mathf.Abs(health.Max - snap.playerMaxHealth) > 0.01f)
                {
                    health.SetMax(snap.playerMaxHealth, false);
                }

                // Adjust current health to match saved value.
                float target = Mathf.Clamp(snap.playerHealth, 0f, health.Max);
                float diff = target - health.Current;

                if (diff > 0.01f)
                    health.Heal(diff);
                else if (diff < -0.01f)
                    health.TakeDamage(-diff, DamageSource.Generic);
            }
            else
            {
                Debug.LogWarning("[PlayerPersistence] Apply: HealthComponent not found on player — skipping health restore.");
            }

            // Faction is always "Player" in this game; no setter needed.
        }
    }
}
