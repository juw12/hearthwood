namespace Hearthwood.Core
{
    /// <summary>
    /// Read-only slot in an <c>InventoryComponent</c>. UI binds to this for grid rendering.
    /// </summary>
    [System.Serializable]
    public struct InventorySlot
    {
        [UnityEngine.SerializeField] private ItemData _item;
        [UnityEngine.SerializeField] private int _count;

        /// <summary>The item held in this slot. Null when empty.</summary>
        public ItemData Item => _item;

        /// <summary>Stack count. Always 0 when Item is null.</summary>
        public int Count => _count;

        public bool IsEmpty => _item == null || _count <= 0;

        public InventorySlot(ItemData item, int count)
        {
            _item = item;
            _count = count;
        }

        public static InventorySlot Empty => new InventorySlot(null, 0);
    }
}
