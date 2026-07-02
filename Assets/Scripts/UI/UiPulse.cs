using UnityEngine;

namespace Starquill.UI
{
    /// Gentle alpha ping-pong for attention (banner pulses). Requires a
    /// CanvasGroup on the same GameObject.
    public class UiPulse : MonoBehaviour
    {
        private CanvasGroup group;
        private bool pulsing;

        public void SetPulsing(bool value)
        {
            pulsing = value;
            if (group == null) group = GetComponent<CanvasGroup>();
            if (!pulsing && group != null) group.alpha = 1f;
        }

        private void Update()
        {
            if (!pulsing) return;
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 0.7f + 0.3f * Mathf.PingPong(Time.time * 1.6f, 1f);
        }
    }
}
