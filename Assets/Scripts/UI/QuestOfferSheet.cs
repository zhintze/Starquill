using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Managers;
using Starquill.Quests;

namespace Starquill.UI
{
    /// Bottom-sheet dialogs for the quest flow: the discovery offer, the
    /// retry prompt after a retreat, and the retreat confirmation.
    public static class QuestOfferSheet
    {
        public enum Mode { Offer, Retry, RetreatConfirm }

        public static BottomSheet Show(Transform canvasRoot, Mode mode)
        {
            var gm = GameManager.Instance;
            var spec = gm != null ? gm.QuestLog.ActiveSpec : null;
            if (gm == null || spec == null) return null;

            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot, content =>
                Fill(content, gm, spec, mode, () => sheet), heightFraction: 0.5f);
            return sheet;
        }

        private static void Fill(Transform content, GameManager gm, QuestSpec spec,
            Mode mode, System.Func<BottomSheet> getSheet)
        {
            var zone = gm.QuestZones.GetZone(spec.ZoneIndex);

            // Header: name + tier chip row
            var headerRow = new GameObject("Header", typeof(RectTransform));
            headerRow.transform.SetParent(content, false);
            headerRow.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 16f;

            var name = UiFactory.Text(headerRow.transform, spec.DisplayName, UiFactory.TextStyle.Heading);
            var nameRT = name.rectTransform;
            nameRT.anchorMin = Vector2.zero;
            nameRT.anchorMax = new Vector2(0.72f, 1);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;

            Color tierColor = spec.Tier switch
            {
                QuestTier.Elite => new Color(0.65f, 0.4f, 0.9f),
                QuestTier.Hard => new Color(0.9f, 0.45f, 0.2f),
                QuestTier.Boss => UiTheme.BestGold,
                _ => UiTheme.DeltaNeutral
            };
            var chip = UiFactory.Chip(headerRow.transform, QuestPresenter.TierLabel(spec.Tier),
                new Color(tierColor.r, tierColor.g, tierColor.b, 0.35f), 220, 64);
            var chipRT = chip.GetComponent<RectTransform>();
            chipRT.anchorMin = new Vector2(1, 0.5f);
            chipRT.anchorMax = new Vector2(1, 0.5f);
            chipRT.pivot = new Vector2(1, 0.5f);
            chipRT.anchoredPosition = Vector2.zero;

            if (mode == Mode.RetreatConfirm)
            {
                AddCaption(content, "Retreating keeps only half the gold earned this attempt.");
                AddCaption(content,
                    $"Earned so far: {NumberFormatter.FormatCompact(gm.QuestLog.GoldEarnedInQuest)}g");

                Actions(content, getSheet,
                    ("Keep fighting", UiFactory.ButtonKind.Secondary, (System.Action)(() => { })),
                    ("Retreat", UiFactory.ButtonKind.Destructive, () => gm.RetreatQuest()));
                return;
            }

            // Dialogue + facts
            string dialogue = QuestPresenter.PickDialogue(zone?.IntroDialogue, spec.QuestIndex);
            if (!string.IsNullOrEmpty(dialogue))
            {
                var d = AddCaption(content, $"\"{dialogue}\"");
                d.fontStyle = FontStyles.Italic;
            }
            AddCaption(content, $"{spec.Waves.Length} waves");
            AddCaption(content, QuestPresenter.RewardsPreview(spec.Reward, gm.EstimateQuestGold(spec)));

            if (mode == Mode.Offer)
            {
                Actions(content, getSheet,
                    ("ACCEPT", UiFactory.ButtonKind.Primary, (System.Action)(() => gm.AcceptQuest())),
                    ("Decline", UiFactory.ButtonKind.Secondary, () => gm.DeclineQuest()));
            }
            else // Retry
            {
                Actions(content, getSheet,
                    ("RETRY", UiFactory.ButtonKind.Primary, (System.Action)(() => gm.RetryQuest())),
                    ("Dismiss", UiFactory.ButtonKind.Destructive, () => gm.DismissRetreatedQuest()));
            }
        }

        private static TMP_Text AddCaption(Transform content, string text)
        {
            var tmp = UiFactory.Text(content, text, UiFactory.TextStyle.Caption);
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 14f;
            return tmp;
        }

        private static void Actions(Transform content, System.Func<BottomSheet> getSheet,
            params (string label, UiFactory.ButtonKind kind, System.Action action)[] buttons)
        {
            var row = new GameObject("Actions", typeof(RectTransform));
            row.transform.SetParent(content, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.ButtonPrimaryHeight;
            le.flexibleHeight = 0;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiTheme.Space2;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            foreach (var (label, kind, action) in buttons)
            {
                var captured = action;
                UiFactory.Button(row.transform, label, kind, () =>
                {
                    captured?.Invoke();
                    getSheet()?.Close();
                });
            }
        }
    }
}
