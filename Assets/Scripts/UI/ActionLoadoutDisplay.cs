using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.UI
{
    public class ActionLoadoutDisplay : MonoBehaviour
    {
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private Transform poolSummaryContainer;
        [SerializeField] private TMP_Text drawerBarLabel;
        [SerializeField] private TMP_Text lockIndicatorLabel;
        [SerializeField] private Slider lockProgressBar;
        [SerializeField] private GameObject lockIndicatorObj;

        public event Action<int> OnEquippedSlotTapped;
        public event Action OnEmptySlotTapped;

        private CharacterInstance currentCharacter;

        public void Refresh(CharacterInstance character, CharacterInstance[] party)
        {
            currentCharacter = character;
            if (character == null) return;

            RefreshPoolSummary(party);
            RefreshEquipped(character);
            RefreshLockIndicator(character);
            RefreshDrawerBar(character);
        }

        private void RefreshPoolSummary(CharacterInstance[] party)
        {
            if (poolSummaryContainer == null) return;

            for (int i = poolSummaryContainer.childCount - 1; i >= 0; i--)
                Destroy(poolSummaryContainer.GetChild(i).gameObject);

            if (party == null) return;
            var summary = PoolSummaryData.FromParty(party);

            var statTypes = new[] { StatType.STR, StatType.DEX, StatType.CON, StatType.INT, StatType.WIS, StatType.CHA };
            foreach (var stat in statTypes)
            {
                int count = summary.GetCount(stat);
                bool isGap = summary.HasGap(stat);

                var label = new GameObject($"Pool_{stat}", typeof(RectTransform));
                label.transform.SetParent(poolSummaryContainer, false);
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{stat}: {count}";
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = isGap ? new Color(0.9f, 0.3f, 0.3f) : StatTypeColors.GetColor(stat);
            }
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
                if (verb != null)
                {
                    var card = CreateVerbCard(verb, i);
                    card.transform.SetParent(equippedContainer, false);
                }
                else
                {
                    var empty = CreateEmptySlotCard();
                    empty.transform.SetParent(equippedContainer, false);
                }
            }
        }

        private void RefreshLockIndicator(CharacterInstance character)
        {
            if (lockIndicatorObj == null) return;

            int currentSlots = character.GetVerbSlotCount();
            if (currentSlots >= 5)
            {
                lockIndicatorObj.SetActive(false);
                return;
            }

            lockIndicatorObj.SetActive(true);

            int nextUnlockLevel = currentSlots switch
            {
                2 => 11,
                3 => 26,
                4 => 51,
                _ => 999
            };

            int prevUnlockLevel = currentSlots switch
            {
                2 => 1,
                3 => 11,
                4 => 26,
                _ => 51
            };

            if (lockIndicatorLabel != null)
                lockIndicatorLabel.text = $"{currentSlots}/5 slots \u00B7 Next at Lv {nextUnlockLevel}";

            if (lockProgressBar != null)
            {
                float progress = (float)(character.level - prevUnlockLevel) / (nextUnlockLevel - prevUnlockLevel);
                lockProgressBar.value = Mathf.Clamp01(progress);
            }
        }

        private void RefreshDrawerBar(CharacterInstance character)
        {
            if (drawerBarLabel == null) return;
            int available = character.unlockedVerbs.Count - character.equippedVerbs.Count;
            drawerBarLabel.text = $"{available} verb{(available != 1 ? "s" : "")} available";
        }

        private GameObject CreateVerbCard(VerbDefinition verb, int slotIndex)
        {
            var data = ActionCardData.FromVerb(verb);

            var card = new GameObject($"VerbCard_{verb.verbId}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            le.flexibleWidth = 1;

            var img = card.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f);

            // Stat-type color stripe (left border)
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(card.transform, false);
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0, 0);
            stripeRT.anchorMax = new Vector2(0, 1);
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(6, 0);
            stripeRT.anchoredPosition = Vector2.zero;
            stripe.GetComponent<Image>().color = StatTypeColors.GetColor(verb.statType);

            // Verb name (top-left)
            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.5f);
            nameRT.anchorMax = new Vector2(0.55f, 1);
            nameRT.offsetMin = new Vector2(14, 2);
            nameRT.offsetMax = new Vector2(0, -4);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = data.DisplayName;
            nameTmp.fontSize = 20;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = Color.white;

            // Stat type + target mode (bottom-left)
            var infoObj = new GameObject("Info", typeof(RectTransform));
            infoObj.transform.SetParent(card.transform, false);
            var infoRT = infoObj.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0, 0);
            infoRT.anchorMax = new Vector2(0.55f, 0.5f);
            infoRT.offsetMin = new Vector2(14, 4);
            infoRT.offsetMax = new Vector2(0, -2);
            var infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
            infoTmp.text = $"{data.StatAbbreviation} \u00B7 {data.TargetModeText}";
            infoTmp.fontSize = 14;
            infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
            infoTmp.color = new Color(0.6f, 0.6f, 0.65f);

            // Damage + cooldown (right side)
            var statsObj = new GameObject("Stats", typeof(RectTransform));
            statsObj.transform.SetParent(card.transform, false);
            var statsRT = statsObj.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.55f, 0);
            statsRT.anchorMax = new Vector2(1, 1);
            statsRT.offsetMin = new Vector2(0, 4);
            statsRT.offsetMax = new Vector2(-10, -4);
            var statsTmp = statsObj.AddComponent<TextMeshProUGUI>();
            string dmgPrefix = data.IsHealing ? "heal" : "dmg";
            statsTmp.text = $"{data.DamageText} {dmgPrefix}  \u23F1 {data.CooldownText}";
            statsTmp.fontSize = 16;
            statsTmp.alignment = TextAlignmentOptions.MidlineRight;
            statsTmp.color = new Color(0.8f, 0.8f, 0.85f);

            // Button handler
            var btn = card.GetComponent<Button>();
            int idx = slotIndex;
            btn.onClick.AddListener(() => OnEquippedSlotTapped?.Invoke(idx));

            return card;
        }

        private GameObject CreateEmptySlotCard()
        {
            var card = new GameObject("EmptySlot", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            le.flexibleWidth = 1;

            var img = card.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.15f, 0.5f);

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(card.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8, 4);
            labelRT.offsetMax = new Vector2(-8, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "+ Empty Slot";
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.5f, 0.5f, 0.55f);
            tmp.fontStyle = FontStyles.Italic;

            var btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => OnEmptySlotTapped?.Invoke());

            return card;
        }
    }
}
