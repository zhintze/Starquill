using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Display;
using Starquill.Equipment;

namespace Starquill.UI
{
    /// The app-wide item card (mid-density, UiTheme.CardHeight).
    /// Anatomy: rarity-framed sprite | name / stat pair / ability / meta | delta chip.
    /// Used by the loot list, drawer candidate list, and party slot list.
    public static class ItemCardBuilder
    {
        public struct CardOptions
        {
            public EquipmentInstance CompareAgainst;
            public bool ShowBestTag;
            public bool ShowSprite;
            public bool HideDelta;
            public int SlotIndexForSprite;
            public Action OnTapped;
        }

        public static GameObject Build(ItemDisplayData data, EquipmentInstance item,
            CardOptions options, Transform parent)
        {
            var card = new GameObject($"Item_{data.DisplayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            if (parent != null) card.transform.SetParent(parent, false);

            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, UiTheme.CardHeight);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.CardHeight;
            le.minHeight = UiTheme.CardHeight;
            le.flexibleWidth = 1;
            card.GetComponent<Image>().color = UiTheme.Card;

            float pad = UiTheme.CardPadding;

            // Left: rarity-framed sprite
            if (options.ShowSprite)
            {
                var frameImg = UiFactory.Frame(card.transform, data.RarityColor);
                var frameRT = frameImg.transform.parent.GetComponent<RectTransform>();
                frameRT.anchorMin = new Vector2(0, 0.5f);
                frameRT.anchorMax = new Vector2(0, 0.5f);
                frameRT.pivot = new Vector2(0, 0.5f);
                frameRT.anchoredPosition = new Vector2(pad, 0);
                LoadEquipmentSprite(frameImg, item, options.SlotIndexForSprite);
            }

            // Middle column bounds (pixels from left / right)
            float textLeft = options.ShowSprite ? pad + UiTheme.CardIcon + pad : pad;
            float textRight = UiTheme.DeltaChipWidth + 2f * pad;

            // Line 1: name (rarity color) + optional BEST tag
            var nameTmp = MidLine(card, "Name", textLeft, textRight, 0.72f, 1f);
            nameTmp.text = data.DisplayName;
            UiFactory.ApplyStyle(nameTmp, UiFactory.TextStyle.Heading);
            nameTmp.color = data.RarityColor;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.alignment = TextAlignmentOptions.BottomLeft;

            // Line 2: stat pair (primary stat-colored, secondary softer)
            var pairTmp = MidLine(card, "StatPair", textLeft, textRight, 0.46f, 0.72f);
            var cmp = ComparisonData.Build(item, null);
            if (cmp.IsLegacyItem)
            {
                pairTmp.text = "Legacy item";
                UiFactory.ApplyStyle(pairTmp, UiFactory.TextStyle.BodySecondary);
            }
            else
            {
                pairTmp.text = ItemDisplayData.StatLabelColored(data.PrimaryStat, data.PrimaryValue)
                    + "    " + UiFactory.ColorTag(
                        ItemDisplayData.StatLabel(data.SecondaryStat, data.SecondaryValue),
                        UiTheme.TextSecondary);
                UiFactory.ApplyStyle(pairTmp, UiFactory.TextStyle.Body);
            }

            // Line 3: ability (text left + Image pips right of text block); omitted when absent
            if (data.HasAbility)
            {
                var abilityTmp = MidLine(card, "Ability", textLeft, textRight, 0.24f, 0.46f);
                abilityTmp.text = data.AbilityName + "  " + UiFactory.ColorTag(
                    $"{data.AbilityStat} +{data.AbilityPotency:0.#}", UiTheme.TextDim);
                UiFactory.ApplyStyle(abilityTmp, UiFactory.TextStyle.Caption);
                abilityTmp.textWrappingMode = TextWrappingModes.NoWrap;
                abilityTmp.overflowMode = TextOverflowModes.Ellipsis;

                var pips = UiFactory.PipRow(card.transform, data.AbilityLevel, data.AbilityMaxLevel);
                var pipsRT = pips.GetComponent<RectTransform>();
                pipsRT.anchorMin = new Vector2(1, 0.24f);
                pipsRT.anchorMax = new Vector2(1, 0.46f);
                pipsRT.pivot = new Vector2(1, 0.5f);
                pipsRT.anchoredPosition = new Vector2(-textRight, 0);
                pipsRT.sizeDelta = new Vector2(data.AbilityMaxLevel * 28f, 0);
            }

            // Line 4: meta (slot · rarity · sell)
            var metaTmp = MidLine(card, "Meta", textLeft, textRight, 0f, 0.24f);
            metaTmp.text = $"{data.SlotName} · {data.RarityName} · {NumberFormatter.FormatCompact(data.SellValue)}g";
            UiFactory.ApplyStyle(metaTmp, UiFactory.TextStyle.CaptionDim);
            metaTmp.alignment = TextAlignmentOptions.TopLeft;

            // Right: delta chip (+ BEST caption above when flagged)
            if (item != null && !options.HideDelta)
            {
                var delta = ItemComparer.Compare(item, options.CompareAgainst);
                Color chipColor = delta.IsUpgrade ? UiTheme.DeltaUp
                    : (delta.TotalDelta < 0 ? UiTheme.DeltaDown : UiTheme.DeltaNeutral);
                string sign = delta.TotalDelta > 0 ? "+" : "";
                var chip = UiFactory.Chip(card.transform, $"{sign}{delta.TotalDelta:F0}", chipColor);
                var chipRT = chip.GetComponent<RectTransform>();
                chipRT.anchorMin = new Vector2(1, 0.5f);
                chipRT.anchorMax = new Vector2(1, 0.5f);
                chipRT.pivot = new Vector2(1, 0.5f);
                chipRT.anchoredPosition = new Vector2(-pad, 0);

                if (options.ShowBestTag)
                {
                    var bestTmp = UiFactory.Text(card.transform, "BEST",
                        UiFactory.TextStyle.Caption, TextAlignmentOptions.Center);
                    bestTmp.color = UiTheme.BestGold;
                    bestTmp.fontStyle = FontStyles.Bold;
                    var bestRT = bestTmp.rectTransform;
                    bestRT.anchorMin = new Vector2(1, 0.5f);
                    bestRT.anchorMax = new Vector2(1, 0.5f);
                    bestRT.pivot = new Vector2(1, 0f);
                    bestRT.anchoredPosition = new Vector2(-pad, UiTheme.DeltaChipHeight * 0.5f + UiTheme.Space1);
                    bestRT.sizeDelta = new Vector2(UiTheme.DeltaChipWidth, UiTheme.FontCaption + 6f);
                }
            }

            if (options.OnTapped != null)
            {
                var tapped = options.OnTapped;
                card.GetComponent<Button>().onClick.AddListener(() => tapped());
            }

            return card;
        }

        private static TMP_Text MidLine(GameObject card, string name,
            float leftPx, float rightPx, float yMin, float yMax)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(card.transform, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, yMin);
            rt.anchorMax = new Vector2(1, yMax);
            rt.offsetMin = new Vector2(leftPx, 0);
            rt.offsetMax = new Vector2(-rightPx, 0);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.richText = true;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        public static void LoadEquipmentSprite(Image target, EquipmentInstance item, int slotIndex)
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
    }
}
