using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Core;
using Starquill.Display;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class EquipmentDrawer : MonoBehaviour
    {
        [SerializeField] private GameObject drawerPanel;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Transform itemListContainer;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button backButton;

        // Selected slot card elements
        [SerializeField] private Image selectedSlotStripe;
        [SerializeField] private TMP_Text selectedSlotLabel;
        [SerializeField] private TMP_Text selectedSlotNameText;
        [SerializeField] private TMP_Text selectedSlotInfoText;
        [SerializeField] private Image selectedSlotSprite;
        [SerializeField] private TMP_Text selectedSlotStats;

        private EquipmentInstance equippedItem;
        private EquipmentInstance previewedItem;
        private int selectedSlotIndex = -1;
        private Coroutine slideCoroutine;
        private Vector2 openPosition;

        private static readonly string[] SlotLabels =
        {
            "Head", "Chest", "Arms", "Legs", "Shoes",
            "Main Hand", "Off Hand",
            "Accessory", "Accessory", "Accessory", "Accessory"
        };

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

            if (drawerPanel != null)
                openPosition = drawerPanel.GetComponent<RectTransform>().anchoredPosition;

            Close();
        }

        public void Open(int slotIndex, EquipmentInstance equipped, List<EquipmentInstance> candidates)
        {
            selectedSlotIndex = slotIndex;
            equippedItem = equipped;
            previewedItem = null;
            IsOpen = true;

            if (drawerPanel != null)
            {
                drawerPanel.SetActive(true);
                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(AnimateSlideIn());
            }

            RefreshSelectedSlot();
            RefreshItemList(candidates, equipped);
            RefreshButtons();
        }

        public void Close()
        {
            IsOpen = false;
            previewedItem = null;
            selectedSlotIndex = -1;
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }
            if (drawerPanel != null) drawerPanel.SetActive(false);
        }

        public void UpdateSummary(string text)
        {
            if (summaryLabel != null) summaryLabel.text = text;
        }

        private IEnumerator AnimateSlideIn()
        {
            var rt = drawerPanel.GetComponent<RectTransform>();
            Vector2 closedPos = openPosition + Vector2.down * 800;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                rt.anchoredPosition = Vector2.Lerp(closedPos, openPosition, t);
                yield return null;
            }
            rt.anchoredPosition = openPosition;
            slideCoroutine = null;
        }

        private void RefreshSelectedSlot()
        {
            if (selectedSlotIndex < 0) return;

            string slotName = selectedSlotIndex < SlotLabels.Length ? SlotLabels[selectedSlotIndex] : "Slot";

            if (selectedSlotLabel != null)
                selectedSlotLabel.text = slotName;

            if (equippedItem != null)
            {
                if (selectedSlotNameText != null)
                {
                    selectedSlotNameText.text = equippedItem.DisplayName;
                    selectedSlotNameText.fontStyle = FontStyles.Bold;
                    selectedSlotNameText.color = Color.white;
                }

                if (selectedSlotInfoText != null)
                {
                    selectedSlotInfoText.text = $"{equippedItem.Rarity}";
                    selectedSlotInfoText.color = ItemDisplayData.GetRarityColor(equippedItem.Rarity);
                }

                if (selectedSlotStripe != null)
                    selectedSlotStripe.color = ItemDisplayData.GetRarityColor(equippedItem.Rarity);

                LoadEquipmentSprite(selectedSlotSprite, equippedItem, selectedSlotIndex);

                if (selectedSlotStats != null)
                {
                    var displayData = ItemDisplayData.FromItem(equippedItem, 1);
                    selectedSlotStats.text = $"<size=12>{displayData.StatSummary}</size>";
                }
            }
            else
            {
                if (selectedSlotNameText != null)
                {
                    selectedSlotNameText.text = "Empty";
                    selectedSlotNameText.fontStyle = FontStyles.Italic;
                    selectedSlotNameText.color = new Color(0.4f, 0.4f, 0.45f);
                }

                if (selectedSlotInfoText != null)
                    selectedSlotInfoText.text = "";

                if (selectedSlotStripe != null)
                    selectedSlotStripe.color = new Color(0.25f, 0.25f, 0.3f);

                if (selectedSlotSprite != null)
                    selectedSlotSprite.color = Color.clear;

                if (selectedSlotStats != null)
                    selectedSlotStats.text = "";
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

            var best = ItemComparer.FindBestForSlot((EquipmentSlot)selectedSlotIndex, candidates, equipped);
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
            rt.sizeDelta = new Vector2(0, 280);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 280;
            le.flexibleWidth = 1;

            var img = row.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.15f);

            // Rarity stripe (6px wide, full height)
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(row.transform, false);
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0, 0);
            stripeRT.anchorMax = new Vector2(0, 1);
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(6, 0);
            stripeRT.anchoredPosition = Vector2.zero;
            stripe.GetComponent<Image>().color = isBest
                ? new Color(1f, 0.84f, 0f)
                : ItemDisplayData.GetRarityColor(item.Rarity);

            // === Left column (0-45%): slot type, item name, rarity ===

            // Top-left: Best badge + slot type
            string slotName = selectedSlotIndex < SlotLabels.Length ? SlotLabels[selectedSlotIndex] : "Slot";
            var slotLabel = new GameObject("SlotLabel", typeof(RectTransform));
            slotLabel.transform.SetParent(row.transform, false);
            var slotLabelRT = slotLabel.GetComponent<RectTransform>();
            slotLabelRT.anchorMin = new Vector2(0, 0.7f);
            slotLabelRT.anchorMax = new Vector2(0.45f, 1f);
            slotLabelRT.offsetMin = new Vector2(16, 0);
            slotLabelRT.offsetMax = new Vector2(0, -8);
            var slotTmp = slotLabel.AddComponent<TextMeshProUGUI>();
            slotTmp.text = isBest ? $"\u2605 Best {slotName}" : slotName;
            slotTmp.fontSize = 14;
            slotTmp.color = isBest ? new Color(1f, 0.84f, 0f) : new Color(0.5f, 0.5f, 0.55f);
            slotTmp.fontStyle = isBest ? FontStyles.Bold : FontStyles.Normal;
            slotTmp.alignment = TextAlignmentOptions.BottomLeft;
            slotTmp.richText = true;

            // Middle-left: Item name
            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(row.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.35f);
            nameRT.anchorMax = new Vector2(0.45f, 0.7f);
            nameRT.offsetMin = new Vector2(16, 0);
            nameRT.offsetMax = new Vector2(0, 0);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = item.DisplayName;
            nameTmp.fontSize = 18;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Bottom-left: Rarity
            var infoObj = new GameObject("Info", typeof(RectTransform));
            infoObj.transform.SetParent(row.transform, false);
            var infoRT = infoObj.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0, 0);
            infoRT.anchorMax = new Vector2(0.45f, 0.35f);
            infoRT.offsetMin = new Vector2(16, 8);
            infoRT.offsetMax = new Vector2(0, 0);
            var infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
            infoTmp.text = $"{item.Rarity}";
            infoTmp.fontSize = 13;
            infoTmp.color = ItemDisplayData.GetRarityColor(item.Rarity);
            infoTmp.alignment = TextAlignmentOptions.TopLeft;

            // === Middle column (45-70%): Equipment display sprite ===
            var spriteObj = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
            spriteObj.transform.SetParent(row.transform, false);
            var spriteRT = spriteObj.GetComponent<RectTransform>();
            spriteRT.anchorMin = new Vector2(0.45f, 0.05f);
            spriteRT.anchorMax = new Vector2(0.70f, 0.95f);
            spriteRT.offsetMin = Vector2.zero;
            spriteRT.offsetMax = Vector2.zero;
            var spriteImg = spriteObj.GetComponent<Image>();
            spriteImg.preserveAspect = true;
            LoadEquipmentSprite(spriteImg, item, selectedSlotIndex);

            // === Right column (70-100%): Stats ===
            var delta = ItemComparer.Compare(item, equipped);
            var statsObj = new GameObject("Stats", typeof(RectTransform));
            statsObj.transform.SetParent(row.transform, false);
            var statsRT = statsObj.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.70f, 0);
            statsRT.anchorMax = new Vector2(1, 1);
            statsRT.offsetMin = new Vector2(4, 8);
            statsRT.offsetMax = new Vector2(-8, -8);
            var statsTmp = statsObj.AddComponent<TextMeshProUGUI>();

            string sign = delta.TotalDelta >= 0 ? "+" : "";
            string arrow = delta.IsUpgrade ? "\u25B2" : (delta.TotalDelta < 0 ? "\u25BC" : "\u2013");
            Color deltaColor = delta.IsUpgrade ? new Color(0.3f, 0.9f, 0.3f)
                : (delta.TotalDelta < 0 ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.6f, 0.6f, 0.6f));
            string deltaHex = ColorUtility.ToHtmlStringRGB(deltaColor);

            var displayData = ItemDisplayData.FromItem(item, 1);
            statsTmp.text = $"<color=#{deltaHex}><size=22>{arrow} {sign}{delta.TotalDelta:F0}</size></color>\n\n<size=12>{displayData.StatSummary}</size>";
            statsTmp.fontSize = 16;
            statsTmp.alignment = TextAlignmentOptions.Center;
            statsTmp.color = new Color(0.7f, 0.7f, 0.75f);
            statsTmp.richText = true;

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

        private static void LoadEquipmentSprite(Image target, EquipmentInstance item, int slotIndex)
        {
            if (target == null || item == null) return;
            if (item.LayerCodes == null || item.LayerCodes.Length == 0)
            {
                target.color = Color.clear;
                return;
            }

            string spritePath;
            if (slotIndex == 5 || slotIndex == 6)
                spritePath = ImageToken.BuildWeaponSpritePath(item.ItemType, item.LayerCodes[0], item.ItemNum);
            else
                spritePath = ImageToken.BuildEquipmentSpritePath(item.ItemType, item.ItemNum, item.LayerCodes[0]);

            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                target.sprite = sprite;
                target.color = item.BaseColor;
                target.preserveAspect = true;
            }
            else
            {
                target.color = new Color(item.BaseColor.r, item.BaseColor.g, item.BaseColor.b, 0.3f);
            }
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
