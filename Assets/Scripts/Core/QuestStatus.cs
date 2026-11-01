namespace Hearthwood.Core
{
    /// <summary>Lifecycle of a quest. The <see cref="QuestSystem"/> is the only writer.</summary>
    public enum QuestStatus
    {
        /// <summary>Available to start but not yet started.</summary>
        Available,

        /// <summary>Currently being completed.</summary>
        Active,

        /// <summary>All objectives completed; awaiting reward delivery.</summary>
        ReadyToComplete,

        /// <summary>Reward delivered, quest removed from active list.</summary>
        Completed,

        /// <summary>Quest failed (e.g. timed out or mutually exclusive with another quest).</summary>
        Failed,
    }
}
