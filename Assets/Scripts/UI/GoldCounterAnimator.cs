using TMPro;
using UnityEngine;

namespace Starquill.UI
{
    public class GoldCounterAnimator : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float lerpSpeed = 4f;

        private double displayedValue;
        private double targetValue;

        public void SetTarget(double value)
        {
            targetValue = value;
        }

        public void SetImmediate(double value)
        {
            targetValue = value;
            displayedValue = value;
            UpdateLabel();
        }

        private void Update()
        {
            if (System.Math.Abs(displayedValue - targetValue) < 0.5)
            {
                displayedValue = targetValue;
            }
            else
            {
                displayedValue = displayedValue + (targetValue - displayedValue) * lerpSpeed * Time.deltaTime;
            }
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label != null)
                label.text = NumberFormatter.FormatCompact(displayedValue) + " Gold";
        }
    }
}
