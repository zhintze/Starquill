using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Managers;
using Starquill.Quests;
using Starquill.Destinations;

namespace Starquill.UI
{
    /// Quests screen (nav index 1): one scroll view with section headers:
    /// QUEST (current-quest card + zone ladder) then DESTINATIONS (fragment
    /// progress + the live key pouch, plus a locked card for the location
    /// system from the dungeon-key design doc).
    public class QuestsScreenController : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private ScreenManager screenManager;

        private bool initialized;

        private IEnumerator Start()
        {
            yield return null; // wait for GameManager.Start()
            Initialize();
            Refresh();
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;

            if (screenManager != null)
                screenManager.OnScreenChanged += HandleScreenChanged;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnQuestOffered += _ => Refresh();
                gm.OnQuestRetreated += Refresh;
                gm.OnQuestCompleted += (s, g, l) => Refresh();
                gm.OnKeysChanged += HandleKeysChanged;
            }
        }

        private void HandleScreenChanged(int index)
        {
            if (index == 1) Refresh();
        }

        private void HandleKeysChanged() => Refresh();

        public void Refresh()
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            var gm = GameManager.Instance;
            if (gm == null) return;

            var log = gm.QuestLog;
            var zone = gm.QuestZones.GetZone(log.ZoneIndex);

            // ===== QUEST section =====
            SectionHeader("QUEST");
            BuildCurrentQuestCard(gm, log);

            var zoneHeader = UiFactory.Text(content,
                $"{zone?.Name ?? "Unknown"}  " + UiFactory.ColorTag($"Zone {log.ZoneIndex + 1}", UiTheme.TextDim),
                UiFactory.TextStyle.Body);
            zoneHeader.fontStyle = FontStyles.Bold;
            zoneHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 16f;

            UiFactory.LadderRow(content,
                QuestPresenter.LadderStates(log.NextQuestIndex, log.Phase), 72f);

            // ===== DESTINATIONS section =====
            SectionHeader("DESTINATIONS");

            var gmFrag = gm.Exploration.FragmentProgress;
            const float target = 100f;
            var fragLabel = UiFactory.Text(content,
                $"Fragments  {gmFrag:F0}/{target:F0} " +
                UiFactory.ColorTag("· gathered while exploring", UiTheme.TextDim),
                UiFactory.TextStyle.Caption);
            fragLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 12f;
            var fragBar = UiFactory.ProgressBar(content, new Color(0.2f, 0.8f, 0.7f), 28f);
            fragBar.SetFraction(gmFrag / target);

            BuildKeyPouch(gm);
            LockedCard("Locations", "Discovered places, open briefly. Grind fast.");
        }

        private void BuildKeyPouch(GameManager gm)
        {
            var pouch = gm.KeyPouch;
            int softCap = gm.economyConfig != null ? gm.economyConfig.keySoftCap : 30;

            var header = UiFactory.Text(content,
                "KEYS  " + UiFactory.ColorTag($"{pouch.Keys.Count}/{softCap}", UiTheme.TextDim),
                UiFactory.TextStyle.Body);
            header.fontStyle = FontStyles.Bold;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 16f;

            if (pouch.Keys.Count == 0)
            {
                var empty = UiFactory.Text(content,
                    "Keys drop while exploring. Spend them on dungeons.",
                    UiFactory.TextStyle.CaptionDim);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 16f;
                return;
            }

            foreach (var key in pouch.Keys)
                BuildKeyCard(gm, key);
        }

        private void BuildKeyCard(GameManager gm, KeyInstance key)
        {
            var card = new GameObject($"Key_{key.DisplayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(content, false);
            card.GetComponent<Image>().color = UiTheme.Card;
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 160f;
            le.flexibleWidth = 1;

            // Accent stripe: the key's color family at a glance
            var stripe = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(card.transform, false);
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0, 0);
            stripeRT.anchorMax = new Vector2(0, 1);
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(16f, 0);
            stripe.GetComponent<Image>().color = KeyPresenter.AccentColor(key);

            float textLeft = 16f + UiTheme.Space3;

            var title = UiFactory.Text(card.transform, KeyPresenter.Title(key), UiFactory.TextStyle.Body);
            title.fontStyle = FontStyles.Bold;
            var titleRT = title.rectTransform;
            titleRT.anchorMin = new Vector2(0, 0.5f);
            titleRT.anchorMax = new Vector2(0.68f, 1f);
            titleRT.offsetMin = new Vector2(textLeft, 0);
            titleRT.offsetMax = Vector2.zero;

            var sub = UiFactory.Text(card.transform,
                KeyPresenter.SubLine(key, gm.economyConfig), UiFactory.TextStyle.CaptionDim);
            var subRT = sub.rectTransform;
            subRT.anchorMin = new Vector2(0, 0);
            subRT.anchorMax = new Vector2(1, 0.5f);
            subRT.offsetMin = new Vector2(textLeft, UiTheme.Space1);
            subRT.offsetMax = new Vector2(-UiTheme.Space3, 0);

            int maxD = gm.economyConfig != null ? gm.economyConfig.keyMaxDifficulty : 6;
            var pips = UiFactory.PipRow(card.transform,
                KeyPresenter.DifficultyPips(key), maxD, 24f);
            var pipsRT = pips.GetComponent<RectTransform>();
            pipsRT.anchorMin = new Vector2(1, 0.75f);
            pipsRT.anchorMax = new Vector2(1, 0.75f);
            pipsRT.pivot = new Vector2(1, 0.5f);
            pipsRT.anchoredPosition = new Vector2(-UiTheme.Space3, 0);
            pipsRT.sizeDelta = new Vector2(maxD * 32f, 32f);

            card.GetComponent<Button>().onClick.AddListener(() =>
                KeyDetailSheet.Show(transform.root, key));
        }

        private void SectionHeader(string title)
        {
            var header = new GameObject($"Section_{title}", typeof(RectTransform));
            header.transform.SetParent(content, false);
            header.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 28f;

            var tmp = UiFactory.Text(header.transform, title, UiFactory.TextStyle.Heading);
            tmp.color = UiTheme.TextSecondary;
            var rt = tmp.rectTransform;
            UiFactory.StretchFill(rt);
            rt.offsetMin = new Vector2(0, 0);
            tmp.alignment = TextAlignmentOptions.BottomLeft;

            var rule = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            rule.transform.SetParent(header.transform, false);
            var ruleRT = rule.GetComponent<RectTransform>();
            ruleRT.anchorMin = new Vector2(0, 0);
            ruleRT.anchorMax = new Vector2(1, 0);
            ruleRT.pivot = new Vector2(0.5f, 0);
            ruleRT.sizeDelta = new Vector2(0, 3);
            rule.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        }

        private void BuildCurrentQuestCard(GameManager gm, QuestLog log)
        {
            var card = new GameObject("CurrentQuest", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(content, false);
            card.GetComponent<Image>().color = UiTheme.Card;
            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiTheme.Space2;
            layout.padding = new RectOffset((int)UiTheme.CardPadding, (int)UiTheme.CardPadding,
                (int)UiTheme.Space3, (int)UiTheme.Space3);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var spec = log.ActiveSpec;
            switch (log.Phase)
            {
                case QuestPhase.Offered:
                {
                    Title(card.transform, spec.DisplayName, UiTheme.BestGold);
                    Caption(card.transform,
                        $"{QuestPresenter.TierLabel(spec.Tier)} · {spec.Waves.Length} waves");
                    Caption(card.transform,
                        QuestPresenter.RewardsPreview(spec.Reward, gm.EstimateQuestGold(spec)));
                    ActionRow(card.transform,
                        ("ACCEPT", UiFactory.ButtonKind.Primary, (System.Action)(() => { gm.AcceptQuest(); Refresh(); })),
                        ("Decline", UiFactory.ButtonKind.Secondary, () => { gm.DeclineQuest(); Refresh(); }));
                    break;
                }
                case QuestPhase.Active:
                {
                    Title(card.transform, spec.DisplayName, UiTheme.AccentGreen);
                    Caption(card.transform,
                        QuestPresenter.BannerText(QuestPhase.Active, spec, log.ActiveWaveIndex));
                    var bar = UiFactory.ProgressBar(card.transform, UiTheme.AccentGreen);
                    bar.SetFraction(QuestPresenter.WaveFraction(spec, log.ActiveWaveIndex));
                    ActionRow(card.transform,
                        ("Return to battle", UiFactory.ButtonKind.Primary,
                            (System.Action)(() => screenManager?.ShowScreen(0))),
                        ("Retreat", UiFactory.ButtonKind.Destructive, () =>
                        {
                            var sheet = QuestOfferSheet.Show(transform.root, QuestOfferSheet.Mode.RetreatConfirm);
                            if (sheet != null) sheet.OnClosed += Refresh;
                        }));
                    break;
                }
                case QuestPhase.Retreated:
                {
                    Title(card.transform, spec.DisplayName, UiTheme.DeltaDown);
                    Caption(card.transform, "Retreated — the quest waits for another attempt.");
                    ActionRow(card.transform,
                        ("RETRY", UiFactory.ButtonKind.Primary,
                            (System.Action)(() => { gm.RetryQuest(); screenManager?.ShowScreen(0); })),
                        ("Dismiss", UiFactory.ButtonKind.Destructive, () => { gm.DismissRetreatedQuest(); Refresh(); }));
                    break;
                }
                default:
                {
                    Title(card.transform, "Exploring…", UiTheme.TextPrimary);
                    Caption(card.transform,
                        "The party is searching for the next quest. The path bar fills toward a guaranteed discovery.");
                    break;
                }
            }
        }

        private void LockedCard(string title, string teaser)
        {
            var card = new GameObject($"Locked_{title}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(content, false);
            card.GetComponent<Image>().color = new Color(UiTheme.Card.r, UiTheme.Card.g, UiTheme.Card.b, 0.6f);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 150f;
            le.flexibleWidth = 1;

            var titleTmp = UiFactory.Text(card.transform, title, UiFactory.TextStyle.Body);
            titleTmp.color = UiTheme.TextSecondary;
            titleTmp.fontStyle = FontStyles.Bold;
            var titleRT = titleTmp.rectTransform;
            titleRT.anchorMin = new Vector2(0, 0.5f);
            titleRT.anchorMax = new Vector2(0.7f, 1f);
            titleRT.offsetMin = new Vector2(UiTheme.Space3, 0);
            titleRT.offsetMax = Vector2.zero;

            var teaserTmp = UiFactory.Text(card.transform, teaser, UiFactory.TextStyle.CaptionDim);
            var teaserRT = teaserTmp.rectTransform;
            teaserRT.anchorMin = new Vector2(0, 0);
            teaserRT.anchorMax = new Vector2(1, 0.5f);
            teaserRT.offsetMin = new Vector2(UiTheme.Space3, UiTheme.Space1);
            teaserRT.offsetMax = new Vector2(-UiTheme.Space3, 0);

            var chip = UiFactory.Chip(card.transform, "LOCKED", new Color(0.3f, 0.3f, 0.38f), 200, 56);
            var chipRT = chip.GetComponent<RectTransform>();
            chipRT.anchorMin = new Vector2(1, 0.72f);
            chipRT.anchorMax = new Vector2(1, 0.72f);
            chipRT.pivot = new Vector2(1, 0.5f);
            chipRT.anchoredPosition = new Vector2(-UiTheme.Space3, 0);
        }

        private static void Title(Transform parent, string text, Color color)
        {
            var tmp = UiFactory.Text(parent, text, UiFactory.TextStyle.Heading);
            tmp.color = color;
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 10f;
        }

        private static void Caption(Transform parent, string text)
        {
            var tmp = UiFactory.Text(parent, text, UiFactory.TextStyle.Caption);
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 10f;
        }

        private void ActionRow(Transform parent,
            params (string label, UiFactory.ButtonKind kind, System.Action action)[] buttons)
        {
            var row = new GameObject("Actions", typeof(RectTransform));
            row.transform.SetParent(parent, false);
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
                UiFactory.Button(row.transform, label, kind, action);
        }

        private void OnDestroy()
        {
            if (screenManager != null)
                screenManager.OnScreenChanged -= HandleScreenChanged;
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnKeysChanged -= HandleKeysChanged;
        }
    }
}
