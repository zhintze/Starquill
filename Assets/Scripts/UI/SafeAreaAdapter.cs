using UnityEngine;

namespace Starquill.UI
{
    /// <summary>
    /// Adjusts this RectTransform's anchors to match Screen.safeArea,
    /// keeping UI away from notches, home indicators, and rounded corners.
    /// Place on a panel between Canvas and all UI content.
    /// A full-screen background behind this panel prevents visible gaps.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAdapter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea)
                ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            var safeArea = Screen.safeArea;
            lastSafeArea = safeArea;

            // Only apply vertical (top/bottom) safe area insets.
            // Horizontal insets from curved edges or side notches cause
            // more layout problems than they solve in portrait mode.
            float yMin = safeArea.y / Screen.height;
            float yMax = (safeArea.y + safeArea.height) / Screen.height;

            rectTransform.anchorMin = new Vector2(0f, yMin);
            rectTransform.anchorMax = new Vector2(1f, yMax);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
