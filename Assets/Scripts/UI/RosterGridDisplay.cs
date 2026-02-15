using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;

namespace Starquill.UI
{
    public class RosterGridDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Color partyBadgeColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color selectedBorderColor = new Color(1f, 0.9f, 0.3f);

        public event Action<int> OnCharacterTapped;

        public void Refresh(CharacterRoster roster, int selectedIndex)
        {
            if (cardContainer == null) return;

            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);

            for (int i = 0; i < roster.Characters.Count; i++)
            {
                var character = roster.Characters[i];
                var card = CreateCard(character, i, roster.IsInParty(i),
                    roster.GetPartySlot(i), i == selectedIndex);
                card.transform.SetParent(cardContainer, false);
            }
        }

        private GameObject CreateCard(CharacterInstance character, int rosterIndex,
            bool inParty, int partySlot, bool isSelected)
        {
            var card = new GameObject($"Card_{character.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 200);

            var img = card.GetComponent<Image>();
            img.color = isSelected ? selectedBorderColor : new Color(0.2f, 0.2f, 0.25f);

            var btn = card.GetComponent<Button>();
            int idx = rosterIndex;
            btn.onClick.AddListener(() => OnCharacterTapped?.Invoke(idx));

            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0);
            nameRT.anchorMax = new Vector2(1, 0.2f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = $"{character.displayName}\nLv {character.level}";
            nameTmp.fontSize = 16;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;

            var portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(RawImage));
            portraitObj.transform.SetParent(card.transform, false);
            var portraitRT = portraitObj.GetComponent<RectTransform>();
            portraitRT.anchorMin = new Vector2(0.1f, 0.25f);
            portraitRT.anchorMax = new Vector2(0.9f, 0.95f);
            portraitRT.offsetMin = Vector2.zero;
            portraitRT.offsetMax = Vector2.zero;
            portraitObj.GetComponent<RawImage>().color = new Color(0.3f, 0.3f, 0.35f);

            if (inParty && partySlot >= 0)
            {
                var badgeObj = new GameObject("Badge", typeof(RectTransform));
                badgeObj.transform.SetParent(card.transform, false);
                var badgeRT = badgeObj.GetComponent<RectTransform>();
                badgeRT.anchorMin = new Vector2(0.7f, 0.8f);
                badgeRT.anchorMax = new Vector2(1f, 1f);
                badgeRT.offsetMin = Vector2.zero;
                badgeRT.offsetMax = Vector2.zero;
                var badgeTmp = badgeObj.AddComponent<TextMeshProUGUI>();
                badgeTmp.text = $"{partySlot + 1}";
                badgeTmp.fontSize = 20;
                badgeTmp.fontStyle = FontStyles.Bold;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = partyBadgeColor;
            }

            return card;
        }
    }
}
