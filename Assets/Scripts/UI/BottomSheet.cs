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

            // Content container
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            UiFactory.StretchFill(contentRT);
            contentRT.offsetMin = new Vector2(UiTheme.Space3, UiTheme.Space3);
            contentRT.offsetMax = new Vector2(-UiTheme.Space3, -(UiTheme.Space2 + 20f));

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiTheme.Space2;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
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
