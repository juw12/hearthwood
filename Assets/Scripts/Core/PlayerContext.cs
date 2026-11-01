using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// Read-only snapshot of the player at the moment an interaction started.
    /// Passed to <see cref="IInteractable.Interact"/> so the target can react to
    /// who is interacting (faction checks, quest gating, dialogue branching).
    /// </summary>
    public readonly struct PlayerContext
    {
        /// <summary>The interacting player's transform (null in some preview contexts).</summary>
        public readonly Transform Transform;

        /// <summary>Current health 0..max.</summary>
        public readonly float Health;

        /// <summary>Maximum health.</summary>
        public readonly float MaxHealth;

        /// <summary>Faction tags (e.g. "Villager", "Outsider"). Used for dialogue/quest gating.</summary>
        public readonly string Faction;

        /// <summary>Number of favor tokens the player currently holds.</summary>
        public readonly int FavorTokens;

        public PlayerContext(Transform transform, float health, float maxHealth, string faction, int favorTokens)
        {
            Transform = transform;
            Health = health;
            MaxHealth = maxHealth;
            Faction = faction;
            FavorTokens = favorTokens;
        }

        /// <summary>True if the player is alive.</summary>
        public bool IsAlive => Health > 0f;
    }
}
