using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Managers;
using Starquill.Quests;

namespace Starquill.UI
{
    /// Quests screen (nav index 1): [Quests | Destinations] tabs.
    /// Quests tab: current-quest card + zone ladder. Destinations tab:
    /// live fragment progress + locked future-slot cards for the
    /// encounter/location/dungeon systems (dungeon-key design doc).
    public class QuestsScreenController : MonoBehaviour
    {
        [SerializeField] private Transform tabsContainer;
        [SerializeField] private Transform questsContent;
        [SerializeField] private Transform destinationsContent;
        [SerializeField] private ScreenManager screenManager;

        private bool initialized;
        private int activeTab;

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

            if (tabsContainer != null)
            {
                UiFactory.SegmentedTabs(tabsContainer,
                    new[] { "Quests", "Destinations" }, idx =>
                    {
                        activeTab = idx;
                        if (questsContent != null) questsContent.gameObject.SetActive(idx == 0);
                        if (destinationsContent != null) destinationsContent.gameObject.SetActive(idx == 1);
                        Refresh();
                    });
            }

            if (screenManager != null)
                screenManager.OnScreenChanged += HandleScreenChanged;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnQuestOffered += _ => Refresh();
                gm.OnQuestRetreated += Refresh;
                gm.OnQuestCompleted += (s, g, l) => Refresh();
            }

            BuildDestinations();
        }

        private void HandleScreenChanged(int index)
        {
            if (index == 1) Refresh();
        }

        public void Refresh()
        {
            if (activeTab == 0) BuildQuestsTab();
            else RefreshFragmentBar();
        }

        // ---------- Quests tab ----------

        private void BuildQuestsTab()
        {
            if (questsContent == null) return;
            for (int i = questsContent.childCount - 1; i >= 0; i--)
                Destroy(questsContent.GetChild(i).gameObject);

            var gm = GameManager.Instance;
            if (gm == null) return;

            var log = gm.QuestLog;
            var zone = gm.QuestZones.GetZone(log.ZoneIndex);

            BuildCurrentQuestCard(gm, log, zone);

            // Zone header
            var header = UiFactory.Text(questsContent,
                $"{zone?.Name ?? "Unknown"}  " + UiFactory.ColorTag($"Zone {log.ZoneIndex + 1}", UiTheme.TextDim),
                UiFactory.TextStyle.Heading);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 24f;

            UiFactory.LadderRow(questsContent,
                QuestPresenter.LadderStates(log.NextQuestIndex, log.Phase), 72f);
        }

        private void BuildCurrentQuestCard(GameManager gm, QuestLog log, QuestZone zone)
        {
            var card = new GameObject("CurrentQuest", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(questsContent, false);
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
                        "The party is searching for the next quest. Discoveries happen while clearing waves.");
                    break;
                }
            }
        }

        // ---------- Destinations tab ----------

        private UiFactory.ProgressBarHandle fragmentBar;
        private TMP_Text fragmentLabel;

        private void BuildDestinations()
        {
            if (destinationsContent == null) return;

            var header = UiFactory.Text(destinationsContent, "Fragments", UiFactory.TextStyle.Heading);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 16f;

            fragmentLabel = UiFactory.Text(destinationsContent, "", UiFactory.TextStyle.CaptionDim);
            fragmentLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 10f;

            fragmentBar = UiFactory.ProgressBar(destinationsContent, new Color(0.2f, 0.8f, 0.7f), 32f);

            LockedCard("Encounters", "Spend keys on targeted loot hunts.");
            LockedCard("Locations", "Discovered places, open briefly. Grind fast.");
            LockedCard("Dungeons", "Extended multi-zone challenges with curated loot.");

            RefreshFragmentBar();
            destinationsContent.gameObject.SetActive(false);
        }

        private void RefreshFragmentBar()
        {
            var gm = GameManager.Instance;
            if (gm == null || fragmentLabel == null) return;
            float progress = gm.Exploration.FragmentProgress;
            const float target = 100f;
            fragmentLabel.text = $"{progress:F0}/{target:F0} · fragments gather while exploring";
            fragmentBar.SetFraction(progress / target);
        }

        private void LockedCard(string title, string teaser)
        {
            var card = new GameObject($"Locked_{title}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(destinationsContent, false);
            card.GetComponent<Image>().color = new Color(UiTheme.Card.r, UiTheme.Card.g, UiTheme.Card.b, 0.6f);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 170f;
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
        }
    }
}
