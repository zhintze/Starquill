using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    /// Runtime-constructed modal bottom sheet: scrim (tap to close) + slide-up panel.
    /// General-purpose: item detail now; quest detail / shop confirmations later.
    public class BottomSheet : MonoBehaviour
    {
        private RectTransform panelRT;
        private float panelHeight;
        private Coroutine slideCoroutine;

        public Transform Content { get; private set; }
        public event Action OnClosed;

        public static BottomSheet Show(Transform canvasRoot, Action<Transform> fillContent,
            float heightFraction = 0.55f)
        {
            var root = new GameObject("BottomSheet", typeof(RectTransform));
            root.transform.SetParent(canvasRoot, false);
            var rootRT = root.GetComponent<RectTransform>();
            UiFactory.StretchFill(rootRT);
            root.transform.SetAsLastSibling();

            // Render above everything, including panels with their own sorting
            // overrides (the equipment drawer uses sortingOrder 10). Each open
            // sheet sorts above the ones already up: with equal orders Unity's
            // tie-break is unreliable, and a detail sheet opened from a
            // completion sheet could draw underneath it.
            int openSheets = FindObjectsByType<BottomSheet>(FindObjectsSortMode.None).Length;
            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100 + openSheets;
            root.AddComponent<GraphicRaycaster>();

            var sheet = root.AddComponent<BottomSheet>();

            // Scrim
            var scrim = new GameObject("Scrim", typeof(RectTransform), typeof(Image), typeof(Button));
            scrim.transform.SetParent(root.transform, false);
            UiFactory.StretchFill(scrim.GetComponent<RectTransform>());
            scrim.GetComponent<Image>().color = UiTheme.Scrim;
            scrim.GetComponent<Button>().onClick.AddListener(sheet.Close);

            // Panel
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            sheet.panelRT = panel.GetComponent<RectTransform>();
            sheet.panelRT.anchorMin = new Vector2(0, 0);
            sheet.panelRT.anchorMax = new Vector2(1, heightFraction);
            sheet.panelRT.offsetMin = Vector2.zero;
            sheet.panelRT.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = UiTheme.Sheet;

            // Drag handle (visual affordance; tap scrim or handle to close)
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image), typeof(Button));
            handle.transform.SetParent(panel.transform, false);
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.anchorMin = new Vector2(0.5f, 1);
            handleRT.anchorMax = new Vector2(0.5f, 1);
            handleRT.pivot = new Vector2(0.5f, 1);
            handleRT.anchoredPosition = new Vector2(0, -UiTheme.Space2);
            handleRT.sizeDelta = new Vector2(160, 12);
            handle.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
            handle.GetComponent<Button>().onClick.AddListener(sheet.Close);

            // Back button: same size and placement convention as the loot
            // header's Optimize All (top-right, 320x90).
            var back = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(panel.transform, false);
            var backRT = back.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(1, 1);
            backRT.anchorMax = new Vector2(1, 1);
            backRT.pivot = new Vector2(1, 1);
            backRT.anchoredPosition = new Vector2(-UiTheme.Space3, -UiTheme.Space2);
            backRT.sizeDelta = new Vector2(320, 90);
            back.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f);
            back.GetComponent<Button>().onClick.AddListener(sheet.Close);
            var backLabel = UiFactory.Text(back.transform, "Back",
                UiFactory.TextStyle.Body, TMPro.TextAlignmentOptions.Center);
            backLabel.fontSize = 30;
            backLabel.fontStyle = TMPro.FontStyles.Bold;
            UiFactory.StretchFill(backLabel.rectTransform);

            // Content container
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            UiFactory.StretchFill(contentRT);
            contentRT.offsetMin = new Vector2(UiTheme.Space3, UiTheme.Space3);
            // top inset clears the drag handle + Back button row
            contentRT.offsetMax = new Vector2(-UiTheme.Space3, -(UiTheme.Space2 + 90f + UiTheme.Space1));

            // childControlHeight MUST be true so children's LayoutElement
            // preferred heights drive the stack (false = overlapping rows).
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiTheme.Space2;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            sheet.Content = content.transform;
            fillContent?.Invoke(content.transform);

            sheet.slideCoroutine = sheet.StartCoroutine(sheet.SlideIn());
            return sheet;
        }

        private IEnumerator SlideIn()
        {
            panelHeight = ((RectTransform)transform).rect.height * panelRT.anchorMax.y;
            float duration = 0.22f, elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                panelRT.anchoredPosition = new Vector2(0, Mathf.Lerp(-panelHeight, 0, t));
                yield return null;
            }
            panelRT.anchoredPosition = Vector2.zero;
            slideCoroutine = null;
        }

        public void Close()
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            StartCoroutine(SlideOutAndDestroy());
        }

        private IEnumerator SlideOutAndDestroy()
        {
            float duration = 0.18f, elapsed = 0f;
            Vector2 start = panelRT.anchoredPosition;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                panelRT.anchoredPosition = Vector2.Lerp(start, new Vector2(0, -panelHeight), t);
                yield return null;
            }
            OnClosed?.Invoke();
            Destroy(gameObject);
        }
    }
}
