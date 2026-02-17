using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Display;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    public class EquipmentSlotsDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Button autoEquipButton;

        private int selectedSlotIndex = -1;
        private CharacterInstance currentCharacter;
        private LootInventory inventory;

        public event Action<int> OnSlotTapped;
        public event Action OnOptimizeTapped;

        private static readonly int[][] CategorySlots =
        {
            new[] { 0, 1, 2, 3, 4 },    // Armor: Head, Torso, Arms, Legs, Feet
            new[] { 5, 6 },              // Weapons: MainHand, OffHand
            new[] { 7, 8, 9, 10 },       // Accessories: Misc1-4
        };

        private static readonly string[] CategoryNames = { "ARMOR", "WEAPONS", "ACCESSORIES" };

        private static readonly string[] SlotLabels =
        {
            "Head", "Chest", "Arms", "Legs", "Shoes",
            "Main Hand", "Off Hand",
            "Accessory", "Accessory", "Accessory", "Accessory"
        };

        public void Initialize()
        {
            if (autoEquipButton != null)
            {
                autoEquipButton.onClick.AddListener(() => OnOptimizeTapped?.Invoke());
                autoEquipButton.interactable = true;
            }

            var gm = GameManager.Instance;
            if (gm != null) inventory = gm.LootInventory;
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
            if (cardContainer == null || character == null) return;

            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);

            for (int cat = 0; cat < CategorySlots.Length; cat++)
            {
                var header = CreateCategoryHeader(CategoryNames[cat]);
                header.transform.SetParent(cardContainer, false);

                foreach (int slotIndex in CategorySlots[cat])
                {
                    EquipmentInstance item = slotIndex < character.equipment.Length
                        ? character.equipment[slotIndex]
                        : null;
                    var card = CreateCard(slotIndex, item, slotIndex == selectedSlotIndex);
                    card.transform.SetParent(cardContainer, false);
                }
            }
        }

        private GameObject CreateCategoryHeader(string categoryName)
        {
            var header = new GameObject($"Header_{categoryName}", typeof(RectTransform), typeof(Image));
            var rt = header.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);
            var le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.flexibleWidth = 1;

            var bg = header.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.13f);

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(header.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(16, 0);
            labelRT.offsetMax = Vector2.zero;
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = categoryName;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.5f, 0.5f, 0.55f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return header;
        }

        private GameObject CreateCard(int slotIndex, EquipmentInstance item, bool isSelected)
        {
            var card = new GameObject($"Card_{slotIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 140);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 140;
            le.flexibleWidth = 1;

            // Background
            var bg = card.GetComponent<Image>();
            bg.color = isSelected
                ? new Color(0.2f, 0.2f, 0.25f)
                : new Color(0.12f, 0.12f, 0.15f);

            // Left rarity stripe (6px wide, full height)
            Color stripeColor = item != null
                ? (isSelected ? new Color(1f, 0.84f, 0f) : ItemDisplayData.GetRarityColor(item.Rarity))
                : new Color(0.25f, 0.25f, 0.3f);
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(card.transform, false);
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0, 0);
            stripeRT.anchorMax = new Vector2(0, 1);
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(6, 0);
            stripeRT.anchoredPosition = Vector2.zero;
            stripe.GetComponent<Image>().color = stripeColor;

            // === Text area (left 60%) ===

            // Slot label (top-left, gray)
            var slotLabel = new GameObject("SlotLabel", typeof(RectTransform));
            slotLabel.transform.SetParent(card.transform, false);
            var slotLabelRT = slotLabel.GetComponent<RectTransform>();
            slotLabelRT.anchorMin = new Vector2(0, 0.7f);
            slotLabelRT.anchorMax = new Vector2(0.6f, 1f);
            slotLabelRT.offsetMin = new Vector2(16, 0);
            slotLabelRT.offsetMax = new Vector2(0, -8);
            var slotTmp = slotLabel.AddComponent<TextMeshProUGUI>();
            slotTmp.text = SlotLabels[slotIndex];
            slotTmp.fontSize = 14;
            slotTmp.color = new Color(0.5f, 0.5f, 0.55f);
            slotTmp.alignment = TextAlignmentOptions.BottomLeft;

            // Item name (middle-left, white bold)
            var nameLabel = new GameObject("Name", typeof(RectTransform));
            nameLabel.transform.SetParent(card.transform, false);
            var nameRT = nameLabel.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.35f);
            nameRT.anchorMax = new Vector2(0.6f, 0.7f);
            nameRT.offsetMin = new Vector2(16, 0);
            nameRT.offsetMax = new Vector2(0, 0);
            var nameTmp = nameLabel.AddComponent<TextMeshProUGUI>();
            nameTmp.text = item != null ? item.DisplayName : "Empty";
            nameTmp.fontSize = 18;
            nameTmp.fontStyle = item != null ? FontStyles.Bold : FontStyles.Italic;
            nameTmp.color = item != null ? Color.white : new Color(0.4f, 0.4f, 0.45f);
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Rarity + level info (bottom-left)
            if (item != null)
            {
                var infoLabel = new GameObject("Info", typeof(RectTransform));
                infoLabel.transform.SetParent(card.transform, false);
                var infoRT = infoLabel.GetComponent<RectTransform>();
                infoRT.anchorMin = new Vector2(0, 0);
                infoRT.anchorMax = new Vector2(0.6f, 0.35f);
                infoRT.offsetMin = new Vector2(16, 8);
                infoRT.offsetMax = new Vector2(0, 0);
                var infoTmp = infoLabel.AddComponent<TextMeshProUGUI>();
                infoTmp.text = $"{item.Rarity}";
                infoTmp.fontSize = 13;
                infoTmp.color = ItemDisplayData.GetRarityColor(item.Rarity);
                infoTmp.alignment = TextAlignmentOptions.TopLeft;
            }

            // Upgrade pip (green dot if better item in inventory)
            if (inventory != null && item != null)
            {
                var candidates = inventory.GetItemsForSlot((EquipmentSlot)slotIndex);
                var best = ItemComparer.FindBestForSlot((EquipmentSlot)slotIndex, candidates, item);
                if (best != null)
                {
                    var pip = new GameObject("UpgradePip", typeof(RectTransform), typeof(Image));
                    pip.transform.SetParent(card.transform, false);
                    var pipRT = pip.GetComponent<RectTransform>();
                    pipRT.anchorMin = new Vector2(0.55f, 0.08f);
                    pipRT.anchorMax = new Vector2(0.55f, 0.08f);
                    pipRT.pivot = new Vector2(0.5f, 0.5f);
                    pipRT.sizeDelta = new Vector2(12, 12);
                    pip.GetComponent<Image>().color = new Color(0.2f, 0.9f, 0.3f);
                }
            }

            // === Equipment sprite (right 30%) ===
            if (item != null && item.LayerCodes != null && item.LayerCodes.Length > 0)
            {
                var spriteObj = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
                spriteObj.transform.SetParent(card.transform, false);
                var spriteRT = spriteObj.GetComponent<RectTransform>();
                spriteRT.anchorMin = new Vector2(0.65f, 0.05f);
                spriteRT.anchorMax = new Vector2(0.95f, 0.95f);
                spriteRT.offsetMin = Vector2.zero;
                spriteRT.offsetMax = Vector2.zero;

                var spriteImg = spriteObj.GetComponent<Image>();
                spriteImg.preserveAspect = true;

                // Load the primary layer sprite
                // Weapons (slot 5 or 6) use different path format
                string spritePath;
                if (slotIndex == 5 || slotIndex == 6)
                    spritePath = ImageToken.BuildWeaponSpritePath(item.ItemType, item.LayerCodes[0], item.ItemNum);
                else
                    spritePath = ImageToken.BuildEquipmentSpritePath(item.ItemType, item.ItemNum, item.LayerCodes[0]);

                var sprite = Resources.Load<Sprite>(spritePath);
                if (sprite != null)
                {
                    spriteImg.sprite = sprite;
                    spriteImg.color = item.BaseColor;
                }
                else
                {
                    // Fallback: colored placeholder
                    spriteImg.color = new Color(item.BaseColor.r, item.BaseColor.g, item.BaseColor.b, 0.3f);
                }
            }

            // Button handler
            var btn = card.GetComponent<Button>();
            int idx = slotIndex;
            btn.onClick.AddListener(() =>
            {
                selectedSlotIndex = idx;
                OnSlotTapped?.Invoke(idx);
            });

            return card;
        }
    }
}
