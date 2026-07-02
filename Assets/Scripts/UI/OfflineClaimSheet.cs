using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Managers;
using Starquill.Services;

namespace Starquill.UI
{
    /// Welcome-back sheet: shows pending offline earnings with Claim / 2x-ad.
    /// Shown once per launch when GameManager has pending earnings.
    public static class OfflineClaimSheet
    {
        public static BottomSheet ShowIfPending(Transform canvasRoot)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.PendingOfflineGold <= 0) return null;

            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot, content =>
            {
                var title = UiFactory.Text(content, "Welcome back", UiFactory.TextStyle.Heading);
                title.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 14f;

                var summary = UiFactory.Text(content,
                    ShopPresenter.OfflineSummary(gm.PendingOfflineSeconds, gm.PendingOfflineGold),
                    UiFactory.TextStyle.Body);
                summary.color = UiTheme.BestGold;
                summary.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 12f;

                var caption = UiFactory.Text(content,
                    "The party kept exploring while you were away.", UiFactory.TextStyle.CaptionDim);
                caption.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 10f;

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

                UiFactory.Button(row.transform, "CLAIM", UiFactory.ButtonKind.Primary, () =>
                {
                    gm.ClaimOfflineEarnings(false);
                    sheet?.Close();
                });
                var adHandle = UiFactory.Button(row.transform, "CLAIM 2x (ad)", UiFactory.ButtonKind.Secondary, () =>
                {
                    gm.Ads.ShowRewarded(AdPlacement.Offline2x, ok =>
                    {
                        gm.ClaimOfflineEarnings(ok);
                        sheet?.Close();
                    });
                });
                adHandle.SetEnabled(gm.Ads.IsReady(AdPlacement.Offline2x), "Ad not ready");
            }, heightFraction: 0.38f);

            // Dismissing without choosing still grants the plain amount —
            // away-earnings are never lost.
            sheet.OnClosed += () => gm.ClaimOfflineEarnings(false);
            return sheet;
        }
    }
}
