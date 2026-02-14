using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class VerbCardAnimator : MonoBehaviour
    {
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private float slideOffset = 400f;

        public void AnimateCardIn(RectTransform card, int targetIndex, float cardWidth, float spacing, Action onComplete = null)
        {
            float targetX = targetIndex * (cardWidth + spacing);
            float startX = targetX + slideOffset;
            card.anchoredPosition = new Vector2(startX, card.anchoredPosition.y);
            StartCoroutine(SlideToPosition(card, targetX, onComplete));
        }

        public void AnimateSlideLeft(RectTransform card, int targetIndex, float cardWidth, float spacing, Action onComplete = null)
        {
            float targetX = targetIndex * (cardWidth + spacing);
            StartCoroutine(SlideToPosition(card, targetX, onComplete));
        }

        public void AnimateCardOut(RectTransform card, Action onComplete = null)
        {
            StartCoroutine(FadeOut(card, onComplete));
        }

        private IEnumerator SlideToPosition(RectTransform card, float targetX, Action onComplete)
        {
            float elapsed = 0f;
            float startX = card.anchoredPosition.x;
            float startY = card.anchoredPosition.y;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / slideDuration);
                card.anchoredPosition = new Vector2(
                    Mathf.Lerp(startX, targetX, t), startY);
                yield return null;
            }

            card.anchoredPosition = new Vector2(targetX, startY);
            onComplete?.Invoke();
        }

        private IEnumerator FadeOut(RectTransform card, Action onComplete)
        {
            var images = card.GetComponentsInChildren<Graphic>();
            float elapsed = 0f;
            float fadeDuration = slideDuration * 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);
                foreach (var img in images)
                {
                    var c = img.color;
                    c.a = alpha;
                    img.color = c;
                }
                yield return null;
            }

            card.gameObject.SetActive(false);
            foreach (var img in images)
            {
                var c = img.color;
                c.a = 1f;
                img.color = c;
            }
            onComplete?.Invoke();
        }
    }
}
