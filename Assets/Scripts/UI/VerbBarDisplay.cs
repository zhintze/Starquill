using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class VerbBarDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Vector2 cardSize = new Vector2(300, 100);
        [SerializeField] private int maxCards = 6;

        private readonly List<GameObject> cards = new();

        public void PopulateWithPlaceholders()
        {
            ClearCards();

            var placeholders = new[]
            {
                ("Bash", new Color(0.9f, 0.5f, 0.1f)),
                ("Analyze", new Color(0.2f, 0.5f, 0.9f)),
                ("Slash", new Color(0.7f, 0.7f, 0.7f)),
                ("Heal", new Color(0.2f, 0.8f, 0.4f)),
                ("Shield", new Color(0.6f, 0.4f, 0.2f))
            };

            for (int i = 0; i < placeholders.Length && i < maxCards; i++)
            {
                var (verbName, color) = placeholders[i];
                CreateCard(verbName, color, $"{3 + i * 2}s left");
            }
        }

        private void CreateCard(string verbName, Color bgColor, string cooldownText)
        {
            var parent = cardContainer != null ? cardContainer : transform;

            var card = new GameObject($"VerbCard_{cards.Count}");
            card.transform.SetParent(parent, false);

            var cardRT = card.AddComponent<RectTransform>();
            cardRT.sizeDelta = cardSize;

            var bg = card.AddComponent<Image>();
            bg.color = bgColor;

            var nameObj = new GameObject("VerbName");
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.4f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = verbName;
            nameTMP.fontSize = 28;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.white;

            var cdObj = new GameObject("Cooldown");
            cdObj.transform.SetParent(card.transform, false);
            var cdRT = cdObj.AddComponent<RectTransform>();
            cdRT.anchorMin = new Vector2(0, 0);
            cdRT.anchorMax = new Vector2(1, 0.4f);
            cdRT.offsetMin = Vector2.zero;
            cdRT.offsetMax = Vector2.zero;
            var cdTMP = cdObj.AddComponent<TextMeshProUGUI>();
            cdTMP.text = cooldownText;
            cdTMP.fontSize = 18;
            cdTMP.alignment = TextAlignmentOptions.Center;
            cdTMP.color = new Color(1, 1, 1, 0.7f);

            cards.Add(card);
        }

        private void ClearCards()
        {
            foreach (var card in cards)
                if (card != null) Destroy(card);
            cards.Clear();
        }
    }
}
