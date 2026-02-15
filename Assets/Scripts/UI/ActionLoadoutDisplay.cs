using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Data;

namespace Starquill.UI
{
    public class ActionLoadoutDisplay : MonoBehaviour
    {
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private Transform availableContainer;

        public event Action<VerbDefinition> OnAvailableTapped;
        public event Action<int> OnEquippedTapped;

        public void Refresh(CharacterInstance character)
        {
            if (character == null) return;

            RefreshEquipped(character);
            RefreshAvailable(character);
        }

        private void RefreshEquipped(CharacterInstance character)
        {
            if (equippedContainer == null) return;

            for (int i = equippedContainer.childCount - 1; i >= 0; i--)
                Destroy(equippedContainer.GetChild(i).gameObject);

            int slotCount = character.GetVerbSlotCount();
            for (int i = 0; i < slotCount; i++)
            {
                VerbDefinition verb = i < character.equippedVerbs.Count ? character.equippedVerbs[i] : null;
                var card = CreateActionCard(verb, i, true, i >= character.equippedVerbs.Count);
                card.transform.SetParent(equippedContainer, false);
            }

            for (int i = slotCount; i < 5; i++)
            {
                var locked = CreateLockedSlot(i);
                locked.transform.SetParent(equippedContainer, false);
            }
        }

        private void RefreshAvailable(CharacterInstance character)
        {
            if (availableContainer == null) return;

            for (int i = availableContainer.childCount - 1; i >= 0; i--)
                Destroy(availableContainer.GetChild(i).gameObject);

            foreach (var verb in character.unlockedVerbs)
            {
                if (character.equippedVerbs.Contains(verb)) continue;
                var card = CreateActionCard(verb, -1, false, false);
                card.transform.SetParent(availableContainer, false);
            }
        }

        private GameObject CreateActionCard(VerbDefinition verb, int slotIndex, bool isEquipped, bool isEmpty)
        {
            var card = new GameObject($"Action_{(verb != null ? verb.verbId : "empty")}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 80);

            var img = card.GetComponent<Image>();
            if (verb != null)
                img.color = StatTypeColors.GetColor(verb.statType);
            else
                img.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);

            var btn = card.GetComponent<Button>();
            if (isEquipped)
            {
                int idx = slotIndex;
                btn.onClick.AddListener(() => OnEquippedTapped?.Invoke(idx));
            }
            else if (verb != null)
            {
                var v = verb;
                btn.onClick.AddListener(() => OnAvailableTapped?.Invoke(v));
            }

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(card.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8, 4);
            labelRT.offsetMax = new Vector2(-8, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = verb != null ? verb.displayName : (isEmpty ? "+" : "");
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return card;
        }

        private GameObject CreateLockedSlot(int slotIndex)
        {
            int unlockLevel = slotIndex switch { 2 => 11, 3 => 26, 4 => 51, _ => 0 };
            var card = new GameObject($"LockedSlot_{slotIndex}", typeof(RectTransform), typeof(Image));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 80);
            card.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.3f);

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(card.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"Lv {unlockLevel}";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.4f, 0.4f);

            return card;
        }
    }
}
