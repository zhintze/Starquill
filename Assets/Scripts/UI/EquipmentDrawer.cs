using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Core;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Candidate list for one equipment slot. Tapping a candidate opens the
    /// ItemDetailSheet (comparison + EQUIP/Sell); the drawer itself has no
    /// preview or equip state.
    public class EquipmentDrawer : MonoBehaviour
    {
        [SerializeField] private GameObject drawerPanel;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Transform itemListContainer;
        [SerializeField] private Button backButton;

        private int selectedSlotIndex = -1;
        private int targetRosterIndex;
        private Coroutine slideCoroutine;
        private Vector2 openPosition;

        public bool IsOpen { get; private set; }
        /// Raised after the sheet mutates state (equip, sell, level-up).
        public event Action OnItemsChanged;
        public event Action OnBackPressed;

        public void Initialize()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() =>
                {
                    OnBackPressed?.Invoke();
                    Close();
                });

            if (drawerPanel != null)
                openPosition = drawerPanel.GetComponent<RectTransform>().anchoredPosition;

            Close();
        }

        public void Open(int slotIndex, EquipmentInstance equipped,
            List<EquipmentInstance> candidates, int rosterIndex)
        {
            selectedSlotIndex = slotIndex;
            targetRosterIndex = rosterIndex;
            IsOpen = true;

            if (drawerPanel != null)
            {
                drawerPanel.SetActive(true);
                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(AnimateSlideIn());
            }

            RefreshItemList(candidates, equipped);
        }

        public void Close()
        {
            IsOpen = false;
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

        private void RefreshItemList(List<EquipmentInstance> candidates, EquipmentInstance equipped)
        {
            if (itemListContainer == null) return;

            for (int i = itemListContainer.childCount - 1; i >= 0; i--)
                Destroy(itemListContainer.GetChild(i).gameObject);

            if (candidates == null || candidates.Count == 0)
            {
                UiFactory.EmptyState(itemListContainer, "No items for this slot");
                return;
            }

            var best = ItemComparer.FindBestForSlot((EquipmentSlot)selectedSlotIndex, candidates, equipped);
            candidates.Sort((a, b) => ItemComparer.ScoreItem(b).CompareTo(ItemComparer.ScoreItem(a)));

            var gm = GameManager.Instance;
            int questLevel = gm != null ? gm.questLevel : 1;

            foreach (var item in candidates)
            {
                var data = ItemDisplayData.FromItem(item, questLevel,
                    gm != null ? gm.AbilityTable : null,
                    gm != null ? gm.economyConfig : null);

                var capturedItem = item;
                ItemCardBuilder.Build(data, item, new ItemCardBuilder.CardOptions
                {
                    CompareAgainst = equipped,
                    ShowBestTag = item == best,
                    ShowSprite = true,
                    SlotIndexForSprite = selectedSlotIndex,
                    OnTapped = () => ItemDetailSheet.Show(transform.root, capturedItem,
                        questLevel, () => OnItemsChanged?.Invoke(),
                        fixedTargetIndex: targetRosterIndex)
                }, itemListContainer);
            }
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }
    }
}
