namespace Hearthwood.Core
{
    /// <summary>
    /// Single line of dialogue passed to <c>DialogueRunner.OnLineShown</c>.
    /// Pure data — no MonoBehaviour or Unity asset references.
    /// </summary>
    public readonly struct DialogueLine
    {
        /// <summary>Stable id of the speaker (matches <c>NPCData.Id</c> or "Player" for inner monologue).</summary>
        public readonly string SpeakerId;

        /// <summary>Localized display name of the speaker.</summary>
        public readonly string SpeakerName;

        /// <summary>The actual line of text.</summary>
        public readonly string Text;

        /// <summary>Index into the source <c>DialogueNode</c> for continuation.</summary>
        public readonly int NodeIndex;

        public DialogueLine(string speakerId, string speakerName, string text, int nodeIndex)
        {
            SpeakerId = speakerId;
            SpeakerName = speakerName;
            Text = text;
            NodeIndex = nodeIndex;
        }
    }
}
