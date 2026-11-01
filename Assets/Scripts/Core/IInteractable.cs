namespace Hearthwood.Core
{
    /// <summary>
    /// Implemented by anything the player can interact with: NPCs, collectibles, doors,
    /// quest triggers. The player controller resolves the focused IInteractable each frame
    /// and calls <see cref="Interact"/> when the Interact input fires.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Localized prompt shown above the focus indicator (e.g. "Press E to Talk").</summary>
        string InteractPrompt { get; }

        /// <summary>True if the player in <paramref name="ctx"/> may interact right now.</summary>
        bool CanInteract(PlayerContext ctx);

        /// <summary>
        /// Trigger the interaction. May pause the game (e.g. starting dialogue),
        /// give items, or advance quests. Must not throw on failure — log and no-op.
        /// </summary>
        void Interact(PlayerContext ctx);

        /// <summary>
        /// Raised when the player controller's focus raycast enters/exits this interactable.
        /// Listeners (e.g. UI prompt) subscribe to show/hide the prompt.
        /// </summary>
        System.Action<bool> OnFocusChanged { get; set; }
    }
}
