using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class BottomNavDisplay : MonoBehaviour
    {
        [SerializeField] private Button[] navButtons;
        [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private ScreenManager screenManager;

        private int activeTab;

        public void SetScreenManager(ScreenManager manager)
        {
            screenManager = manager;
        }

        public void SetActiveTab(int index)
        {
            activeTab = index;
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;
                var colors = navButtons[i].colors;
                colors.normalColor = i == index ? activeColor : inactiveColor;
                navButtons[i].colors = colors;

                var label = navButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.color = i == index ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public void CreatePlaceholderButtons()
        {
            var tabNames = new[] { "Explore", "Quests", "Loot", "Party", "Shop" };
            navButtons = new Button[tabNames.Length];

            for (int i = 0; i < tabNames.Length; i++)
            {
                var btnObj = new GameObject($"NavBtn_{tabNames[i]}");
                btnObj.transform.SetParent(transform, false);

                var rt = btnObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 100);

                var img = btnObj.AddComponent<Image>();
                img.color = inactiveColor;

                var btn = btnObj.AddComponent<Button>();
                navButtons[i] = btn;

                int tabIndex = i;
                btn.onClick.AddListener(() => OnNavButtonClicked(tabIndex));

                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(btnObj.transform, false);
                var labelRT = labelObj.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = Vector2.zero;
                labelRT.offsetMax = Vector2.zero;
                var tmp = labelObj.AddComponent<TextMeshProUGUI>();
                tmp.text = tabNames[i];
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }

        private void OnNavButtonClicked(int index)
        {
            SetActiveTab(index);
            if (screenManager != null)
                screenManager.ShowScreen(index);
        }
    }
}
