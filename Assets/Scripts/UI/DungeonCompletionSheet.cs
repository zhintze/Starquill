using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Destinations;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Reward reveal after a dungeon run ends (mirrors QuestCompletionSheet):
    /// waves vs par, bonus-roll callout, gold, reward item cards (tap-through
    /// to detail), and a mailbox notice when rewards did not fit.
    public static class DungeonCompletionSheet
    {
        public static BottomSheet Show(Transform canvasRoot, DungeonRun run,
            List<EquipmentInstance> rewards, double goldBonus)
        {
            var gm = GameManager.Instance;
            if (gm == null || run == null) return null;

            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot, content =>
                Fill(content, gm, run, rewards, goldBonus, () => sheet), heightFraction: 0.62f);
            return sheet;
        }

        private static void Fill(Transform content, GameManager gm, DungeonRun run,
            List<EquipmentInstance> rewards, double goldBonus, System.Func<BottomSheet> getSheet)
        {
            var spec = run.Spec;

            var title = UiFactory.Text(content, $"{spec.Key.DisplayName} complete",
                UiFactory.TextStyle.Heading);
            title.color = UiTheme.BestGold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 14f;

            // Same function the reward roller used, so the callout number
            // matches the rolled rewards by construction.
            int bonusRolls = run.BonusRolls(gm.economyConfig.dungeonBonusWavesPerRoll,
                gm.economyConfig.dungeonBonusRollCap);

            var waves = UiFactory.Text(content,
                $"Waves cleared  {run.WavesCleared} / par {spec.ParWaves}",
                UiFactory.TextStyle.Body);
            waves.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 10f;

            if (bonusRolls > 0)
            {
                var bonus = UiFactory.Text(content,
                    $"Par beaten: +{bonusRolls} bonus reward roll{(bonusRolls > 1 ? "s" : "")}",
                    UiFactory.TextStyle.Body);
                bonus.color = UiTheme.BestGold;
                bonus.fontStyle = FontStyles.Bold;
                bonus.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 10f;
            }
            else
            {
                var bonus = UiFactory.Text(content,
                    "Beat par for bonus reward rolls", UiFactory.TextStyle.CaptionDim);
                bonus.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 10f;
            }

            var goldTmp = UiFactory.Text(content,
                $"+{NumberFormatter.FormatCompact(goldBonus)} gold", UiFactory.TextStyle.Display);
            goldTmp.color = UiTheme.BestGold;
            goldTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontDisplay + 12f;

            if (rewards != null && rewards.Count > 0)
            {
                foreach (var item in rewards)
                {
                    var data = ItemDisplayData.FromItem(item, gm.questLevel,
                        gm.AbilityTable, gm.economyConfig);
                    var captured = item;
                    ItemCardBuilder.Build(data, item, new ItemCardBuilder.CardOptions
                    {
                        ShowSprite = true,
                        HideDelta = true,
                        SlotIndexForSprite = (int)item.Slot,
                        OnTapped = () => ItemDetailSheet.Show(getSheet()?.transform.parent ?? content.root,
                            captured, gm.questLevel, null)
                    }, content);
                }
            }

            // Overflow rewards land in the mailbox (LootScreenController's
            // mailbox card is the collect path); surface that here.
            if (gm.RewardMailbox.Count > 0)
            {
                var notice = UiFactory.Text(content,
                    $"{gm.RewardMailbox.Count} reward{(gm.RewardMailbox.Count > 1 ? "s" : "")} waiting in the mailbox: make room in your inventory (Loot screen)",
                    UiFactory.TextStyle.Caption);
                notice.color = UiTheme.BestGold;
                notice.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 14f;
            }

            var row = new GameObject("Actions", typeof(RectTransform));
            row.transform.SetParent(content, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.ButtonPrimaryHeight;
            le.flexibleHeight = 0;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            UiFactory.Button(row.transform, "Continue", UiFactory.ButtonKind.Primary,
                () => getSheet()?.Close());
        }
    }
}
