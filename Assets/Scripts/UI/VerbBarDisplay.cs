using System;
using System.Collections.Generic;
using TMPro;
using Starquill.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class VerbBarDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Vector2 cardSize = new Vector2(300, 100);
        [SerializeField] private int maxCards = 3;

        private readonly List<GameObject> cards = new();
        private bool locked;

        public event Action<int> OnCardTapped;

        public void SetLocked(bool isLocked)
        {
            locked = isLocked;
            UpdateLockVisual();
        }

        public void RebuildFromSlots(IReadOnlyList<DrawnVerb> slots)
        {
            ClearCards();
            for (int i = 0; i < slots.Count && i < maxCards; i++)
            {
                var verb = slots[i];
                var color = StatTypeColors.GetColor(verb.Verb.statType);
                CreateCard(verb.Verb.displayName, color, i);
            }
        }

        public void PopulateWithPlaceholders()
        {
            ClearCards();

            var placeholders = new[]
            {
                ("Bash", new Color(0.9f, 0.5f, 0.1f)),
                ("Analyze", new Color(0.2f, 0.5f, 0.9f)),
                ("Slash", new Color(0.7f, 0.7f, 0.7f)),
            };

            for (int i = 0; i < placeholders.Length && i < maxCards; i++)
            {
                var (verbName, color) = placeholders[i];
                CreateCard(verbName, color, i);
            }
        }

        private void CreateCard(string verbName, Color bgColor, int index)
        {
            var parent = cardContainer != null ? cardContainer : transform;

            var card = new GameObject($"VerbCard_{cards.Count}");
            card.transform.SetParent(parent, false);

            var cardRT = card.AddComponent<RectTransform>();
            cardRT.sizeDelta = cardSize;

            var bg = card.AddComponent<Image>();
            bg.color = locked ? Color.Lerp(bgColor, Color.gray, 0.5f) : bgColor;

            var btn = card.AddComponent<Button>();
            int capturedIndex = index;
            btn.onClick.AddListener(() =>
            {
                if (!locked) OnCardTapped?.Invoke(capturedIndex);
            });

            var nameObj = new GameObject("VerbName");
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.3f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = verbName;
            nameTMP.fontSize = 28;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.white;
            nameTMP.raycastTarget = false;

            cards.Add(card);
        }

        private void UpdateLockVisual()
        {
            foreach (var card in cards)
            {
                if (card == null) continue;
                var img = card.GetComponent<Image>();
                if (img == null) continue;
                float gray = locked ? 0.5f : 0f;
                img.color = Color.Lerp(img.color, Color.gray, gray);
            }
        }

        private void ClearCards()
        {
            foreach (var card in cards)
                if (card != null) Destroy(card);
            cards.Clear();
        }
    }
}
