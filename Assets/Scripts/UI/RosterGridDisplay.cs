using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Display;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class RosterGridDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Color partyBadgeColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color selectedBorderColor = new Color(1f, 0.9f, 0.3f);

        private DisplayDataRegistry registry;
        private DisplayBuilder builder;
        private readonly List<CharacterPortraitRenderer> renderers = new();

        public event Action<int> OnCharacterTapped;

        public void Initialize(DisplayDataRegistry reg, DisplayBuilder bld)
        {
            registry = reg;
            builder = bld;
        }

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
            var rawImg = portraitObj.GetComponent<RawImage>();
            rawImg.color = Color.white;

            RenderPortrait(rosterIndex, character, rawImg);

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

        private void RenderPortrait(int index, CharacterInstance character, RawImage target)
        {
            if (registry == null || builder == null) return;
            if (!registry.Species.TryGetValue(character.speciesId, out var speciesData)) return;

            while (renderers.Count <= index)
            {
                var obj = new GameObject($"RosterPortrait_{renderers.Count}");
                obj.transform.SetParent(transform);
                var r = obj.AddComponent<CharacterPortraitRenderer>();
                r.Initialize(new ImageResolver(), 150);
                renderers.Add(r);
            }

            renderers[index].SetHeadCrop(speciesData.HeadYOffset, speciesData.HeadZoom);

            var instance = character.GetOrCreateAppearance(speciesData, registry);
            var equipList = new List<EquipmentDisplayInfo>();
            foreach (var eq in character.equipment)
            {
                if (eq == null) continue;
                equipList.Add(new EquipmentDisplayInfo
                {
                    ItemType = eq.ItemType, ItemNum = eq.ItemNum,
                    BaseColor = eq.BaseColor,
                    VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                        ?? new Dictionary<int, Color>(eq.VarianceColors),
                    IsOffhand = eq.Slot == EquipmentSlot.OffHand,
                    LayerVariants = eq.LayerVariants
                });
            }

            renderers[index].RebuildFromData(instance, speciesData, equipList, builder);
            target.texture = renderers[index].Texture;
        }

        private void OnDestroy()
        {
            foreach (var r in renderers)
                if (r != null) Destroy(r.gameObject);
            renderers.Clear();
        }
    }
}
