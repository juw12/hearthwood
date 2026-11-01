namespace Hearthwood.Core
{
    /// <summary>
    /// Implemented by world pickups: herbs, coins, quest items. On contact with the player
    /// trigger, <see cref="OnPicked"/> is called which mutates the player's inventory and
    /// notifies the quest system.
    /// </summary>
    public interface ICollectible
    {
        /// <summary>Asset describing this collectible (id, display name, prompt, quest link).</summary>
        CollectibleData Data { get; }

        /// <summary>True if the player in <paramref name="ctx"/> may pick this up right now.</summary>
        bool CanPick(PlayerContext ctx);

        /// <summary>
        /// Adds the item to the inventory and notifies the quest system. The collectible
        /// is responsible for de-spawning itself (gameObject.SetActive(false) + return to pool).
        /// </summary>
        void OnPicked(IInventoryPort inventory);
    }

    /// <summary>
    /// Lightweight proxy so <see cref="ICollectible"/> does not depend on the Player assembly.
    /// The concrete <c>InventoryComponent</c> lives in <c>Hearthwood.Player</c> and implements this.
    /// </summary>
    public interface IInventoryPort
    {
        bool TryAdd(ItemData item, int count);
        bool HasItem(ItemData item, int count = 1);
    }
}
