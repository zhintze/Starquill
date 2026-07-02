using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Destinations;
using Starquill.Managers;

namespace Starquill.UI
{
    /// In-run dungeon strip on the explore screen (QuestBannerDisplay's slot
    /// pattern, one row below the quest banner): key name, m:ss countdown,
    /// waves vs par. Gold-tinted once par is beaten. Hidden outside a run.
    public class DungeonHudDisplay : MonoBehaviour
    {
        private const float RefreshInterval = 0.2f; // 5 Hz keeps the countdown live

        private GameObject strip;
        private TMP_Text title;
        private TMP_Text countdown;
        private TMP_Text waveLine;
        private Image[] accents;
        private float refreshTimer;
        private bool subscribed;

        private IEnumerator Start()
        {
            BuildUi();
            strip.SetActive(false);

            yield return null; // wait for GameManager.Start()

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnDungeonStarted += HandleDungeonStarted;
                gm.OnDungeonEnded += HandleDungeonEnded;
                subscribed = true;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (!subscribed || gm == null) return;
            gm.OnDungeonStarted -= HandleDungeonStarted;
            gm.OnDungeonEnded -= HandleDungeonEnded;
        }

        private void Update()
        {
            if (strip == null || !strip.activeSelf) return;
            refreshTimer += Time.deltaTime;
            if (refreshTimer < RefreshInterval) return;
            refreshTimer = 0f;
            Refresh();
        }

        private void HandleDungeonStarted(DungeonSpec _) => Refresh();

        private void HandleDungeonEnded(DungeonRun _,
            System.Collections.Generic.List<Starquill.Equipment.EquipmentInstance> __,
            double ___) => Refresh();

        private void Refresh()
        {
            if (strip == null) return;
            var gm = GameManager.Instance;
            var run = gm != null ? gm.ActiveDungeon : null;
            if (run == null)
            {
                strip.SetActive(false);
                return;
            }

            strip.SetActive(true);
            title.text = run.Spec.Key.DisplayName;
            countdown.text = KeyPresenter.FormatMinSec(Mathf.CeilToInt(run.TimeLeft));

            bool parBeaten = run.WavesCleared > run.Spec.ParWaves;
            waveLine.text = $"Waves {run.WavesCleared} / par {run.Spec.ParWaves}"
                + (parBeaten ? " · bonus loot" : "");
            waveLine.color = parBeaten ? UiTheme.BestGold : UiTheme.TextSecondary;
            var accent = parBeaten ? UiTheme.BestGold : KeyPresenter.AccentColor(run.Spec.Key);
            foreach (var a in accents)
                if (a != null) a.color = accent;
        }

        private void BuildUi()
        {
            strip = new GameObject("DungeonStrip", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(transform, false);
            UiFactory.StretchFill((RectTransform)strip.transform);
            strip.GetComponent<Image>().color = UiTheme.Card;

            // Accent edges, same construction as UiFactory.Banner
            accents = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var edge = new GameObject(i == 0 ? "AccentTop" : "AccentBottom",
                    typeof(RectTransform), typeof(Image));
                edge.transform.SetParent(strip.transform, false);
                var rt = edge.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, i == 0 ? 1 : 0);
                rt.anchorMax = new Vector2(1, i == 0 ? 1 : 0);
                rt.pivot = new Vector2(0.5f, i == 0 ? 1 : 0);
                rt.sizeDelta = new Vector2(0, 4);
                accents[i] = edge.GetComponent<Image>();
                accents[i].color = UiTheme.AccentGreen;
            }

            title = UiFactory.Text(strip.transform, "", UiFactory.TextStyle.Body);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            var titleRT = title.rectTransform;
            titleRT.anchorMin = new Vector2(0, 0.5f);
            titleRT.anchorMax = new Vector2(0.72f, 1f);
            titleRT.offsetMin = new Vector2(UiTheme.Space3, 0);
            titleRT.offsetMax = Vector2.zero;

            countdown = UiFactory.Text(strip.transform, "",
                UiFactory.TextStyle.Body, TextAlignmentOptions.MidlineRight);
            countdown.fontStyle = FontStyles.Bold;
            var countdownRT = countdown.rectTransform;
            countdownRT.anchorMin = new Vector2(0.72f, 0.5f);
            countdownRT.anchorMax = new Vector2(1f, 1f);
            countdownRT.offsetMin = Vector2.zero;
            countdownRT.offsetMax = new Vector2(-UiTheme.Space3, 0);

            waveLine = UiFactory.Text(strip.transform, "", UiFactory.TextStyle.Caption);
            var waveRT = waveLine.rectTransform;
            waveRT.anchorMin = new Vector2(0, 0);
            waveRT.anchorMax = new Vector2(1, 0.5f);
            waveRT.offsetMin = new Vector2(UiTheme.Space3, UiTheme.Space1);
            waveRT.offsetMax = new Vector2(-UiTheme.Space3, 0);
        }
    }
}
