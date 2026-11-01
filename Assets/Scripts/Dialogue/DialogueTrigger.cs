using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.Dialogue
{
    /// <summary>
    /// Attach to a GameObject with a trigger Collider. When the player enters the
    /// trigger volume and no dialogue is running, starts a dialogue session.
    /// Useful for signposts, notice boards, and other static dialogue points.
    /// </summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField, Tooltip("The dialogue tree to play when the player enters.")]
        private DialogueData _dialogue;

        [Header("Speaker")]
        [SerializeField, Tooltip("MonoBehaviour on this object (or child) that implements IDialogueSpeaker.")]
        private MonoBehaviour _speakerBehaviour;

        [Header("Debug")]
        [SerializeField, Tooltip("If true, log trigger interactions to the console.")]
        private bool _verboseLogging = false;

        private IDialogueSpeaker _speaker;

        private void Awake()
        {
            _speaker = _speakerBehaviour as IDialogueSpeaker;
            if (_speaker == null && _speakerBehaviour != null)
            {
                Debug.LogWarning($"[DialogueTrigger] {_speakerBehaviour.name} does not implement IDialogueSpeaker.");
            }
        }

        private void OnValidate()
        {
            if (_speakerBehaviour != null && _speakerBehaviour is not IDialogueSpeaker)
            {
                Debug.LogWarning("[DialogueTrigger] _speakerBehaviour does not implement IDialogueSpeaker.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (DialogueRunner.Instance == null || DialogueRunner.Instance.IsRunning) return;
            if (_dialogue == null)
            {
                Debug.LogWarning($"[DialogueTrigger] No dialogue assigned on '{name}'.");
                return;
            }
            if (_speaker == null)
            {
                // Re-resolve in case the reference was set after Awake.
                _speaker = _speakerBehaviour as IDialogueSpeaker;
                if (_speaker == null)
                {
                    Debug.LogWarning($"[DialogueTrigger] No valid speaker on '{name}'.");
                    return;
                }
            }

            // Get player context via IPlayerContextProvider if available.
            var provider = other.GetComponent<IPlayerContextProvider>();
            PlayerContext ctx = provider != null ? provider.Current : default;

            Log($"Starting dialogue '{_dialogue.Id}' with '{_speaker.SpeakerId}'.");
            DialogueRunner.Instance.StartDialogue(_speaker, _dialogue, ctx);
        }

        private void Log(string msg)
        {
            if (_verboseLogging) Debug.Log($"[DialogueTrigger] {msg}");
        }
    }
}
