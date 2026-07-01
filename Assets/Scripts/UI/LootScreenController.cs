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

            var gm = GameManager.Instance;
            int questLevel = gm != null ? gm.questLevel : 1;

            foreach (var item in sorted)
            {
                var data = ItemDisplayData.FromItem(item, questLevel,
                    gm != null ? gm.AbilityTable : null,
                    gm != null ? gm.economyConfig : null);

                var capturedItem = item;
                ItemCardBuilder.Build(data, item, new ItemCardBuilder.CardOptions
                {
                    CompareAgainst = FindBestUpgradeTarget(item),
                    ShowSprite = true,
                    SlotIndexForSprite = (int)item.Slot,
                    OnTapped = () => ShowItemDetail(capturedItem)
                }, itemGridContainer);
            }
        }

        // Equipped item of the roster character for whom this item is the biggest upgrade.
        private EquipmentInstance FindBestUpgradeTarget(EquipmentInstance item)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Roster == null || gm.Roster.Characters.Count == 0) return null;

            EquipmentInstance bestEquipped = null;
            float bestDelta = float.MinValue;
            foreach (var character in gm.Roster.Characters)
            {
                var equipped = character.equipment[(int)item.Slot];
                var diff = ItemComparer.Compare(item, equipped);
                if (diff.TotalDelta > bestDelta)
                {
                    bestDelta = diff.TotalDelta;
                    bestEquipped = equipped;
                }
            }
            return bestEquipped;
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
