using System.Collections;
using UnityEngine;
using Starquill.Exploration;
using Starquill.Managers;
using Starquill.Quests;

namespace Starquill.UI
{
    /// State-driven quest strip on the explore screen:
    /// Offered = pulsing discovery notice (tap -> offer sheet)
    /// Retreated = retry notice (tap -> retry sheet)
    /// Active = wave progress HUD + Retreat button (confirmation sheet)
    /// Idle = hidden.
    public class QuestBannerDisplay : MonoBehaviour
    {
        private UiFactory.BannerHandle banner;
        private UiFactory.ProgressBarHandle waveBar;
        private bool subscribed;

        private IEnumerator Start()
        {
            banner = UiFactory.Banner(transform,
                actionLabel: "Retreat",
                onAction: HandleActionTapped,
                onTapped: HandleBannerTapped);

            // Thin wave-progress fill along the banner's bottom edge
            waveBar = UiFactory.ProgressBar(banner.Root.transform, UiTheme.AccentGreen, 10f);
            Destroy(waveBar.Root.GetComponent<UnityEngine.UI.LayoutElement>());
            var barRT = waveBar.Root.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0, 0);
            barRT.anchorMax = new Vector2(1, 0);
            barRT.pivot = new Vector2(0.5f, 0);
            barRT.sizeDelta = new Vector2(0, 10);
            barRT.anchoredPosition = Vector2.zero;

            yield return null; // wait for GameManager.Start()

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnQuestOffered += HandleQuestChanged;
                gm.OnQuestRetreated += Refresh;
                gm.OnQuestCompleted += HandleQuestCompleted;
                gm.OnWaveStarted += HandleWaveStarted;
                subscribed = true;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (!subscribed || gm == null) return;
            gm.OnQuestOffered -= HandleQuestChanged;
            gm.OnQuestRetreated -= Refresh;
            gm.OnQuestCompleted -= HandleQuestCompleted;
            gm.OnWaveStarted -= HandleWaveStarted;
        }

        private void HandleQuestChanged(QuestSpec _) => Refresh();
        private void HandleWaveStarted(System.Collections.Generic.IReadOnlyList<Starquill.Combat.EnemyState> _) => Refresh();

        private void HandleQuestCompleted(QuestSpec spec, double goldBonus,
            System.Collections.Generic.List<Starquill.Equipment.EquipmentInstance> rewards)
        {
            Refresh();
            QuestCompletionSheet.Show(transform.root, spec, goldBonus, rewards);
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) { banner.SetVisible(false); return; }

            var log = gm.QuestLog;
            string text = QuestPresenter.BannerText(log.Phase, log.ActiveSpec, log.ActiveWaveIndex);
            if (string.IsNullOrEmpty(text))
            {
                banner.SetVisible(false);
                return;
            }

            banner.SetVisible(true);
            banner.SetText(text);

            bool active = log.Phase == QuestPhase.Active;
            banner.SetActionVisible(active);
            banner.SetPulsing(log.Phase == QuestPhase.Offered);
            banner.SetAccent(log.Phase switch
            {
                QuestPhase.Offered => UiTheme.BestGold,
                QuestPhase.Retreated => UiTheme.DeltaDown,
                _ => UiTheme.AccentGreen
            });

            waveBar.Root.SetActive(active);
            if (active)
                waveBar.SetFraction(QuestPresenter.WaveFraction(log.ActiveSpec, log.ActiveWaveIndex));
        }

        private void HandleBannerTapped()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            switch (gm.QuestLog.Phase)
            {
                case QuestPhase.Offered:
                    Open(QuestOfferSheet.Mode.Offer);
                    break;
                case QuestPhase.Retreated:
                    Open(QuestOfferSheet.Mode.Retry);
                    break;
            }
        }

        private void HandleActionTapped()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.QuestLog.Phase == QuestPhase.Active)
                Open(QuestOfferSheet.Mode.RetreatConfirm);
        }

        private void Open(QuestOfferSheet.Mode mode)
        {
            var sheet = QuestOfferSheet.Show(transform.root, mode);
            if (sheet != null) sheet.OnClosed += Refresh;
        }
    }
}
