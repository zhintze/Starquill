using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Fills a BottomSheet with item detail: header, equipped-vs-selected comparison,
    /// ability block (XP bar + gold level-up), target character row, EQUIP/Sell actions.
    public static class ItemDetailSheet
    {
        /// onChanged fires after any state mutation (equip, sell, level-up) so callers refresh.
        public static BottomSheet Show(Transform canvasRoot, EquipmentInstance item,
            int questLevel, Action onChanged, int? fixedTargetIndex = null)
        {
            var gm = GameManager.Instance;
            int targetIndex = fixedTargetIndex ?? FindBestTargetIndex(gm, item);

            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot, content =>
                Fill(content, item, questLevel, targetIndex, fixedTargetIndex.HasValue,
                    onChanged, () => sheet));
            return sheet;
        }

        private static void Fill(Transform content, EquipmentInstance item, int questLevel,
            int targetIndex, bool targetFixed, Action onChanged, Func<BottomSheet> getSheet)
        {
            var gm = GameManager.Instance;
            var data = ItemDisplayData.FromItem(item, questLevel,
                gm != null ? gm.AbilityTable : null,
                gm != null ? gm.economyConfig : null);

            // --- Header ---
            var name = UiFactory.Text(content, data.DisplayName, UiFactory.TextStyle.Heading);
            name.color = data.RarityColor;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 12f;

            var sub = UiFactory.Text(content,
                $"{data.RarityName} · {data.SlotName}", UiFactory.TextStyle.CaptionDim);
            sub.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;

            // --- Comparison table vs target's equipped item ---
            var comparisonHost = new GameObject("Comparison", typeof(RectTransform));
            comparisonHost.transform.SetParent(content, false);
            var comparisonLayout = comparisonHost.AddComponent<VerticalLayoutGroup>();
            comparisonLayout.spacing = UiTheme.Space1;
            comparisonLayout.childControlWidth = true;
            comparisonLayout.childControlHeight = false;
            comparisonLayout.childForceExpandWidth = true;
            var comparisonFitter = comparisonHost.AddComponent<ContentSizeFitter>();
            comparisonFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            void RebuildComparison(int idx)
            {
                for (int i = comparisonHost.transform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.Destroy(comparisonHost.transform.GetChild(i).gameObject);

                var equipped = GetEquipped(gm, idx, item);
                var cmp = ComparisonData.Build(item, equipped);

                if (cmp.IsLegacyItem)
                {
                    var legacy = UiFactory.Text(comparisonHost.transform,
                        "Legacy item: no stat data (pre-update)", UiFactory.TextStyle.CaptionDim);
                    legacy.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;
                    return;
                }

                var headerRow = UiFactory.Text(comparisonHost.transform,
                    UiFactory.ColorTag("EQUIPPED", UiTheme.TextDim) + "  >  "
                    + UiFactory.ColorTag("SELECTED", UiTheme.TextSecondary),
                    UiFactory.TextStyle.Caption);
                headerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 6f;

                foreach (var row in cmp.Rows)
                {
                    Color deltaColor = row.Delta > 0 ? UiTheme.DeltaUp
                        : (row.Delta < 0 ? UiTheme.DeltaDown : UiTheme.DeltaNeutral);
                    string sign = row.Delta > 0 ? "+" : "";
                    string line = UiFactory.ColorTag(row.Stat.ToString(),
                            StatTypeColors.GetColor(row.Stat))
                        + $"   {row.EquippedValue}  >  {row.SelectedValue}   "
                        + UiFactory.ColorTag($"({sign}{row.Delta})", deltaColor);
                    var rowTmp = UiFactory.Text(comparisonHost.transform, line, UiFactory.TextStyle.Body);
                    rowTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 8f;
                }
            }

            // --- Ability block ---
            if (data.HasAbility)
            {
                var abilityTitle = UiFactory.Text(content, "", UiFactory.TextStyle.Body);
                abilityTitle.fontStyle = FontStyles.Bold;
                abilityTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 8f;

                var abilityRow = new GameObject("AbilityPips", typeof(RectTransform));
                abilityRow.transform.SetParent(content, false);
                abilityRow.AddComponent<LayoutElement>().preferredHeight = 28f;
                var pips = UiFactory.PipRow(abilityRow.transform, data.AbilityLevel, data.AbilityMaxLevel, 24f);
                var pipsRT = pips.GetComponent<RectTransform>();
                pipsRT.anchorMin = new Vector2(0, 0);
                pipsRT.anchorMax = new Vector2(0, 1);
                pipsRT.pivot = new Vector2(0, 0.5f);

                var desc = UiFactory.Text(content,
                    string.IsNullOrEmpty(data.AbilityDescription)
                        ? $"{data.AbilityStat} +{data.AbilityPotency:0.#} while equipped"
                        : data.AbilityDescription,
                    UiFactory.TextStyle.Caption);
                desc.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;

                bool maxed = data.AbilityLevel >= data.AbilityMaxLevel;
                if (maxed)
                {
                    var maxTmp = UiFactory.Text(content, "MAX LEVEL", UiFactory.TextStyle.Caption);
                    maxTmp.color = UiTheme.BestGold;
                    maxTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;
                    abilityTitle.text = $"{data.AbilityName}  Lv {data.AbilityLevel}/{data.AbilityMaxLevel}";
                }
                else
                {
                    abilityTitle.text = $"{data.AbilityName}  Lv {data.AbilityLevel}/{data.AbilityMaxLevel}"
                        + UiFactory.ColorTag($"   {data.AbilityXpFraction * 100f:F0}% to next", UiTheme.TextDim);

                    var xpBar = UiFactory.ProgressBar(content, new Color(0.35f, 0.7f, 1f));
                    xpBar.SetFraction(data.AbilityXpFraction);

                    float cost = gm != null ? gm.GetAbilityLevelUpCost(item) : float.MaxValue;
                    bool affordable = gm != null && cost < float.MaxValue && gm.gold >= cost;
                    string costLabel = cost < float.MaxValue
                        ? $"Level Up ({NumberFormatter.FormatCompact(cost)}g)" : "Level Up";

                    var levelUp = UiFactory.Button(content, costLabel, UiFactory.ButtonKind.Secondary, () =>
                    {
                        if (gm != null && gm.LevelUpAbility(item))
                        {
                            onChanged?.Invoke();
                            getSheet()?.Close();
                            ItemDetailSheet.Show(getSheet()?.transform.parent ?? content.root,
                                item, questLevel, onChanged);
                        }
                    });
                    levelUp.SetEnabled(affordable,
                        cost < float.MaxValue ? $"Need {NumberFormatter.FormatCompact(cost)}g" : null);
                }
            }

            // --- Target character row ---
            int currentTarget = targetIndex;
            if (gm != null && gm.Roster != null && gm.Roster.Characters.Count > 0 && !targetFixed)
            {
                var targetLabel = UiFactory.Text(content, "Equip on", UiFactory.TextStyle.CaptionDim);
                targetLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 6f;

                var names = new string[gm.Roster.Characters.Count];
                for (int i = 0; i < names.Length; i++)
                    names[i] = gm.Roster.Characters[i].displayName;

                UiFactory.SegmentedTabs(content, names, idx =>
                {
                    currentTarget = idx;
                    RebuildComparison(idx);
                }, initialIndex: Mathf.Clamp(targetIndex, 0, names.Length - 1));
            }
            else
            {
                RebuildComparison(currentTarget);
            }

            // --- Actions ---
            var actionRow = new GameObject("Actions", typeof(RectTransform));
            actionRow.transform.SetParent(content, false);
            actionRow.AddComponent<LayoutElement>().preferredHeight = UiTheme.ButtonPrimaryHeight;
            var actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = UiTheme.Space2;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;

            var equipHandle = UiFactory.Button(actionRow.transform, "EQUIP", UiFactory.ButtonKind.Primary, () =>
            {
                if (gm != null)
                {
                    gm.EquipItemFromInventory(item, currentTarget);
                    onChanged?.Invoke();
                }
                getSheet()?.Close();
            });
            equipHandle.Root.GetComponent<LayoutElement>().flexibleWidth = 2;

            var sellHandle = UiFactory.Button(actionRow.transform,
                $"Sell {NumberFormatter.FormatCompact(data.SellValue)}g",
                UiFactory.ButtonKind.Destructive, () =>
            {
                if (gm != null)
                {
                    gm.SellItem(item);
                    onChanged?.Invoke();
                }
                getSheet()?.Close();
            });
            sellHandle.Root.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        private static int FindBestTargetIndex(GameManager gm, EquipmentInstance item)
        {
            if (gm == null || gm.Roster == null || item == null) return 0;
            int best = 0;
            float bestDelta = float.MinValue;
            for (int i = 0; i < gm.Roster.Characters.Count; i++)
            {
                var diff = ItemComparer.Compare(item, gm.Roster.Characters[i].equipment[(int)item.Slot]);
                if (diff.TotalDelta > bestDelta) { bestDelta = diff.TotalDelta; best = i; }
            }
            return best;
        }

        private static EquipmentInstance GetEquipped(GameManager gm, int characterIndex, EquipmentInstance item)
        {
            if (gm == null || gm.Roster == null || item == null) return null;
            if (characterIndex < 0 || characterIndex >= gm.Roster.Characters.Count) return null;
            return gm.Roster.Characters[characterIndex].equipment[(int)item.Slot];
        }
    }
}
