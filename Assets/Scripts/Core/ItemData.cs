using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>Top-level item category. Drives inventory UI sorting and merchant menus.</summary>
    public enum ItemType
    {
        Generic,
        Consumable,
        Weapon,
        Armor,
        Quest,
        Material,
        Currency,
        Key,
    }

    /// <summary>
    /// Canonical inventory item definition. Both inventory and shop systems bind to this.
    /// Create assets via Assets ▸ Create ▸ Hearthwood ▸ Items ▸ Item.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "Hearthwood/Items/Item", order = 100)]
    public sealed class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable id used by save data. NEVER change after release.")]
        private string _id = "item_";

        [SerializeField, Tooltip("Localized display name shown in UI.")]
        private string _displayName = "Item";

        [SerializeField, TextArea(2, 6), Tooltip("Flavor description. Optional.")]
        private string _description = "";

        [Header("Visuals")]
        [SerializeField, Tooltip("Icon used in inventory UI and tooltips.")]
        private Sprite _icon;

        [Header("Stacking")]
        [SerializeField, Min(1), Tooltip("Maximum stack count. 1 for non-stackable items.")]
        private int _maxStack = 1;

        [SerializeField, Tooltip("Item category. Drives UI sorting.")]
        private ItemType _type = ItemType.Generic;

        [Header("Economy")]
        [SerializeField, Min(0), Tooltip("Base sell value.")]
        private int _basePrice = 0;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MaxStack => _maxStack;
        public ItemType Type => _type;
        public int BasePrice => _basePrice;
    }
}
