using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Starquill.UI
{
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float floatDistance = 100f;
        [SerializeField] private float duration = 1f;

        private readonly List<TMP_Text> pool = new();
        private int nextIndex;

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var obj = new GameObject($"DmgNum_{i}", typeof(RectTransform));
                obj.transform.SetParent(transform, false);
                var tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 28;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                obj.SetActive(false);
                pool.Add(tmp);
            }
        }

        public void SpawnDamage(Vector2 position, float damage, Color color, bool isVerbHit = false)
        {
            if (!gameObject.activeInHierarchy) return;
            var tmp = pool[nextIndex];
            nextIndex = (nextIndex + 1) % poolSize;

            tmp.text = ((int)damage).ToString();
            tmp.color = color;
            tmp.fontSize = isVerbHit ? 32 : 24;

            var rt = tmp.GetComponent<RectTransform>();
            rt.anchoredPosition = position;

            tmp.gameObject.SetActive(true);
            StartCoroutine(AnimateFloat(tmp, position));
        }

        public void SpawnGold(Vector2 position, double amount)
        {
            if (!gameObject.activeInHierarchy) return;
            var tmp = pool[nextIndex];
            nextIndex = (nextIndex + 1) % poolSize;

            tmp.text = NumberFormatter.FormatGoldDrop(amount);
            tmp.color = new Color(1f, 0.84f, 0f);
            tmp.fontSize = 28;

            var rt = tmp.GetComponent<RectTransform>();
            rt.anchoredPosition = position;

            tmp.gameObject.SetActive(true);
            StartCoroutine(AnimateFloat(tmp, position));
        }

        private IEnumerator AnimateFloat(TMP_Text tmp, Vector2 startPos)
        {
            float elapsed = 0f;
            var rt = tmp.GetComponent<RectTransform>();
            Color startColor = tmp.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = startPos + Vector2.up * (floatDistance * t);
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            tmp.gameObject.SetActive(false);
        }
    }
}
