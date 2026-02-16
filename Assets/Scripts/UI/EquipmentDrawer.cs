using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class EquipmentDrawer : MonoBehaviour
    {
        [SerializeField] private GameObject drawerPanel;
        [SerializeField] private TMP_Text headerLabel;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Transform itemListContainer;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button backButton;

        private EquipmentInstance equippedItem;
        private EquipmentInstance previewedItem;
        private int selectedSlotIndex = -1;

        public bool IsOpen { get; private set; }
        public event Action<EquipmentInstance> OnEquipPressed;
        public event Action OnBackPressed;

        public void Initialize()
        {
            if (equipButton != null)
                equipButton.onClick.AddListener(() =>
                {
                    if (previewedItem != null)
                    {
                        OnEquipPressed?.Invoke(previewedItem);
                        previewedItem = null;
                    }
                });
            if (backButton != null)
                backButton.onClick.AddListener(() =>
                {
                    previewedItem = null;
                    OnBackPressed?.Invoke();
                    Close();
                });
            Close();
        }

        public void Open(int slotIndex, EquipmentInstance equipped, List<EquipmentInstance> candidates)
        {
            selectedSlotIndex = slotIndex;
            equippedItem = equipped;
            previewedItem = null;
            IsOpen = true;

            if (drawerPanel != null) drawerPanel.SetActive(true);

            RefreshHeader();
            RefreshItemList(candidates, equipped);
            RefreshButtons();
        }

        public void Close()
        {
            IsOpen = false;
            previewedItem = null;
            selectedSlotIndex = -1;
            if (drawerPanel != null) drawerPanel.SetActive(false);
        }

        public void UpdateSummary(string text)
        {
            if (summaryLabel != null) summaryLabel.text = text;
        }

        private void RefreshHeader()
        {
            if (headerLabel == null) return;

            if (equippedItem != null)
            {
                var data = ItemDisplayData.FromItem(equippedItem, 1);
                headerLabel.text = $"<color=#{ColorUtility.ToHtmlStringRGB(data.RarityColor)}>{data.DisplayName}</color>\n" +
                                   $"<size=14>{data.StatSummary}</size>";
            }
            else
            {
                string slotName = ItemDisplayData.GetSlotName((EquipmentSlot)selectedSlotIndex);
                headerLabel.text = $"Empty {slotName} \u2014 Equip something!";
            }
        }

        private void RefreshItemList(List<EquipmentInstance> candidates, EquipmentInstance equipped)
        {
            if (itemListContainer == null) return;

            for (int i = itemListContainer.childCount - 1; i >= 0; i--)
                Destroy(itemListContainer.GetChild(i).gameObject);

            if (candidates == null || candidates.Count == 0)
            {
                var emptyLabel = new GameObject("Empty", typeof(RectTransform));
                emptyLabel.transform.SetParent(itemListContainer, false);
                var tmp = emptyLabel.AddComponent<TextMeshProUGUI>();
                tmp.text = "No items for this slot";
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.5f, 0.5f, 0.55f);
                var le = emptyLabel.AddComponent<LayoutElement>();
                le.preferredHeight = 60;
                le.flexibleWidth = 1;
                return;
            }

            // Find best item for BEST badge
            var best = ItemComparer.FindBestForSlot((EquipmentSlot)selectedSlotIndex, candidates, equipped);

            // Sort by score descending (best first)
            candidates.Sort((a, b) => ItemComparer.ScoreItem(b).CompareTo(ItemComparer.ScoreItem(a)));

            foreach (var item in candidates)
            {
                bool isBest = item == best;
                var row = CreateItemRow(item, equipped, isBest);
                row.transform.SetParent(itemListContainer, false);
            }
        }

        private GameObject CreateItemRow(EquipmentInstance item, EquipmentInstance equipped, bool isBest)
        {
            var row = new GameObject($"Item_{item.DisplayName}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 70);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            le.flexibleWidth = 1;

            var img = row.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f);

            // Rarity stripe (left border)
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(row.transform, false);
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0, 0);
            stripeRT.anchorMax = new Vector2(0, 1);
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(5, 0);
            stripeRT.anchoredPosition = Vector2.zero;
            stripe.GetComponent<Image>().color = ItemDisplayData.GetRarityColor(item.Rarity);

            // Item name + rarity (left side)
            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(row.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0);
            nameRT.anchorMax = new Vector2(0.6f, 1);
            nameRT.offsetMin = new Vector2(12, 4);
            nameRT.offsetMax = new Vector2(0, -4);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            string bestTag = isBest ? " <color=#FFD700>\u2605 BEST</color>" : "";
            nameTmp.text = $"<b>{item.DisplayName}</b>{bestTag}\n<size=12><color=#999>{item.Rarity}</color></size>";
            nameTmp.fontSize = 16;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = Color.white;
            nameTmp.richText = true;

            // Delta badge (right side)
            var delta = ItemComparer.Compare(item, equipped);
            var deltaObj = new GameObject("Delta", typeof(RectTransform));
            deltaObj.transform.SetParent(row.transform, false);
            var deltaRT = deltaObj.GetComponent<RectTransform>();
            deltaRT.anchorMin = new Vector2(0.6f, 0);
            deltaRT.anchorMax = new Vector2(1, 1);
            deltaRT.offsetMin = new Vector2(0, 4);
            deltaRT.offsetMax = new Vector2(-8, -4);
            var deltaTmp = deltaObj.AddComponent<TextMeshProUGUI>();
            string sign = delta.TotalDelta >= 0 ? "+" : "";
            string arrow = delta.IsUpgrade ? "\u25B2" : (delta.TotalDelta < 0 ? "\u25BC" : "\u2013");
            Color deltaColor = delta.IsUpgrade ? new Color(0.3f, 0.9f, 0.3f)
                : (delta.TotalDelta < 0 ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.6f, 0.6f, 0.6f));
            deltaTmp.text = $"{arrow} {sign}{delta.TotalDelta:F0}";
            deltaTmp.fontSize = 22;
            deltaTmp.alignment = TextAlignmentOptions.MidlineRight;
            deltaTmp.color = deltaColor;

            // Tap to preview
            var btn = row.GetComponent<Button>();
            var capturedItem = item;
            btn.onClick.AddListener(() =>
            {
                previewedItem = capturedItem;
                RefreshButtons();
            });

            return row;
        }

        private void RefreshButtons()
        {
            if (equipButton != null)
                equipButton.interactable = previewedItem != null;
        }

        private void OnDestroy()
        {
            if (equipButton != null) equipButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }
    }
}
