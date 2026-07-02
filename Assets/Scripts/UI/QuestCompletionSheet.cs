using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Equipment;
using Starquill.Managers;
using Starquill.Quests;

namespace Starquill.UI
{
    /// Reward reveal after a quest completes: dialogue, gold, reward item
    /// cards (tap-through to item detail), next-quest teaser.
    public static class QuestCompletionSheet
    {
        public static BottomSheet Show(Transform canvasRoot, QuestSpec spec,
            double goldBonus, List<EquipmentInstance> rewards)
        {
            var gm = GameManager.Instance;
            if (gm == null || spec == null) return null;

            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot, content =>
                Fill(content, gm, spec, goldBonus, rewards, () => sheet), heightFraction: 0.62f);
            return sheet;
        }

        private static void Fill(Transform content, GameManager gm, QuestSpec spec,
            double goldBonus, List<EquipmentInstance> rewards, System.Func<BottomSheet> getSheet)
        {
            var zone = gm.QuestZones.GetZone(spec.ZoneIndex);
            bool zoneCleared = spec.Tier == QuestTier.Boss;

            var title = UiFactory.Text(content, $"{spec.DisplayName} complete",
                UiFactory.TextStyle.Heading);
            title.color = UiTheme.BestGold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 14f;

            string dialogue = QuestPresenter.PickDialogue(zone?.CompletionDialogue, spec.QuestIndex);
            if (!string.IsNullOrEmpty(dialogue))
            {
                var d = UiFactory.Text(content, $"\"{dialogue}\"", UiFactory.TextStyle.Caption);
                d.fontStyle = FontStyles.Italic;
                d.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 14f;
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

            if (zoneCleared)
            {
                var nextZone = gm.QuestZones.GetZone(spec.ZoneIndex + 1);
                string nextName = nextZone != null && nextZone != zone ? nextZone.Name : "the frontier";
                var cleared = UiFactory.Text(content,
                    $"Zone cleared — {nextName} unlocked", UiFactory.TextStyle.Body);
                cleared.color = UiTheme.AccentGreen;
                cleared.fontStyle = FontStyles.Bold;
                cleared.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 12f;
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
