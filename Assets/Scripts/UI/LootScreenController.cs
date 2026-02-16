using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    public class LootScreenController : MonoBehaviour
    {
        [SerializeField] private TMP_Text inventoryCountLabel;
        [SerializeField] private Transform itemGridContainer;
        [SerializeField] private Button optimizeAllButton;
        [SerializeField] private ScreenManager screenManager;
        [SerializeField] private ItemDetailPanel itemDetailPanel;

        public void Initialize()
        {
            if (optimizeAllButton != null)
                optimizeAllButton.onClick.AddListener(HandleOptimizeAll);
            if (screenManager != null)
                screenManager.OnScreenChanged += HandleScreenChanged;
            if (itemDetailPanel != null)
            {
                itemDetailPanel.Initialize();
                itemDetailPanel.OnSold += Refresh;
                itemDetailPanel.OnEquipped += Refresh;
                itemDetailPanel.OnClosed += Refresh;
            }
        }

        private void HandleScreenChanged(int screenIndex)
        {
            if (screenIndex == 2) Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            RefreshHeader(gm.LootInventory);
            RefreshGrid(gm.LootInventory);
        }

        private void RefreshHeader(LootInventory inventory)
        {
            if (inventoryCountLabel != null)
                inventoryCountLabel.text = $"Inventory: {inventory.Count}/{inventory.Capacity}";
        }

        private void RefreshGrid(LootInventory inventory)
        {
            if (itemGridContainer == null) return;

            for (int i = itemGridContainer.childCount - 1; i >= 0; i--)
                Destroy(itemGridContainer.GetChild(i).gameObject);

            var sorted = inventory.Items.OrderByDescending(ItemComparer.ScoreItem).ToList();

            foreach (var item in sorted)
            {
                var tile = CreateItemTile(item);
                tile.transform.SetParent(itemGridContainer, false);
            }
        }

        private GameObject CreateItemTile(EquipmentInstance item)
        {
            var tile = new GameObject($"Item_{item.DisplayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));

            var img = tile.GetComponent<Image>();
            Color rarityColor = ItemDisplayData.GetRarityColor(item.Rarity);
            img.color = rarityColor;

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(tile.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4, 4);
            labelRT.offsetMax = new Vector2(-4, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{item.DisplayName}\n<size=10>{item.Rarity} {ItemDisplayData.GetSlotName(item.Slot)}</size>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var btn = tile.GetComponent<Button>();
            var capturedItem = item;
            btn.onClick.AddListener(() => ShowItemDetail(capturedItem));

            return tile;
        }

        private void ShowItemDetail(EquipmentInstance item)
        {
            if (itemDetailPanel == null) return;
            var gm = GameManager.Instance;
            int questLevel = gm != null ? gm.questLevel : 1;
            itemDetailPanel.Show(item, questLevel);
        }

        private void HandleOptimizeAll()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Roster == null) return;

            int totalEquipped = 0;
            for (int i = 0; i < gm.Roster.Characters.Count; i++)
                totalEquipped += gm.AutoEquipCharacter(i);

            Refresh();
        }

        private void OnDestroy()
        {
            if (screenManager != null)
                screenManager.OnScreenChanged -= HandleScreenChanged;
            if (optimizeAllButton != null)
                optimizeAllButton.onClick.RemoveAllListeners();
            if (itemDetailPanel != null)
            {
                itemDetailPanel.OnSold -= Refresh;
                itemDetailPanel.OnEquipped -= Refresh;
                itemDetailPanel.OnClosed -= Refresh;
            }
        }
    }
}
