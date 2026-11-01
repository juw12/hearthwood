using System;
using System.Collections.Generic;
using Hearthwood.Core;
using UnityEngine;

namespace Hearthwood.Player
{
    /// <summary>
    /// Fixed-capacity grid inventory. Implements <see cref="IInventoryPort"/> for
    /// cross-subsystem item addition (e.g. Collectible.OnPicked). Stacking respects
    /// <see cref="ItemData.MaxStack"/>; overflow spills to the next available slot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryComponent : MonoBehaviour, IInventoryPort
    {
        [Header("Inventory")]
        [SerializeField, Min(1), Tooltip("Maximum number of grid slots.")]
        private int _capacity = 20;

        private List<InventorySlot> _slots;

        /// <summary>Total grid capacity in slots.</summary>
        public int Capacity => _capacity;

        /// <summary>Read-only view of all slots (including empty ones).</summary>
        public IReadOnlyList<InventorySlot> Slots => _slots;

        /// <summary>
        /// Raised after any mutation: <see cref="TryAdd"/> (success), <see cref="TryRemove"/> (success).
        /// Listeners: InventoryUI, HUD, QuestSystem.
        /// </summary>
        public event Action OnInventoryChanged;

        private void Awake()
        {
            _slots = new List<InventorySlot>(_capacity);
            for (int i = 0; i < _capacity; i++)
                _slots.Add(InventorySlot.Empty);
        }

        /// <summary>
        /// Returns true if the inventory holds at least <paramref name="count"/> of the given item
        /// across all slots (stacked).
        /// </summary>
        public bool HasItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0) return true;
            return GetCount(item) >= count;
        }

        /// <summary>
        /// Attempt to add <paramref name="count"/> copies of <paramref name="item"/>.
        /// Fills existing stacks first (respecting <see cref="ItemData.MaxStack"/>),
        /// then empty slots. Returns false if there is insufficient space.
        /// </summary>
        public bool TryAdd(ItemData item, int count = 1)
        {
            if (item == null || count <= 0) return false;

            int remaining = count;
            int maxStack = Mathf.Max(1, item.MaxStack);

            // Phase 1: fill existing stacks of the same item.
            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                if (_slots[i].Item != item) continue;
                int space = maxStack - _slots[i].Count;
                if (space <= 0) continue;
                int add = Mathf.Min(space, remaining);
                _slots[i] = new InventorySlot(item, _slots[i].Count + add);
                remaining -= add;
            }

            // Phase 2: fill empty slots.
            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                if (!_slots[i].IsEmpty) continue;
                int add = Mathf.Min(maxStack, remaining);
                _slots[i] = new InventorySlot(item, add);
                remaining -= add;
            }

            // Overflow: not all items fit.
            if (remaining > 0)
            {
                Debug.LogWarning($"[InventoryComponent] TryAdd overflow: {remaining}x '{item.DisplayName}' could not fit.");
                return false;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Attempt to remove <paramref name="count"/> copies of <paramref name="item"/>.
        /// Removes from the last slot containing the item first (preserves stack order).
        /// Returns false if insufficient items exist.
        /// </summary>
        public bool TryRemove(ItemData item, int count = 1)
        {
            if (item == null || count <= 0) return true;
            if (GetCount(item) < count) return false;

            int remaining = count;

            // Remove from last matching slot first (backwards iteration).
            for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_slots[i].Item != item) continue;
                int remove = Mathf.Min(_slots[i].Count, remaining);
                int newCount = _slots[i].Count - remove;
                _slots[i] = newCount > 0 ? new InventorySlot(item, newCount) : InventorySlot.Empty;
                remaining -= remove;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Returns the total count of <paramref name="item"/> across all slots.
        /// Returns 0 if item is null.
        /// </summary>
        public int GetCount(ItemData item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item == item)
                    total += _slots[i].Count;
            }
            return total;
        }
    }
}
