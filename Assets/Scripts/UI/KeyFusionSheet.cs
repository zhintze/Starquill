using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Destinations;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Fills a BottomSheet with the fusion flow for one base key: a scrollable
    /// list of the other pouch keys (invalid partners dimmed with the rule they
    /// break), a preview block for the tapped partner, and a gold-gated CONFIRM.
    public static class KeyFusionSheet
    {
        public static BottomSheet Show(Transform canvasRoot, KeyInstance baseKey,
            Action onFused = null)
        {
            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot,
                content => Fill(content, baseKey, onFused, () => sheet),
                heightFraction: 0.7f);
            return sheet;
        }

        private static void Fill(Transform content, KeyInstance baseKey, Action onFused,
            Func<BottomSheet> getSheet)
        {
            var gm = GameManager.Instance;
            var config = gm != null ? gm.economyConfig : null;
            if (gm == null || config == null) return;

            // --- Header ---
            var name = UiFactory.Text(content,
                "FUSE  " + UiFactory.ColorTag(KeyPresenter.Title(baseKey),
                    KeyPresenter.AccentColor(baseKey)),
                UiFactory.TextStyle.Heading);
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 12f;

            var hint = UiFactory.Text(content,
                "Pick a second key. Difficulties add; matching modifiers merge.",
                UiFactory.TextStyle.CaptionDim);
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;

            // --- Preview block (below the list; filled on partner tap) ---
            // Built before the rows so their tap closures can target it.
            var previewHost = new GameObject("Preview", typeof(RectTransform));
            var previewLayout = previewHost.AddComponent<VerticalLayoutGroup>();
            previewLayout.spacing = UiTheme.Space1;
            previewLayout.childControlWidth = true;
            previewLayout.childControlHeight = true;
            previewLayout.childForceExpandWidth = true;
            previewLayout.childForceExpandHeight = false;

            // --- Scrollable partner list ---
            var scrollGO = new GameObject("PartnerScroll", typeof(RectTransform),
                typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollGO.transform.SetParent(content, false);
            scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredHeight = 460f;
            scrollLE.flexibleHeight = 0;

            var list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(scrollGO.transform, false);
            var listRT = list.GetComponent<RectTransform>();
            listRT.anchorMin = new Vector2(0, 1);
            listRT.anchorMax = new Vector2(1, 1);
            listRT.pivot = new Vector2(0.5f, 1);
            listRT.sizeDelta = Vector2.zero;
            var listLayout = list.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = UiTheme.Space1;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            var fitter = list.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = listRT;
            scroll.viewport = (RectTransform)scrollGO.transform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            void PickPartner(KeyInstance partner, KeyFusionPreview preview)
                => RebuildPreview(previewHost.transform, gm, config, baseKey, partner,
                    preview, onFused, getSheet);

            int partners = 0;
            foreach (var other in gm.KeyPouch.Keys)
            {
                if (other == baseKey) continue;
                partners++;
                var preview = KeyPresenter.FusionPreview(baseKey, other, gm.questLevel, config);
                BuildPartnerRow(list.transform, config, other, preview, PickPartner);
            }

            if (partners == 0)
            {
                var empty = UiFactory.Text(list.transform,
                    "No other keys to fuse. Keys drop while exploring.",
                    UiFactory.TextStyle.CaptionDim);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 16f;
            }

            // Parent the preview host after the list so it sits below it.
            previewHost.transform.SetParent(content, false);
            var placeholder = UiFactory.Text(previewHost.transform,
                "Select a key to preview the fusion.", UiFactory.TextStyle.CaptionDim);
            placeholder.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;
        }

        private static void BuildPartnerRow(Transform parent, Starquill.Data.EconomyConfig config,
            KeyInstance partner, KeyFusionPreview preview,
            Action<KeyInstance, KeyFusionPreview> onPick)
        {
            var row = new GameObject($"Partner_{partner.DisplayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().color = UiTheme.Card;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 150f;
            le.minHeight = UiTheme.TouchMin;
            le.flexibleWidth = 1;

            var stripe = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(row.transform, false);
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0, 0);
            stripeRT.anchorMax = new Vector2(0, 1);
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(16f, 0);
            stripe.GetComponent<Image>().color = KeyPresenter.AccentColor(partner);

            float textLeft = 16f + UiTheme.Space3;

            var title = UiFactory.Text(row.transform, KeyPresenter.Title(partner),
                UiFactory.TextStyle.Body);
            title.fontStyle = FontStyles.Bold;
            var titleRT = title.rectTransform;
            titleRT.anchorMin = new Vector2(0, preview.Valid ? 0f : 0.5f);
            titleRT.anchorMax = new Vector2(0.68f, 1f);
            titleRT.offsetMin = new Vector2(textLeft, 0);
            titleRT.offsetMax = Vector2.zero;

            if (!preview.Valid)
            {
                // The reason line teaches the fusion rules.
                var why = UiFactory.Text(row.transform, preview.InvalidReason,
                    UiFactory.TextStyle.CaptionDim);
                var whyRT = why.rectTransform;
                whyRT.anchorMin = new Vector2(0, 0);
                whyRT.anchorMax = new Vector2(1, 0.5f);
                whyRT.offsetMin = new Vector2(textLeft, UiTheme.Space1);
                whyRT.offsetMax = new Vector2(-UiTheme.Space3, 0);
            }

            var pips = UiFactory.PipRow(row.transform,
                KeyPresenter.DifficultyPips(partner), config.keyMaxDifficulty, 24f);
            var pipsRT = pips.GetComponent<RectTransform>();
            pipsRT.anchorMin = new Vector2(1, 0.5f);
            pipsRT.anchorMax = new Vector2(1, 0.5f);
            pipsRT.pivot = new Vector2(1, 0.5f);
            pipsRT.anchoredPosition = new Vector2(-UiTheme.Space3, 0);
            pipsRT.sizeDelta = new Vector2(config.keyMaxDifficulty * 32f, 32f);

            var btn = row.GetComponent<Button>();
            if (preview.Valid)
            {
                btn.onClick.AddListener(() => onPick(partner, preview));
            }
            else
            {
                btn.interactable = false;
                row.AddComponent<CanvasGroup>().alpha = 0.45f;
            }
        }

        private static void RebuildPreview(Transform host, GameManager gm,
            Starquill.Data.EconomyConfig config, KeyInstance baseKey, KeyInstance partner,
            KeyFusionPreview preview, Action onFused, Func<BottomSheet> getSheet)
        {
            for (int i = host.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(host.GetChild(i).gameObject);

            var label = UiFactory.Text(host, "RESULT", UiFactory.TextStyle.CaptionDim);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 4f;

            var title = UiFactory.Text(host, preview.ResultTitle, UiFactory.TextStyle.Body);
            title.fontStyle = FontStyles.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 8f;

            var pipsRow = new GameObject("ResultPips", typeof(RectTransform));
            pipsRow.transform.SetParent(host, false);
            pipsRow.AddComponent<LayoutElement>().preferredHeight = 32f;
            var pips = UiFactory.PipRow(pipsRow.transform,
                preview.ResultDifficulty, config.keyMaxDifficulty, 28f);
            var pipsRT = pips.GetComponent<RectTransform>();
            pipsRT.anchorMin = new Vector2(0, 0);
            pipsRT.anchorMax = new Vector2(0, 1);
            pipsRT.pivot = new Vector2(0, 0.5f);

            var cost = UiFactory.Text(host,
                UiFactory.ColorTag("Cost", UiTheme.TextDim) + "   " + preview.CostText,
                UiFactory.TextStyle.Body);
            cost.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 8f;

            var confirm = UiFactory.Button(host, "CONFIRM", UiFactory.ButtonKind.Primary, null);
            confirm.Button.onClick.AddListener(() =>
            {
                if (gm.FuseKeys(baseKey, partner))
                {
                    // Both inputs are consumed; the pouch rebuild (OnKeysChanged)
                    // shows the fused key, so just close the whole flow.
                    getSheet()?.Close();
                    onFused?.Invoke();
                }
                else
                {
                    confirm.SetEnabled(false, $"Need {preview.CostText}");
                }
            });

            // Live gold gate: gold accrues every combat tick, so a snapshot
            // taken at preview time goes stale. Re-evaluate on every gold
            // change while this preview's button is alive; the handler
            // detaches when the sheet closes and self-detaches once the
            // button is destroyed (a newer preview replaced it).
            void UpdateGate() => confirm.SetEnabled(gm.gold >= preview.Cost, $"Need {preview.CostText}");
            Action<double> goldGate = null;
            goldGate = _ =>
            {
                if (confirm.Button == null) { gm.OnGoldChanged -= goldGate; return; }
                UpdateGate();
            };
            gm.OnGoldChanged += goldGate;
            var sheet = getSheet();
            if (sheet != null) sheet.OnClosed += () => gm.OnGoldChanged -= goldGate;
            UpdateGate();
        }
    }
}
