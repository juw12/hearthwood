namespace Hearthwood.Core
{
    /// <summary>
    /// Implemented by anything that can speak: NPCs, signposts, ghost echoes. Provides the
    /// dialogue runner with metadata needed to display the line.
    /// </summary>
    public interface IDialogueSpeaker
    {
        /// <summary>Stable id (matches <c>NPCData.Id</c> or a hardcoded speaker key).</summary>
        string SpeakerId { get; }

        /// <summary>Localized display name shown in the dialogue panel header.</summary>
        string SpeakerName { get; }

        /// <summary>
        /// Default dialogue asset. May be overridden at runtime by the speaker
        /// (e.g. quest-gated dialogue returns a different DialogueData based on ctx).
        /// </summary>
        DialogueData GetDefaultDialogue();

        /// <summary>
        /// Optional override that returns a different dialogue depending on quest state
        /// or player faction. Default impl returns <see cref="GetDefaultDialogue"/>.
        /// </summary>
        DialogueData GetDialogueForContext(PlayerContext ctx)
            => GetDefaultDialogue();
    }
}
