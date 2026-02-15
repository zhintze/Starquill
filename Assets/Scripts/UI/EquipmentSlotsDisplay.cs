using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class EquipmentSlotsDisplay : MonoBehaviour
    {
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Button autoEquipButton;

        private static readonly Color[] RarityColors =
        {
            new Color(0.7f, 0.7f, 0.7f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.3f, 0.5f, 1f),
            new Color(0.7f, 0.3f, 0.9f),
            new Color(1f, 0.6f, 0.1f),
        };

        private static readonly string[] SlotNames =
        {
            "Head", "Torso", "Arms", "Legs", "Feet",
            "Main Hand", "Off Hand",
            "Misc 1", "Misc 2", "Misc 3", "Misc 4"
        };

        public event Action<int> OnSlotTapped;

        public void Initialize()
        {
            if (autoEquipButton != null)
                autoEquipButton.interactable = false;
        }

        public void Refresh(CharacterInstance character)
        {
            if (slotsContainer == null || character == null) return;

            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);

            for (int i = 0; i < character.equipment.Length; i++)
            {
                var slot = CreateSlot(i, character.equipment[i]);
                slot.transform.SetParent(slotsContainer, false);
            }
        }

        private GameObject CreateSlot(int slotIndex, EquipmentInstance item)
        {
            var slot = new GameObject($"Slot_{slotIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90, 90);

            var img = slot.GetComponent<Image>();
            if (item != null)
            {
                int rarityIdx = (int)item.Rarity;
                img.color = rarityIdx < RarityColors.Length ? RarityColors[rarityIdx] : RarityColors[0];
            }
            else
            {
                img.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);
            }

            var btn = slot.GetComponent<Button>();
            int idx = slotIndex;
            btn.onClick.AddListener(() => OnSlotTapped?.Invoke(idx));

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(slot.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4, 4);
            labelRT.offsetMax = new Vector2(-4, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = item != null ? $"{item.DisplayName}" : SlotNames[slotIndex];
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = item != null ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            tmp.enableWordWrapping = true;

            return slot;
        }
    }
}
