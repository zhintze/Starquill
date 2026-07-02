using System.Collections;
using TMPro;
using UnityEngine;
using Starquill.Exploration;
using Starquill.Managers;
using Starquill.Quests;

namespace Starquill.UI
{
    /// The path indicator bar (design: Sprint 4 open questions) — always
    /// visible below the TopBar, dual-mode:
    ///   Explore: travel percentage toward a guaranteed quest discovery
    ///   Quest:   wave progress through the active quest
    public class PathBarDisplay : MonoBehaviour
    {
        private UiFactory.ProgressBarHandle bar;
        private TMP_Text label;
        private bool subscribed;

        private IEnumerator Start()
        {
            bar = UiFactory.ProgressBar(transform, UiTheme.AccentGreen, 14f);
            Destroy(bar.Root.GetComponent<UnityEngine.UI.LayoutElement>());
            var barRT = bar.Root.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0, 0);
            barRT.anchorMax = new Vector2(1, 0);
            barRT.pivot = new Vector2(0.5f, 0);
            barRT.sizeDelta = new Vector2(0, 14);
            barRT.anchoredPosition = Vector2.zero;

            label = UiFactory.Text(transform, "", UiFactory.TextStyle.CaptionDim,
                TextAlignmentOptions.MidlineRight);
            var labelRT = label.rectTransform;
            labelRT.anchorMin = new Vector2(0, 0);
            labelRT.anchorMax = new Vector2(1, 1);
            labelRT.offsetMin = new Vector2(UiTheme.Space3, 12);
            labelRT.offsetMax = new Vector2(-UiTheme.Space3, 0);

            yield return null; // wait for GameManager.Start()

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnWaveCleared += Refresh;
                gm.OnWaveStarted += HandleWaveStarted;
                gm.OnQuestCompleted += HandleQuestCompleted;
                gm.OnQuestRetreated += Refresh;
                subscribed = true;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (!subscribed || gm == null) return;
            gm.OnWaveCleared -= Refresh;
            gm.OnWaveStarted -= HandleWaveStarted;
            gm.OnQuestCompleted -= HandleQuestCompleted;
            gm.OnQuestRetreated -= Refresh;
        }

        private void HandleWaveStarted(System.Collections.Generic.IReadOnlyList<Starquill.Combat.EnemyState> _) => Refresh();
        private void HandleQuestCompleted(QuestSpec s, double g,
            System.Collections.Generic.List<Starquill.Equipment.EquipmentInstance> l) => Refresh();

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var log = gm.QuestLog;
            bool inQuest = gm.Exploration.State == ExplorationState.InQuest
                && log.Phase == QuestPhase.Active && log.ActiveSpec != null;

            if (inQuest)
            {
                bar.SetFraction(QuestPresenter.WaveFraction(log.ActiveSpec, log.ActiveWaveIndex));
                label.text = QuestPresenter.BannerText(QuestPhase.Active, log.ActiveSpec, log.ActiveWaveIndex);
            }
            else
            {
                bar.SetFraction(gm.Exploration.TravelProgress);
                label.text = $"Traveling · {gm.Exploration.TravelProgress * 100f:F0}% to the next discovery";
            }
        }
    }
}
