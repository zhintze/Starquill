using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    /// App-wide procedural UI primitives. Every screen composes these;
    /// sizes and colors come exclusively from UiTheme.
    /// Glyph rule: no Unicode symbols in text — pips, arrows, and coins are Images.
    public static class UiFactory
    {
        public enum TextStyle { Display, Heading, Body, Caption, BodySecondary, CaptionDim }
        public enum ButtonKind { Primary, Secondary, Destructive }

        public struct ButtonHandle
        {
            public GameObject Root;
            public Button Button;
            public TMP_Text Label;
            private string baseLabel;

            public ButtonHandle(GameObject root, Button button, TMP_Text label)
            {
                Root = root; Button = button; Label = label; baseLabel = label.text;
            }

            public void SetEnabled(bool enabled, string disabledReason = null)
            {
                Button.interactable = enabled;
                Label.text = !enabled && !string.IsNullOrEmpty(disabledReason)
                    ? disabledReason : baseLabel;
                Label.alpha = enabled ? 1f : 0.6f;
            }

            public void SetLabel(string text)
            {
                baseLabel = text;
                if (Button.interactable) Label.text = text;
            }
        }

        public struct ProgressBarHandle
        {
            public GameObject Root;
            private readonly RectTransform fill;

            public ProgressBarHandle(GameObject root, RectTransform fill)
            {
                Root = root; this.fill = fill;
            }

            public void SetFraction(float f)
            {
                if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(f), 1f);
            }
        }

        // ---------- Text ----------

        public static TMP_Text Text(Transform parent, string text, TextStyle style,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = alignment;
            tmp.richText = true;
            ApplyStyle(tmp, style);
            return tmp;
        }

        public static void ApplyStyle(TMP_Text tmp, TextStyle style)
        {
            switch (style)
            {
                case TextStyle.Display:
                    tmp.fontSize = UiTheme.FontDisplay; tmp.fontStyle = FontStyles.Bold;
                    tmp.color = UiTheme.TextPrimary; break;
                case TextStyle.Heading:
                    tmp.fontSize = UiTheme.FontHeading; tmp.fontStyle = FontStyles.Bold;
                    tmp.color = UiTheme.TextPrimary; break;
                case TextStyle.Body:
                    tmp.fontSize = UiTheme.FontBody; tmp.color = UiTheme.TextPrimary; break;
                case TextStyle.BodySecondary:
                    tmp.fontSize = UiTheme.FontBody; tmp.color = UiTheme.TextSecondary; break;
                case TextStyle.Caption:
                    tmp.fontSize = UiTheme.FontCaption; tmp.color = UiTheme.TextSecondary; break;
                case TextStyle.CaptionDim:
                    tmp.fontSize = UiTheme.FontCaption; tmp.color = UiTheme.TextDim; break;
            }
        }

        // ---------- Buttons ----------

        public static ButtonHandle Button(Transform parent, string label, ButtonKind kind, Action onClick)
        {
            var obj = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            float height = kind == ButtonKind.Primary ? UiTheme.ButtonPrimaryHeight : UiTheme.TouchMin;
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = UiTheme.TouchMin;
            le.flexibleWidth = 1;
            le.flexibleHeight = 0;

            obj.GetComponent<Image>().color = kind switch
            {
                ButtonKind.Primary => UiTheme.AccentGreen,
                ButtonKind.Destructive => new Color(0.55f, 0.2f, 0.2f),
                _ => new Color(0.3f, 0.3f, 0.38f)
            };

            var labelTmp = Text(obj.transform, label, TextStyle.Body, TextAlignmentOptions.Center);
            StretchFill(labelTmp.rectTransform);
            labelTmp.fontStyle = FontStyles.Bold;

            var btn = obj.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return new ButtonHandle(obj, btn, labelTmp);
        }

        // ---------- Chips, pips, frames ----------

        public static GameObject Chip(Transform parent, string text, Color background,
            float width = 0f, float height = 0f)
        {
            var obj = new GameObject($"Chip_{text}", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<Image>().color = background;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(
                width > 0f ? width : UiTheme.DeltaChipWidth,
                height > 0f ? height : UiTheme.DeltaChipHeight);

            var tmp = Text(obj.transform, text, TextStyle.Body, TextAlignmentOptions.Center);
            StretchFill(tmp.rectTransform);
            tmp.fontStyle = FontStyles.Bold;
            return obj;
        }

        public static GameObject PipRow(Transform parent, int level, int max, float pipSize = 20f)
        {
            var row = new GameObject("Pips", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiTheme.Space1;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            for (int i = 0; i < max; i++)
            {
                var pip = new GameObject("Pip", typeof(RectTransform), typeof(Image));
                pip.transform.SetParent(row.transform, false);
                pip.GetComponent<RectTransform>().sizeDelta = new Vector2(pipSize, pipSize);
                pip.GetComponent<Image>().color = i < level
                    ? UiTheme.TextPrimary
                    : new Color(1f, 1f, 1f, 0.25f);
            }
            return row;
        }

        public static RawImage Frame(Transform parent, Color rarityColor, float size = 0f)
        {
            float s = size > 0f ? size : UiTheme.CardIcon;
            var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(parent, false);
            frame.GetComponent<RectTransform>().sizeDelta = new Vector2(s, s);
            frame.GetComponent<Image>().color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.16f);

            // RawImage so callers can crop into the character-sized item canvas
            // via uvRect (see ItemIconFraming).
            var inner = new GameObject("Icon", typeof(RectTransform), typeof(RawImage));
            inner.transform.SetParent(frame.transform, false);
            var innerRT = inner.GetComponent<RectTransform>();
            StretchFill(innerRT);
            innerRT.offsetMin = new Vector2(6, 6);
            innerRT.offsetMax = new Vector2(-6, -6);
            var img = inner.GetComponent<RawImage>();
            img.color = Color.clear;
            return img; // caller assigns texture + uvRect
        }

        // ---------- Bars, tabs, headers ----------

        public static ProgressBarHandle ProgressBar(Transform parent, Color fillColor,
            float height = 24f)
        {
            var bar = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.09f);
            var le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = fillColor;

            return new ProgressBarHandle(bar, fillRT);
        }

        public static GameObject SegmentedTabs(Transform parent, string[] labels, Action<int> onSelected,
            int initialIndex = 0)
        {
            var row = new GameObject("SegmentedTabs", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiTheme.Space1;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.TouchMin;

            var labelTexts = new TMP_Text[labels.Length];
            var underlines = new Image[labels.Length];

            void Select(int idx)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    labelTexts[i].color = i == idx ? UiTheme.TextPrimary : UiTheme.TextDim;
                    underlines[i].color = i == idx ? UiTheme.AccentGreen : Color.clear;
                }
                onSelected?.Invoke(idx);
            }

            for (int i = 0; i < labels.Length; i++)
            {
                var tab = new GameObject($"Tab_{labels[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
                tab.transform.SetParent(row.transform, false);
                tab.GetComponent<Image>().color = UiTheme.Card;

                var tmp = Text(tab.transform, labels[i], TextStyle.Body, TextAlignmentOptions.Center);
                StretchFill(tmp.rectTransform);
                tmp.fontStyle = FontStyles.Bold;
                labelTexts[i] = tmp;

                var underline = new GameObject("Underline", typeof(RectTransform), typeof(Image));
                underline.transform.SetParent(tab.transform, false);
                var uRT = underline.GetComponent<RectTransform>();
                uRT.anchorMin = new Vector2(0.1f, 0f);
                uRT.anchorMax = new Vector2(0.9f, 0f);
                uRT.pivot = new Vector2(0.5f, 0f);
                uRT.sizeDelta = new Vector2(0, 8);
                underlines[i] = underline.GetComponent<Image>();

                int idx = i;
                tab.GetComponent<Button>().onClick.AddListener(() => Select(idx));
            }

            Select(initialIndex);
            return row;
        }

        public static GameObject HeaderBar(Transform parent, string title,
            string actionLabel = null, Action onAction = null)
        {
            var bar = new GameObject("HeaderBar", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            var le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.TouchMin;
            le.flexibleWidth = 1;

            var titleTmp = Text(bar.transform, title, TextStyle.Heading);
            var titleRT = titleTmp.rectTransform;
            titleRT.anchorMin = new Vector2(0, 0);
            titleRT.anchorMax = new Vector2(actionLabel != null ? 0.6f : 1f, 1);
            titleRT.offsetMin = new Vector2(UiTheme.Space3, 0);
            titleRT.offsetMax = Vector2.zero;

            if (actionLabel != null)
            {
                var handle = Button(bar.transform, actionLabel, ButtonKind.Primary, onAction);
                UnityEngine.Object.Destroy(handle.Root.GetComponent<LayoutElement>());
                var btnRT = handle.Root.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.62f, 0.05f);
                btnRT.anchorMax = new Vector2(1f, 0.95f);
                btnRT.offsetMin = Vector2.zero;
                btnRT.offsetMax = new Vector2(-UiTheme.Space3, 0);
            }
            return bar;
        }

        public static GameObject EmptyState(Transform parent, string message)
        {
            var obj = new GameObject("EmptyState", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = UiTheme.CardHeight;
            le.flexibleWidth = 1;
            var tmp = Text(obj.transform, message, TextStyle.CaptionDim, TextAlignmentOptions.Center);
            StretchFill(tmp.rectTransform);
            return obj;
        }

        // ---------- Banner ----------

        public struct BannerHandle
        {
            public GameObject Root;
            private readonly TMP_Text text;
            private readonly Image[] accents;
            private readonly ButtonHandle? action;
            private readonly UiPulse pulse;

            public BannerHandle(GameObject root, TMP_Text text, Image[] accents,
                ButtonHandle? action, UiPulse pulse)
            {
                Root = root; this.text = text; this.accents = accents;
                this.action = action; this.pulse = pulse;
            }

            public void SetText(string value) { if (text != null) text.text = value; }

            public void SetAccent(Color color)
            {
                foreach (var a in accents)
                    if (a != null) a.color = color;
            }

            public void SetVisible(bool visible)
            {
                if (Root != null) Root.SetActive(visible);
                if (!visible && pulse != null) pulse.SetPulsing(false);
            }

            public void SetPulsing(bool pulsing) { if (pulse != null) pulse.SetPulsing(pulsing); }

            public void SetActionVisible(bool visible)
            {
                if (action.HasValue) action.Value.Root.SetActive(visible);
            }
        }

        /// Full-width notice strip: accent borders, Body text, optional
        /// compact action button, tap callback on the strip itself.
        /// Used by quest discovery/retry/wave HUD; reusable for offline
        /// earnings and shop notices.
        public static BannerHandle Banner(Transform parent, string actionLabel = null,
            Action onAction = null, Action onTapped = null)
        {
            var root = new GameObject("Banner", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            StretchFill(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = UiTheme.Card;
            if (onTapped != null)
                root.GetComponent<Button>().onClick.AddListener(() => onTapped());

            var accents = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var edge = new GameObject(i == 0 ? "AccentTop" : "AccentBottom",
                    typeof(RectTransform), typeof(Image));
                edge.transform.SetParent(root.transform, false);
                var rt = edge.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, i == 0 ? 1 : 0);
                rt.anchorMax = new Vector2(1, i == 0 ? 1 : 0);
                rt.pivot = new Vector2(0.5f, i == 0 ? 1 : 0);
                rt.sizeDelta = new Vector2(0, 4);
                accents[i] = edge.GetComponent<Image>();
                accents[i].color = UiTheme.AccentGreen;
            }

            bool hasAction = actionLabel != null;
            var tmp = Text(root.transform, "", TextStyle.Body);
            var textRT = tmp.rectTransform;
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(hasAction ? 0.72f : 1f, 1);
            textRT.offsetMin = new Vector2(UiTheme.Space3, 0);
            textRT.offsetMax = Vector2.zero;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            ButtonHandle? action = null;
            if (hasAction)
            {
                var handle = Button(root.transform, actionLabel, ButtonKind.Secondary, onAction);
                UnityEngine.Object.Destroy(handle.Root.GetComponent<LayoutElement>());
                var btnRT = handle.Root.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.74f, 0.12f);
                btnRT.anchorMax = new Vector2(1f, 0.88f);
                btnRT.offsetMin = Vector2.zero;
                btnRT.offsetMax = new Vector2(-UiTheme.Space2, 0);
                handle.Label.fontSize = UiTheme.FontCaption;
                action = handle;
            }

            var pulse = root.AddComponent<UiPulse>();
            return new BannerHandle(root, tmp, accents, action, pulse);
        }

        // ---------- Ladder ----------

        /// Row of progression nodes (quest ladder). Node visuals are Images
        /// only (glyph rule): Done = accent fill, Current = fill + ring,
        /// Ahead = dim, Boss = gold diamond, BossCurrent = gold diamond + ring.
        public static GameObject LadderRow(Transform parent, LadderNodeState[] states,
            float nodeSize = 64f)
        {
            var row = new GameObject("Ladder", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UiTheme.Space1;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = nodeSize + UiTheme.FontCaption + UiTheme.Space2;

            for (int i = 0; i < states.Length; i++)
            {
                var slot = new GameObject($"Node_{i}", typeof(RectTransform));
                slot.transform.SetParent(row.transform, false);
                slot.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(nodeSize, nodeSize + UiTheme.FontCaption + 6f);

                bool boss = states[i] == LadderNodeState.Boss || states[i] == LadderNodeState.BossCurrent;
                bool current = states[i] == LadderNodeState.Current || states[i] == LadderNodeState.BossCurrent;
                bool done = states[i] == LadderNodeState.Done;

                var node = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                node.transform.SetParent(slot.transform, false);
                var nodeRT = node.GetComponent<RectTransform>();
                nodeRT.anchorMin = new Vector2(0.5f, 1);
                nodeRT.anchorMax = new Vector2(0.5f, 1);
                nodeRT.pivot = new Vector2(0.5f, 1);
                float inner = boss ? nodeSize * 0.72f : nodeSize * 0.8f;
                nodeRT.sizeDelta = new Vector2(inner, inner);
                if (boss) nodeRT.localRotation = Quaternion.Euler(0, 0, 45f);
                node.GetComponent<Image>().color =
                    boss ? (done ? UiTheme.BestGold : new Color(UiTheme.BestGold.r, UiTheme.BestGold.g, UiTheme.BestGold.b, current ? 1f : 0.45f))
                    : done ? UiTheme.AccentGreen
                    : current ? UiTheme.AccentGreen
                    : new Color(1f, 1f, 1f, 0.25f);

                if (current)
                {
                    var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
                    ring.transform.SetParent(slot.transform, false);
                    var ringRT = ring.GetComponent<RectTransform>();
                    ringRT.anchorMin = new Vector2(0.5f, 1);
                    ringRT.anchorMax = new Vector2(0.5f, 1);
                    ringRT.pivot = new Vector2(0.5f, 1);
                    ringRT.sizeDelta = new Vector2(nodeSize, 6);
                    ringRT.anchoredPosition = new Vector2(0, -(inner + 8f));
                    ring.GetComponent<Image>().color = UiTheme.TextPrimary;
                }

                // Tier markers under elite/hard nodes
                string marker = i == 3 || i == 7 ? "E" : (i == 8 || i == 9 ? "H" : null);
                if (marker != null)
                {
                    var tmp = Text(slot.transform, marker, TextStyle.CaptionDim, TextAlignmentOptions.Center);
                    var tmpRT = tmp.rectTransform;
                    tmpRT.anchorMin = new Vector2(0, 0);
                    tmpRT.anchorMax = new Vector2(1, 0);
                    tmpRT.pivot = new Vector2(0.5f, 0);
                    tmpRT.sizeDelta = new Vector2(0, UiTheme.FontCaption + 4f);
                }
            }
            return row;
        }

        // ---------- Helpers ----------

        public static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static string ColorTag(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }
    }
}
