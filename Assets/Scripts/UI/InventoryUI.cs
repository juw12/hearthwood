using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Hearthwood.Core;
using Hearthwood.Player;

namespace Hearthwood.UI
{
    /// <summary>
    /// Inventory modal (I key). Renders a grid of <see cref="InventoryComponent.Capacity"/>
    /// slots showing each item's icon and stack count, with a hover tooltip. Driven by the
    /// inventory's <c>OnInventoryChanged</c> event and the game state machine.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField, Tooltip("The player inventory. Falls back to the player controller's InventoryComponent if null.")]
        private InventoryComponent _inventory;

        [Header("Grid")]
        [SerializeField, Tooltip("Parent transform for slot instances (ideally under a GridLayoutGroup).")]
        private Transform _gridParent;

        [SerializeField, Tooltip("Slot template (an Image + a count TextMeshProUGUI; name them 'Icon'/'Count'). Cloned Capacity times.")]
        private GameObject _slotTemplate;

        [Header("Tooltip")]
        [SerializeField, Tooltip("Tooltip panel root.")]
        private GameObject _tooltipRoot;

        [SerializeField, Tooltip("Tooltip item name.")]
        private TextMeshProUGUI _tooltipTitle;

        [SerializeField, Tooltip("Tooltip item description.")]
        private TextMeshProUGUI _tooltipDescription;

        private InventoryComponent _inv;
        private readonly List<SlotView> _slots = new List<SlotView>();
        private GameObject _panel;

        private void Start()
        {
            _panel = transform.gameObject;
            _inv = _inventory != null
                ? _inventory
                : (PlayerController3D.Instance != null ? PlayerController3D.Instance.GetComponent<InventoryComponent>() : null);
            if (_inv != null)
                _inv.OnInventoryChanged += Refresh;
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            BuildGrid();
            Refresh();
            if (_tooltipRoot != null) _tooltipRoot.SetActive(false);
            SetPanelActive(false);
        }

        private void OnDestroy()
        {
            if (_inv != null)
                _inv.OnInventoryChanged -= Refresh;
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        /// <summary>Toggle the inventory open/closed (drives the game state machine).</summary>
        public void Toggle()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Inventory) { gm.SetState(GameState.Exploring); return; }
            if (gm.State != GameState.Exploring) return;
            gm.SetState(GameState.Inventory);
            Refresh();
        }

        // ─── Grid ────────────────────────────────────────────────────────────────────────
        private void BuildGrid()
        {
            if (_inv == null)
            {
                Debug.LogWarning("[InventoryUI] No inventory found; grid not built.");
                return;
            }
            if (_slotTemplate == null || _gridParent == null)
            {
                Debug.LogWarning("[InventoryUI] Slot template or grid parent not assigned; grid not built.");
                return;
            }

            for (int i = 0; i < _inv.Capacity; i++)
            {
                var slot = Instantiate(_slotTemplate, _gridParent);
                slot.SetActive(true);
                var view = slot.AddComponent<SlotView>();
                view.Initialize(this, i);
                _slots.Add(view);
            }
        }

        private void Refresh()
        {
            if (_inv == null) return;
            var slots = _inv.Slots;
            for (int i = 0; i < _slots.Count && i < slots.Count; i++)
                _slots[i].SetItem(slots[i]);
        }

        // ─── Tooltip ─────────────────────────────────────────────────────────────────────
        private void ShowTooltip(int slotIndex)
        {
            if (_inv == null || _tooltipRoot == null) return;
            var slots = _inv.Slots;
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            var slot = slots[slotIndex];
            if (slot.IsEmpty || slot.Item == null)
            {
                _tooltipRoot.SetActive(false);
                return;
            }
            if (_tooltipTitle != null) _tooltipTitle.text = slot.Item.DisplayName;
            if (_tooltipDescription != null) _tooltipDescription.text = slot.Item.Description;
            _tooltipRoot.SetActive(true);
        }

        private void HideTooltip()
        {
            if (_tooltipRoot != null) _tooltipRoot.SetActive(false);
        }

        // ─── State sync ──────────────────────────────────────────────────────────────────
        private void HandleStateChanged(GameState from, GameState to)
        {
            SetPanelActive(to == GameState.Inventory);
        }

        private void SetPanelActive(bool active)
        {
            if (_panel != null) _panel.SetActive(active);
        }

        // ─── Per-slot hover handler (private nested component) ───────────────────────────
        private sealed class SlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private InventoryUI _owner;
            private int _index;
            private Image _icon;
            private TextMeshProUGUI _count;

            public void Initialize(InventoryUI owner, int index)
            {
                _owner = owner;
                _index = index;
                var iconTf = transform.Find("Icon");
                _icon = iconTf != null ? iconTf.GetComponent<Image>() : GetComponentInChildren<Image>(true);
                var countTf = transform.Find("Count");
                _count = countTf != null ? countTf.GetComponent<TextMeshProUGUI>() : GetComponentInChildren<TextMeshProUGUI>(true);
            }

            public void SetItem(InventorySlot slot)
            {
                if (_icon != null)
                {
                    _icon.sprite = slot.Item != null ? slot.Item.Icon : null;
                    _icon.enabled = slot.Item != null;
                }
                if (_count != null)
                    _count.text = slot.Count > 1 ? slot.Count.ToString() : string.Empty;
            }

            public void OnPointerEnter(PointerEventData eventData) => _owner?.ShowTooltip(_index);
            public void OnPointerExit(PointerEventData eventData) => _owner?.HideTooltip();
        }
    }
}
