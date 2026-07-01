using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Party Equipment tab: 11 slot cards (shared ItemCard) grouped by category.
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

            var gm = GameManager.Instance;
            int questLevel = gm != null ? gm.questLevel : 1;

            for (int cat = 0; cat < CategorySlots.Length; cat++)
            {
                CreateCategoryHeader(CategoryNames[cat]);

                foreach (int slotIndex in CategorySlots[cat])
                {
                    EquipmentInstance item = slotIndex < character.equipment.Length
                        ? character.equipment[slotIndex]
                        : null;

                    if (item == null)
                    {
                        CreateEmptySlotCard(slotIndex);
                        continue;
                    }

                    var data = ItemDisplayData.FromItem(item, questLevel,
                        gm != null ? gm.AbilityTable : null,
                        gm != null ? gm.economyConfig : null);

                    int idx = slotIndex;
                    var card = ItemCardBuilder.Build(data, item, new ItemCardBuilder.CardOptions
                    {
                        ShowSprite = true,
                        HideDelta = true,
                        SlotIndexForSprite = slotIndex,
                        OnTapped = () =>
                        {
                            selectedSlotIndex = idx;
                            OnSlotTapped?.Invoke(idx);
                        }
                    }, cardContainer);

                    if (slotIndex == selectedSlotIndex)
                        card.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.27f);

                    if (HasUpgrade(slotIndex, item))
                    {
                        var tag = UiFactory.Text(card.transform, "UPGRADE",
                            UiFactory.TextStyle.Caption, TextAlignmentOptions.MidlineRight);
                        tag.color = UiTheme.DeltaUp;
                        tag.fontStyle = FontStyles.Bold;
                        var tagRT = tag.rectTransform;
                        tagRT.anchorMin = new Vector2(1, 0.5f);
                        tagRT.anchorMax = new Vector2(1, 0.5f);
                        tagRT.pivot = new Vector2(1, 0.5f);
                        tagRT.anchoredPosition = new Vector2(-UiTheme.CardPadding, 0);
                        tagRT.sizeDelta = new Vector2(220, UiTheme.FontCaption + 8f);
                    }
                }
            }
        }

        private bool HasUpgrade(int slotIndex, EquipmentInstance item)
        {
            if (inventory == null) return false;
            var candidates = inventory.GetItemsForSlot((EquipmentSlot)slotIndex);
            return ItemComparer.FindBestForSlot((EquipmentSlot)slotIndex, candidates, item) != null;
        }

        private void CreateCategoryHeader(string categoryName)
        {
            var header = new GameObject($"Header_{categoryName}", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(cardContainer, false);
            var le = header.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.FontCaption + 2f * UiTheme.Space2;
            le.flexibleWidth = 1;
            header.GetComponent<Image>().color = UiTheme.Background;

            var tmp = UiFactory.Text(header.transform, categoryName, UiFactory.TextStyle.CaptionDim);
            tmp.fontStyle = FontStyles.Bold;
            var rt = tmp.rectTransform;
            UiFactory.StretchFill(rt);
            rt.offsetMin = new Vector2(UiTheme.Space3, 0);
        }

        private void CreateEmptySlotCard(int slotIndex)
        {
            var card = new GameObject($"Empty_{slotIndex}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(cardContainer, false);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.TouchMin;
            le.minHeight = UiTheme.TouchMin;
            le.flexibleWidth = 1;
            card.GetComponent<Image>().color = slotIndex == selectedSlotIndex
                ? new Color(0.2f, 0.2f, 0.27f)
                : UiTheme.Card;

            var tmp = UiFactory.Text(card.transform,
                $"{SlotLabels[slotIndex]} · Empty · tap to browse",
                UiFactory.TextStyle.CaptionDim);
            tmp.fontStyle = FontStyles.Italic;
            var rt = tmp.rectTransform;
            UiFactory.StretchFill(rt);
            rt.offsetMin = new Vector2(UiTheme.Space3, 0);

            int idx = slotIndex;
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedSlotIndex = idx;
                OnSlotTapped?.Invoke(idx);
            });
        }
    }
}
