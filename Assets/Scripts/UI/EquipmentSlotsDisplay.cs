using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class EquipmentSlotsDisplay : MonoBehaviour
    {
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Button autoEquipButton;

        private int selectedSlotIndex = -1;
        private CharacterInstance currentCharacter;

        public event Action<int> OnSlotTapped;
        public event Action OnOptimizeTapped;

        // Slot positions relative to container center (110x110 tiles, 8px gap = 118px spacing)
        // Layout:
        //      [Head]  [Arms]
        // [Main][Chest][Misc1]
        // [Off] [Legs] [Misc2]
        //       [Shoes][Misc3]
        //              [Misc4]
        private static readonly Vector2[] SlotPositions = new Vector2[11]
        {
            new(0, 236),       // 0: Head - center top
            new(0, 118),       // 1: Torso/Chest - center row 2
            new(118, 236),     // 2: Arms - right of head
            new(0, 0),         // 3: Legs - center row 3
            new(0, -118),      // 4: Feet/Shoes - center row 4
            new(-118, 118),    // 5: MainHand - left row 2
            new(-118, 0),      // 6: OffHand - left row 3
            new(118, 118),     // 7: Misc1 - right row 2
            new(118, 0),       // 8: Misc2 - right row 3
            new(118, -118),    // 9: Misc3 - right row 4
            new(118, -236),    // 10: Misc4 - right row 5
        };

        private static readonly string[] SlotLabels =
        {
            "Head", "Chest", "Arms", "Legs", "Shoes",
            "Main", "Off",
            "Misc", "Misc", "Misc", "Misc"
        };

        public void Initialize()
        {
            if (autoEquipButton != null)
            {
                autoEquipButton.onClick.AddListener(() => OnOptimizeTapped?.Invoke());
                autoEquipButton.interactable = true;
            }
        }

        public void SelectSlot(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            if (currentCharacter != null) Refresh(currentCharacter);
        }

        public void ClearSelection()
        {
            selectedSlotIndex = -1;
            if (currentCharacter != null) Refresh(currentCharacter);
        }

        public void Refresh(CharacterInstance character)
        {
            currentCharacter = character;
            if (slotsContainer == null || character == null) return;

            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);

            for (int i = 0; i < character.equipment.Length; i++)
            {
                var slot = CreateSlot(i, character.equipment[i], i == selectedSlotIndex);
                slot.transform.SetParent(slotsContainer, false);
            }
        }

        private GameObject CreateSlot(int slotIndex, EquipmentInstance item, bool isSelected)
        {
            var slot = new GameObject($"Slot_{slotIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110, 110);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            if (slotIndex < SlotPositions.Length)
                rt.anchoredPosition = SlotPositions[slotIndex];

            var img = slot.GetComponent<Image>();
            if (item != null)
            {
                Color rarityColor = ItemDisplayData.GetRarityColor(item.Rarity);
                img.color = isSelected
                    ? Color.Lerp(rarityColor, Color.white, 0.3f)
                    : rarityColor;
            }
            else
            {
                img.color = isSelected
                    ? new Color(0.3f, 0.3f, 0.35f, 0.8f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.5f);
            }

            // Highlight ring for selected slot
            if (isSelected)
            {
                var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(slot.transform, false);
                var ringRT = ring.GetComponent<RectTransform>();
                ringRT.anchorMin = Vector2.zero;
                ringRT.anchorMax = Vector2.one;
                ringRT.offsetMin = new Vector2(-3, -3);
                ringRT.offsetMax = new Vector2(3, 3);
                ring.transform.SetAsFirstSibling();
                var ringImg = ring.GetComponent<Image>();
                ringImg.color = new Color(1f, 0.84f, 0f, 0.8f); // Gold highlight
            }

            // Slot label
            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(slot.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4, 4);
            labelRT.offsetMax = new Vector2(-4, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = item != null ? item.DisplayName : SlotLabels[slotIndex];
            tmp.fontSize = item != null ? 12 : 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = item != null ? Color.white : new Color(0.4f, 0.4f, 0.45f);
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var btn = slot.GetComponent<Button>();
            int idx = slotIndex;
            btn.onClick.AddListener(() =>
            {
                selectedSlotIndex = idx;
                OnSlotTapped?.Invoke(idx);
            });

            return slot;
        }
    }
}
