using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Stacked loot-drop announcements on the explore screen.
    /// Rows fade out after a few seconds; oldest is evicted beyond MaxRows.
    public class LootToastFeed : MonoBehaviour
    {
        private const int MaxRows = 3;
        private const float RowHeight = 72f;
        private const float VisibleSeconds = 2.5f;
        private const float FadeSeconds = 0.6f;

        private readonly List<GameObject> rows = new();
        private bool subscribed;

        private IEnumerator Start()
        {
            yield return null; // wait for GameManager.Start()
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLootDropped += HandleLootDropped;
                subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (subscribed && GameManager.Instance != null)
                GameManager.Instance.OnLootDropped -= HandleLootDropped;
        }

        private void HandleLootDropped(EquipmentInstance item)
        {
            if (item == null) return;
            // Loot drops regardless of which screen is showing; toasts can't
            // run (StartCoroutine errors) while the explore panel is inactive.
            if (!gameObject.activeInHierarchy) return;

            while (rows.Count >= MaxRows)
            {
                var oldest = rows[0];
                rows.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }

            var data = ItemDisplayData.FromItem(item, 1);
            var row = new GameObject($"Toast_{data.DisplayName}",
                typeof(RectTransform), typeof(CanvasGroup));
            row.transform.SetParent(transform, false);

            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, RowHeight);

            var tmp = row.AddComponent<TextMeshProUGUI>();
            string hex = ColorUtility.ToHtmlStringRGB(data.RarityColor);
            tmp.text = $"<color=#{hex}>+ {data.DisplayName}</color>  <size=28><color=#9999A0>{ItemDisplayData.StatLabel(data.PrimaryStat, data.PrimaryValue)}</color></size>";
            tmp.fontSize = 34;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            rows.Add(row);
            Restack();
            StartCoroutine(FadeAndRemove(row));
        }

        private void Restack()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null) continue;
                var rt = rows[i].GetComponent<RectTransform>();
                // newest at bottom, older rows pushed up
                rt.anchoredPosition = new Vector2(0, (rows.Count - 1 - i) * RowHeight);
            }
        }

        private IEnumerator FadeAndRemove(GameObject row)
        {
            yield return new WaitForSeconds(VisibleSeconds);
            if (row == null) yield break;

            var group = row.GetComponent<CanvasGroup>();
            float elapsed = 0f;
            while (elapsed < FadeSeconds && row != null)
            {
                elapsed += Time.deltaTime;
                if (group != null) group.alpha = 1f - (elapsed / FadeSeconds);
                yield return null;
            }

            if (row != null)
            {
                rows.Remove(row);
                Destroy(row);
                Restack();
            }
        }
    }
}
