namespace Hearthwood.Core
{
    /// <summary>
    /// Top-level game state. The state machine in <see cref="GameManager"/> is the single
    /// source of truth — UI and input subsystems read this to decide what action map is
    /// active and which modals are visible.
    /// </summary>
    public enum GameState
    {
        /// <summary>Default gameplay. Player controller active, "Player" action map enabled.</summary>
        Exploring,

        /// <summary>Conversation in progress. Player input muted, "Dialogue" action map enabled.</summary>
        Dialogue,

        /// <summary>Inventory panel open. World paused, "UI" action map enabled.</summary>
        Inventory,

        /// <summary>Quest log panel open (Tab). World paused, "UI" action map enabled.</summary>
        QuestLog,

        /// <summary>Game paused via Esc. Time.timeScale = 0, "UI" action map enabled.</summary>
        Paused,

        /// <summary>Non-interactive sequence (intro/outro). Player input muted.</summary>
        Cutscene,
    }
}
