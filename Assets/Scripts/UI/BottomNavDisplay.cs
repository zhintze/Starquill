using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    /// Bottom navigation: 5 equal targets, accent underline + bright label on the active tab.
    public class BottomNavDisplay : MonoBehaviour
    {
        [SerializeField] private Button[] navButtons;
        [SerializeField] private ScreenManager screenManager;

        private Image[] underlines;
        private TMP_Text[] labels;
        private int activeTab;

        public void SetScreenManager(ScreenManager manager)
        {
            screenManager = manager;
        }

        public void SetActiveTab(int index)
        {
            activeTab = index;
            if (navButtons == null) return;
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (labels != null && labels[i] != null)
                    labels[i].color = i == index ? UiTheme.TextPrimary : UiTheme.TextDim;
                if (underlines != null && underlines[i] != null)
                    underlines[i].color = i == index ? UiTheme.AccentGreen : Color.clear;
            }
        }

        public void CreatePlaceholderButtons()
        {
            var tabNames = new[] { "Explore", "Quests", "Loot", "Party", "Shop" };
            navButtons = new Button[tabNames.Length];
            underlines = new Image[tabNames.Length];
            labels = new TMP_Text[tabNames.Length];

            for (int i = 0; i < tabNames.Length; i++)
            {
                var btnObj = new GameObject($"NavBtn_{tabNames[i]}",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(transform, false);
                btnObj.GetComponent<Image>().color = UiTheme.Card;

                var btn = btnObj.GetComponent<Button>();
                navButtons[i] = btn;
                int tabIndex = i;
                btn.onClick.AddListener(() => OnNavButtonClicked(tabIndex));

                var label = UiFactory.Text(btnObj.transform, tabNames[i],
                    UiFactory.TextStyle.Body, TextAlignmentOptions.Center);
                label.fontSize = 32;
                label.fontStyle = FontStyles.Bold;
                label.color = UiTheme.TextDim;
                UiFactory.StretchFill(label.rectTransform);
                labels[i] = label;

                var underline = new GameObject("Underline", typeof(RectTransform), typeof(Image));
                underline.transform.SetParent(btnObj.transform, false);
                var uRT = underline.GetComponent<RectTransform>();
                uRT.anchorMin = new Vector2(0.15f, 0);
                uRT.anchorMax = new Vector2(0.85f, 0);
                uRT.pivot = new Vector2(0.5f, 0);
                uRT.anchoredPosition = new Vector2(0, UiTheme.Space1);
                uRT.sizeDelta = new Vector2(0, 8);
                underlines[i] = underline.GetComponent<Image>();
                underlines[i].color = Color.clear;
            }

            SetActiveTab(0);
        }

        private void OnNavButtonClicked(int index)
        {
            SetActiveTab(index);
            if (screenManager != null)
                screenManager.ShowScreen(index);
        }
    }
}
