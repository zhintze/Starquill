using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Display;
using Starquill.Equipment;

namespace Starquill.UI
{
    /// Shared code-built card for any item list (loot screen, equipment drawer).
    /// Layout: rarity stripe | name + slot | sprite | stat pair | ability row | score/sell/delta footer.
    public static class ItemCardBuilder
    {
        public struct CardOptions
        {
            public EquipmentInstance CompareAgainst;
            public bool ShowBestTag;
            public bool ShowSprite;
            public int SlotIndexForSprite;
            public float Height;
            public Action OnTapped;
        }

        private static readonly Color CardBackground = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color DimText = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color BestGold = new Color(1f, 0.84f, 0f);
        private static readonly Color UpgradeGreen = new Color(0.3f, 0.9f, 0.3f);
        private static readonly Color DowngradeRed = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color NeutralGray = new Color(0.6f, 0.6f, 0.6f);

        public static GameObject Build(ItemDisplayData data, EquipmentInstance item,
            CardOptions options, Transform parent)
        {
            float height = options.Height > 0f ? options.Height : 280f;

            var card = new GameObject($"Item_{data.DisplayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            if (parent != null) card.transform.SetParent(parent, false);

            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;
            card.GetComponent<Image>().color = CardBackground;

            // Rarity stripe (gold when best)
            var stripe = MakeChild(card, "Stripe", new Vector2(0, 0), new Vector2(0, 1));
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(6, 0);
            stripeRT.anchoredPosition = Vector2.zero;
            stripe.AddComponent<Image>().color = options.ShowBestTag ? BestGold : data.RarityColor;

            float textLeft = options.ShowSprite ? 0.30f : 0.05f;

            // Sprite column
            if (options.ShowSprite)
            {
                var spriteObj = MakeChild(card, "Sprite", new Vector2(0.02f, 0.10f), new Vector2(0.28f, 0.90f));
                var spriteImg = spriteObj.AddComponent<Image>();
                spriteImg.preserveAspect = true;
                LoadEquipmentSprite(spriteImg, item, options.SlotIndexForSprite);
            }

            // Row 1: name (rarity color) + slot top-right
            var nameObj = MakeChild(card, "Name", new Vector2(textLeft, 0.72f), new Vector2(0.78f, 1f), 12, -6);
            var nameTmp = AddText(nameObj, data.DisplayName, 18, data.RarityColor,
                TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;

            var slotObj = MakeChild(card, "Slot", new Vector2(0.78f, 0.72f), new Vector2(1f, 1f), 0, -6, -12);
            string slotText = options.ShowBestTag ? $"<color=#{Hex(BestGold)}>★ Best</color> {data.SlotName}" : data.SlotName;
            AddText(slotObj, slotText, 13, DimText, TextAlignmentOptions.BottomRight);

            // Row 2: stat pair - primary large, secondary smaller/dimmer
            var pairObj = MakeChild(card, "StatPair", new Vector2(textLeft, 0.44f), new Vector2(1f, 0.72f), 12, 0, -12);
            string pair = $"<size=24>{ItemDisplayData.StatLabelColored(data.PrimaryStat, data.PrimaryValue)}</size>"
                + $"    <size=16><alpha=#B4>{ItemDisplayData.StatLabelColored(data.SecondaryStat, data.SecondaryValue)}</alpha></size>";
            AddText(pairObj, pair, 24, Color.white, TextAlignmentOptions.MidlineLeft);

            // Row 3: ability (omitted when absent)
            var abilityObj = MakeChild(card, "Ability", new Vector2(textLeft, 0.22f), new Vector2(1f, 0.44f), 12, 0, -12);
            if (data.HasAbility)
            {
                string pips = BuildPips(data.AbilityLevel, data.AbilityMaxLevel);
                string potency = $"{data.AbilityStat} +{data.AbilityPotency:0.#}";
                AddText(abilityObj,
                    $"<color=#{Hex(BestGold)}>✦</color> {data.AbilityName}  <size=12>{pips}</size>  <color=#{Hex(DimText)}>{potency}</color>",
                    15, new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.MidlineLeft);
            }

            // Footer: score, sell, delta badge
            var footerObj = MakeChild(card, "Footer", new Vector2(textLeft, 0f), new Vector2(0.72f, 0.22f), 12, 6);
            AddText(footerObj,
                $"<color=#{Hex(DimText)}>⚖ {data.Score:F0}   💰 {NumberFormatter.FormatCompact(data.SellValue)}g</color>",
                13, DimText, TextAlignmentOptions.MidlineLeft);

            if (options.CompareAgainst != null || item != null)
            {
                var delta = ItemComparer.Compare(item, options.CompareAgainst);
                if (options.CompareAgainst != null || delta.TotalDelta != 0f)
                {
                    var badgeObj = MakeChild(card, "Delta", new Vector2(0.72f, 0f), new Vector2(1f, 0.22f), 0, 6, -12);
                    string arrow = delta.IsUpgrade ? "▲" : (delta.TotalDelta < 0 ? "▼" : "–");
                    Color deltaColor = delta.IsUpgrade ? UpgradeGreen
                        : (delta.TotalDelta < 0 ? DowngradeRed : NeutralGray);
                    string sign = delta.TotalDelta >= 0 ? "+" : "";
                    AddText(badgeObj, $"{arrow} {sign}{delta.TotalDelta:F0}", 18, deltaColor,
                        TextAlignmentOptions.MidlineRight, FontStyles.Bold);
                }
            }

            if (options.OnTapped != null)
            {
                var tapped = options.OnTapped;
                card.GetComponent<Button>().onClick.AddListener(() => tapped());
            }

            return card;
        }

        public static string BuildPips(int level, int maxLevel)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < maxLevel; i++)
                sb.Append(i < level ? "●" : "○");
            return sb.ToString();
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

        private static GameObject MakeChild(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent.transform, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(right, top);
            return obj;
        }

        private static TextMeshProUGUI AddText(GameObject obj, string text, float size,
            Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.richText = true;
            return tmp;
        }

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
