using System.Collections.Generic;
using Starquill.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class EnemyDisplayController : MonoBehaviour
    {
        [SerializeField] private Image[] silhouettes;

        private readonly List<Image> hpBarFills = new();
        private readonly List<Image> hpBarBgs = new();
        private readonly List<float> displayedHP = new();
        private IReadOnlyList<EnemyState> enemies;

        public void SetupEnemies(IReadOnlyList<EnemyState> newEnemies)
        {
            ClearHPBars();
            enemies = newEnemies;

            int count = Mathf.Min(newEnemies.Count, silhouettes.Length);
            for (int i = 0; i < silhouettes.Length; i++)
            {
                if (i < count)
                {
                    silhouettes[i].gameObject.SetActive(true);
                    var enemy = newEnemies[i];

                    // Tint silhouette with stat type color
                    var statColor = StatTypeColors.GetColor(enemy.StatType);
                    silhouettes[i].color = new Color(
                        statColor.r * 0.4f + 0.15f,
                        statColor.g * 0.4f + 0.15f,
                        statColor.b * 0.4f + 0.15f,
                        0.8f);

                    // Create HP bar
                    CreateHPBar(silhouettes[i].transform, statColor, i);
                    displayedHP.Add(enemy.MaxHP);
                }
                else
                {
                    silhouettes[i].gameObject.SetActive(false);
                }
            }
        }

        private void CreateHPBar(Transform parent, Color fillColor, int index)
        {
            // Background
            var bgObj = new GameObject($"HPBarBg_{index}", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(parent, false);
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.1f, 0f);
            bgRT.anchorMax = new Vector2(0.9f, 0f);
            bgRT.pivot = new Vector2(0.5f, 1f);
            bgRT.anchoredPosition = new Vector2(0, -5);
            bgRT.sizeDelta = new Vector2(0, 12);
            bgObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            hpBarBgs.Add(bgObj.GetComponent<Image>());

            // Fill
            var fillObj = new GameObject($"HPBarFill_{index}", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            var fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillRT.pivot = new Vector2(0f, 0.5f);
            var fillImg = fillObj.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            hpBarFills.Add(fillImg);
        }

        private void Update()
        {
            if (enemies == null) return;
            int count = Mathf.Min(enemies.Count, hpBarFills.Count);
            for (int i = 0; i < count; i++)
            {
                var enemy = enemies[i];
                float targetFill = enemy.MaxHP > 0 ? enemy.CurrentHP / enemy.MaxHP : 0f;
                hpBarFills[i].fillAmount = Mathf.Lerp(hpBarFills[i].fillAmount, targetFill, Time.deltaTime * 8f);

                // Fade on death
                if (!enemy.IsAlive && silhouettes[i].color.a > 0.01f)
                {
                    var c = silhouettes[i].color;
                    c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 4f);
                    silhouettes[i].color = c;

                    var bgC = hpBarBgs[i].color;
                    bgC.a = c.a;
                    hpBarBgs[i].color = bgC;

                    var fillC = hpBarFills[i].color;
                    fillC.a = c.a;
                    hpBarFills[i].color = fillC;
                }
            }
        }

        public Vector2 GetEnemyPosition(int index)
        {
            if (index < 0 || index >= silhouettes.Length) return Vector2.zero;
            return silhouettes[index].rectTransform.anchoredPosition;
        }

        private void ClearHPBars()
        {
            foreach (var bg in hpBarBgs)
                if (bg != null) Destroy(bg.gameObject);
            hpBarBgs.Clear();
            hpBarFills.Clear();
            displayedHP.Clear();
            enemies = null;
        }
    }
}
