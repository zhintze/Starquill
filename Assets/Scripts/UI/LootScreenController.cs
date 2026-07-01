using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private Transform sortTabsContainer;

        private enum SortMode { Score, Newest, Rarity }
        private SortMode sortMode = SortMode.Score;

        private bool initialized;

        private System.Collections.IEnumerator Start()
        {
            yield return null; // wait for GameManager.Start()
            Initialize();
            Refresh();
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;

            if (optimizeAllButton != null)
                optimizeAllButton.onClick.AddListener(HandleOptimizeAll);
            if (screenManager != null)
                screenManager.OnScreenChanged += HandleScreenChanged;
            if (sortTabsContainer != null)
            {
                UiFactory.SegmentedTabs(sortTabsContainer,
                    new[] { "Score", "Newest", "Rarity" }, idx =>
                    {
                        sortMode = (SortMode)idx;
                        Refresh();
                    });
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
            RefreshList(gm.LootInventory);
        }

        private void RefreshHeader(LootInventory inventory)
        {
            if (inventoryCountLabel != null)
                inventoryCountLabel.text = $"Inventory {inventory.Count}/{inventory.Capacity}";
        }

        private void RefreshList(LootInventory inventory)
        {
            if (itemGridContainer == null) return;

            for (int i = itemGridContainer.childCount - 1; i >= 0; i--)
                Destroy(itemGridContainer.GetChild(i).gameObject);

            if (inventory.Count == 0)
            {
                UiFactory.EmptyState(itemGridContainer, "No loot yet · keep exploring");
                return;
            }

            var sorted = Sort(inventory.Items);

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

        private List<EquipmentInstance> Sort(IReadOnlyList<EquipmentInstance> items)
        {
            switch (sortMode)
            {
                case SortMode.Newest:
                    return items.Reverse().ToList();
                case SortMode.Rarity:
                    return items.OrderByDescending(i => (int)i.Rarity)
                        .ThenByDescending(ItemComparer.ScoreItem).ToList();
                default:
                    return items.OrderByDescending(ItemComparer.ScoreItem).ToList();
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
            var gm = GameManager.Instance;
            int questLevel = gm != null ? gm.questLevel : 1;
            ItemDetailSheet.Show(transform.root, item, questLevel, Refresh);
        }

        private void HandleOptimizeAll()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Roster == null) return;

            for (int i = 0; i < gm.Roster.Characters.Count; i++)
                gm.AutoEquipCharacter(i);

            Refresh();
        }

        private void OnDestroy()
        {
            if (screenManager != null)
                screenManager.OnScreenChanged -= HandleScreenChanged;
            if (optimizeAllButton != null)
                optimizeAllButton.onClick.RemoveAllListeners();
        }
    }
}
