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

                ItemCardBuilder.LoadEquipmentSprite(selectedSlotSprite, equippedItem, selectedSlotIndex);

                if (selectedSlotStats != null)
                {
                    var gm = Starquill.Managers.GameManager.Instance;
                    var displayData = ItemDisplayData.FromItem(equippedItem,
                        gm != null ? gm.questLevel : 1,
                        gm != null ? gm.AbilityTable : null,
                        gm != null ? gm.economyConfig : null);
                    string text = $"<size=14>{ItemDisplayData.StatLabelColored(displayData.PrimaryStat, displayData.PrimaryValue)}"
                        + $"  {ItemDisplayData.StatLabelColored(displayData.SecondaryStat, displayData.SecondaryValue)}</size>";
                    if (displayData.HasAbility)
                    {
                        string pips = ItemCardBuilder.BuildPips(displayData.AbilityLevel, displayData.AbilityMaxLevel);
                        text += $"\n<size=12>✦ {displayData.AbilityName} {pips}</size>";
                    }
                    selectedSlotStats.text = text;
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

            var gm = Starquill.Managers.GameManager.Instance;
            foreach (var item in candidates)
            {
                var data = ItemDisplayData.FromItem(item, gm != null ? gm.questLevel : 1,
                    gm != null ? gm.AbilityTable : null,
                    gm != null ? gm.economyConfig : null);

                var capturedItem = item;
                ItemCardBuilder.Build(data, item, new ItemCardBuilder.CardOptions
                {
                    CompareAgainst = equipped,
                    ShowBestTag = item == best,
                    ShowSprite = true,
                    SlotIndexForSprite = selectedSlotIndex,
                    OnTapped = () =>
                    {
                        previewedItem = capturedItem;
                        RefreshButtons();
                    }
                }, itemListContainer);
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
