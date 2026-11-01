namespace Hearthwood.Core
{
    /// <summary>
    /// Payload attached to <c>QuestSystem.Notify*</c> calls when something happens that
    /// may affect a quest objective.
    /// </summary>
    public enum QuestEvent
    {
        CollectiblePicked,
        ItemDelivered,
        NpcTalkedTo,
        AreaEntered,
        EnemyKilled,
        TimePassed,
        DialogueEnded,
    }

    /// <summary>
    /// Implemented by anything that can be a target of a quest objective: NPCs (TalkTo),
    /// collectibles (Pickup), area triggers (ReachArea), enemies (Kill).
    /// </summary>
    public interface IQuestTarget
    {
        /// <summary>Stable id of this target. Matches objective IDs in <c>QuestData</c>.</summary>
        string TargetId { get; }

        /// <summary>Current quest state for a given quest id, or null if not involved.</summary>
        QuestStatus? GetQuestStatus(string questId);

        /// <summary>
        /// Called by the quest system to nudge this target's state forward
        /// (e.g. set "talked to" flag after a conversation ends).
        /// </summary>
        void Notify(string questId, QuestEvent evt, object payload = null);
    }
}
