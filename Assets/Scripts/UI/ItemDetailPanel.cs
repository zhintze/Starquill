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
    public class ItemDetailPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text itemNameLabel;
        [SerializeField] private TMP_Text itemStatsLabel;
        [SerializeField] private TMP_Text sellValueLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform characterPickerContainer;

        private EquipmentInstance currentItem;

        public event Action OnSold;
        public event Action OnEquipped;
        public event Action OnClosed;

        public void Initialize()
        {
            if (sellButton != null) sellButton.onClick.AddListener(HandleSell);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Show(EquipmentInstance item, int questLevel)
        {
            currentItem = item;
            if (panel != null) panel.SetActive(true);

            var data = ItemDisplayData.FromItem(item, questLevel);

            if (itemNameLabel != null)
                itemNameLabel.text = $"<color=#{ColorUtility.ToHtmlStringRGB(data.RarityColor)}>{data.DisplayName}</color>";

            if (itemStatsLabel != null)
            {
                string stats = $"{data.RarityName} {data.SlotName}\nScore: {data.Score:F0}\n{data.StatSummary}";
                if (item.RolledAffixes.Count > 0)
                {
                    stats += "\n\nAffixes:";
                    foreach (var affix in item.RolledAffixes)
                    {
                        string pct = affix.IsPercentage ? "%" : "";
                        stats += $"\n  {affix.StatType} +{affix.Value:F0}{pct}";
                    }
                }
                itemStatsLabel.text = stats;
            }

            if (sellValueLabel != null)
                sellValueLabel.text = $"Sell: {data.SellValue:F0} Gold";

            RefreshCharacterPicker(item);
        }

        public void Close()
        {
            currentItem = null;
            if (panel != null) panel.SetActive(false);
            OnClosed?.Invoke();
        }

        private void RefreshCharacterPicker(EquipmentInstance item)
        {
            if (characterPickerContainer == null) return;

            for (int i = characterPickerContainer.childCount - 1; i >= 0; i--)
                Destroy(characterPickerContainer.GetChild(i).gameObject);

            var gm = GameManager.Instance;
            if (gm == null || gm.Roster == null) return;

            // Find best match
            int bestIdx = -1;
            float bestDelta = float.MinValue;
            for (int i = 0; i < gm.Roster.Characters.Count; i++)
            {
                var c = gm.Roster.Characters[i];
                var equipped = c.equipment[(int)item.Slot];
                var diff = ItemComparer.Compare(item, equipped);
                if (diff.TotalDelta > bestDelta)
                {
                    bestDelta = diff.TotalDelta;
                    bestIdx = i;
                }
            }

            // Create character cards
            for (int i = 0; i < gm.Roster.Characters.Count; i++)
            {
                var character = gm.Roster.Characters[i];
                var equipped = character.equipment[(int)item.Slot];
                var diff = ItemComparer.Compare(item, equipped);
                bool isBestMatch = i == bestIdx && bestDelta > 0;

                var card = CreateCharacterPickerCard(character, i, diff, isBestMatch);
                card.transform.SetParent(characterPickerContainer, false);
            }
        }

        private GameObject CreateCharacterPickerCard(CharacterInstance character, int rosterIndex,
            StatDiff diff, bool isBestMatch)
        {
            var card = new GameObject($"Char_{character.displayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));

            var img = card.GetComponent<Image>();
            img.color = isBestMatch
                ? new Color(0.2f, 0.4f, 0.2f)
                : new Color(0.15f, 0.15f, 0.2f);

            // Name + best match badge
            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.45f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(4, 0);
            nameRT.offsetMax = new Vector2(-4, -2);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            string bestTag = isBestMatch ? "\n<color=#FFD700>\u2605 BEST</color>" : "";
            nameTmp.text = $"{character.displayName}{bestTag}";
            nameTmp.fontSize = 11;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            nameTmp.richText = true;

            // Delta badge
            var deltaObj = new GameObject("Delta", typeof(RectTransform));
            deltaObj.transform.SetParent(card.transform, false);
            var deltaRT = deltaObj.GetComponent<RectTransform>();
            deltaRT.anchorMin = new Vector2(0, 0);
            deltaRT.anchorMax = new Vector2(1, 0.45f);
            deltaRT.offsetMin = new Vector2(4, 2);
            deltaRT.offsetMax = new Vector2(-4, 0);
            var deltaTmp = deltaObj.AddComponent<TextMeshProUGUI>();
            string sign = diff.TotalDelta >= 0 ? "+" : "";
            Color deltaColor = diff.IsUpgrade ? new Color(0.3f, 0.9f, 0.3f)
                : (diff.TotalDelta < 0 ? new Color(0.9f, 0.3f, 0.3f)
                : new Color(0.6f, 0.6f, 0.6f));
            deltaTmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(deltaColor)}>{sign}{diff.TotalDelta:F0}</color>";
            deltaTmp.fontSize = 14;
            deltaTmp.alignment = TextAlignmentOptions.Center;
            deltaTmp.richText = true;

            // Tap to equip
            var btn = card.GetComponent<Button>();
            int idx = rosterIndex;
            var capturedItem = currentItem;
            btn.onClick.AddListener(() =>
            {
                var gm = GameManager.Instance;
                if (gm != null && capturedItem != null)
                {
                    gm.EquipItemFromInventory(capturedItem, idx);
                    Close();
                    OnEquipped?.Invoke();
                }
            });

            return card;
        }

        private void HandleSell()
        {
            if (currentItem == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.SellItem(currentItem);
            Close();
            OnSold?.Invoke();
        }

        private void OnDestroy()
        {
            if (sellButton != null) sellButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }
    }
}
