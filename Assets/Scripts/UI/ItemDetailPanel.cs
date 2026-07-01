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
        private int currentQuestLevel;

        // Ability block (built once, reused)
        private GameObject abilityBlock;
        private TMP_Text abilityTitleLabel;
        private TMP_Text abilityDescLabel;
        private RectTransform xpBarFill;
        private GameObject xpBarRoot;
        private TMP_Text xpLabel;
        private Button levelUpButton;
        private TMP_Text levelUpLabel;

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
            currentQuestLevel = questLevel;
            if (panel != null) panel.SetActive(true);

            var gm = GameManager.Instance;
            var data = ItemDisplayData.FromItem(item, questLevel,
                gm != null ? gm.AbilityTable : null,
                gm != null ? gm.economyConfig : null);

            if (itemNameLabel != null)
                itemNameLabel.text = $"<color=#{ColorUtility.ToHtmlStringRGB(data.RarityColor)}>{data.DisplayName}</color>";

            if (itemStatsLabel != null)
            {
                string header = $"<color=#{ColorUtility.ToHtmlStringRGB(data.RarityColor)}>{data.RarityName}</color>"
                    + $" · {data.SlotName} · <size=13>Score {data.Score:F0}</size>";
                string pair = $"<size=26>{ItemDisplayData.StatLabelColored(data.PrimaryStat, data.PrimaryValue)}</size>"
                    + $"   <size=18><alpha=#B4>{ItemDisplayData.StatLabelColored(data.SecondaryStat, data.SecondaryValue)}</alpha></size>";
                itemStatsLabel.text = $"{header}\n{pair}";
            }

            RefreshAbilityBlock(data);

            if (sellValueLabel != null)
                sellValueLabel.text = $"Sell: {NumberFormatter.FormatCompact(data.SellValue)} Gold";

            RefreshCharacterPicker(item);
        }

        private void RefreshAbilityBlock(ItemDisplayData data)
        {
            if (abilityBlock == null)
                BuildAbilityBlock();

            if (!data.HasAbility)
            {
                abilityBlock.SetActive(false);
                return;
            }

            abilityBlock.SetActive(true);

            string pips = ItemCardBuilder.BuildPips(data.AbilityLevel, data.AbilityMaxLevel);
            abilityTitleLabel.text =
                $"<color=#FFD700>✦</color> {data.AbilityName}  <size=13>{pips}  Lv {data.AbilityLevel}/{data.AbilityMaxLevel}</size>";
            abilityDescLabel.text = string.IsNullOrEmpty(data.AbilityDescription)
                ? $"{data.AbilityStat} +{data.AbilityPotency:0.#} while equipped"
                : data.AbilityDescription;

            bool maxed = data.AbilityLevel >= data.AbilityMaxLevel;
            if (maxed)
            {
                xpBarRoot.SetActive(false);
                xpLabel.text = "<color=#FFD700>MAX LEVEL</color>";
                levelUpButton.gameObject.SetActive(false);
            }
            else
            {
                xpBarRoot.SetActive(true);
                xpBarFill.anchorMax = new Vector2(Mathf.Clamp01(data.AbilityXpFraction), 1f);
                xpLabel.text = $"{data.AbilityXpFraction * 100f:F0}% to next level";
                levelUpButton.gameObject.SetActive(true);

                var gm = GameManager.Instance;
                float cost = gm != null ? gm.GetAbilityLevelUpCost(currentItem) : float.MaxValue;
                bool affordable = gm != null && gm.gold >= cost && cost < float.MaxValue;
                levelUpLabel.text = cost < float.MaxValue
                    ? $"Level Up  ({NumberFormatter.FormatCompact(cost)} g)"
                    : "Level Up";
                levelUpButton.interactable = affordable;
            }
        }

        private void BuildAbilityBlock()
        {
            abilityBlock = new GameObject("AbilityBlock", typeof(RectTransform));
            abilityBlock.transform.SetParent(panel != null ? panel.transform : transform, false);
            if (itemStatsLabel != null)
                abilityBlock.transform.SetSiblingIndex(itemStatsLabel.transform.GetSiblingIndex() + 1);
            var le = abilityBlock.AddComponent<LayoutElement>();
            le.preferredHeight = 150;

            // Title
            abilityTitleLabel = MakeLabel("Title", new Vector2(0, 0.72f), new Vector2(1, 1f), 17, FontStyles.Bold);
            // Description
            abilityDescLabel = MakeLabel("Desc", new Vector2(0, 0.50f), new Vector2(1, 0.72f), 14);
            abilityDescLabel.color = new Color(0.75f, 0.75f, 0.8f);

            // XP bar: background + fill
            xpBarRoot = new GameObject("XpBar", typeof(RectTransform), typeof(Image));
            xpBarRoot.transform.SetParent(abilityBlock.transform, false);
            var barRT = xpBarRoot.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0, 0.34f);
            barRT.anchorMax = new Vector2(0.6f, 0.46f);
            barRT.offsetMin = Vector2.zero;
            barRT.offsetMax = Vector2.zero;
            xpBarRoot.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(xpBarRoot.transform, false);
            xpBarFill = fillObj.GetComponent<RectTransform>();
            xpBarFill.anchorMin = Vector2.zero;
            xpBarFill.anchorMax = new Vector2(0f, 1f);
            xpBarFill.offsetMin = Vector2.zero;
            xpBarFill.offsetMax = Vector2.zero;
            fillObj.GetComponent<Image>().color = new Color(0.35f, 0.7f, 1f);

            xpLabel = MakeLabel("XpLabel", new Vector2(0.62f, 0.30f), new Vector2(1, 0.50f), 12);
            xpLabel.color = new Color(0.6f, 0.6f, 0.65f);

            // Level up button
            var btnObj = new GameObject("LevelUpButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(abilityBlock.transform, false);
            var btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 0);
            btnRT.anchorMax = new Vector2(0.5f, 0.28f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;
            btnObj.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f);
            levelUpButton = btnObj.GetComponent<Button>();
            levelUpButton.onClick.AddListener(HandleLevelUp);

            var btnLabelObj = new GameObject("Label", typeof(RectTransform));
            btnLabelObj.transform.SetParent(btnObj.transform, false);
            var btnLabelRT = btnLabelObj.GetComponent<RectTransform>();
            btnLabelRT.anchorMin = Vector2.zero;
            btnLabelRT.anchorMax = Vector2.one;
            btnLabelRT.offsetMin = Vector2.zero;
            btnLabelRT.offsetMax = Vector2.zero;
            levelUpLabel = btnLabelObj.AddComponent<TextMeshProUGUI>();
            levelUpLabel.fontSize = 15;
            levelUpLabel.alignment = TextAlignmentOptions.Center;
            levelUpLabel.color = Color.white;
        }

        private TMP_Text MakeLabel(string name, Vector2 anchorMin, Vector2 anchorMax,
            float size, FontStyles style = FontStyles.Normal)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(abilityBlock.transform, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.richText = true;
            return tmp;
        }

        private void HandleLevelUp()
        {
            var gm = GameManager.Instance;
            if (gm == null || currentItem == null) return;
            if (gm.LevelUpAbility(currentItem))
                Show(currentItem, currentQuestLevel);
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
