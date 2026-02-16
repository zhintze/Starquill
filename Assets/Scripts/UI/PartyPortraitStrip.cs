using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Display;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class PartyPortraitStrip : MonoBehaviour
    {
        [SerializeField] private RawImage[] portraits;
        [SerializeField] private Image[] highlightRings;
        [SerializeField] private TMP_Text[] nameLabels;
        [SerializeField] private TMP_Text[] levelLabels;
        [SerializeField] private Button[] buttons;
        [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color defaultRingColor = new Color(0.3f, 0.3f, 0.3f);

        private readonly List<CharacterPortraitRenderer> renderers = new();

        public event Action<int> OnPortraitTapped;

        public void Initialize()
        {
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) continue;
                    int idx = i;
                    buttons[i].onClick.AddListener(() => OnPortraitTapped?.Invoke(idx));
                }
            }
        }

        public void Refresh(CharacterRoster roster, int selectedRosterIndex,
            DisplayDataRegistry registry, DisplayBuilder builder)
        {
            var party = roster.GetActiveParty();

            for (int i = 0; i < 4; i++)
            {
                var character = i < party.Length ? party[i] : null;
                bool isSelected = character != null &&
                    roster.ActivePartyIndices[i] == selectedRosterIndex;

                if (highlightRings != null && i < highlightRings.Length && highlightRings[i] != null)
                    highlightRings[i].color = isSelected ? highlightColor : defaultRingColor;

                if (nameLabels != null && i < nameLabels.Length && nameLabels[i] != null)
                    nameLabels[i].text = character?.displayName ?? "";

                if (levelLabels != null && i < levelLabels.Length && levelLabels[i] != null)
                    levelLabels[i].text = character != null ? $"Lv {character.level}" : "";

                if (portraits != null && i < portraits.Length && portraits[i] != null)
                {
                    if (character != null && registry != null && builder != null)
                        RenderPortrait(i, character, registry, builder);
                    else
                        portraits[i].texture = null;
                }
            }
        }

        private void RenderPortrait(int index, CharacterInstance character,
            DisplayDataRegistry registry, DisplayBuilder builder)
        {
            while (renderers.Count <= index)
            {
                var obj = new GameObject($"PortraitRenderer_{renderers.Count}");
                obj.transform.SetParent(transform);
                var r = obj.AddComponent<CharacterPortraitRenderer>();
                r.Initialize(new ImageResolver(), 100);
                renderers.Add(r);
            }

            if (!registry.Species.TryGetValue(character.speciesId, out var speciesData)) return;

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
                    IsOffhand = eq.Slot == EquipmentSlot.OffHand
                });
            }

            renderers[index].RebuildFromData(instance, speciesData, equipList, builder);
            portraits[index].texture = renderers[index].Texture;
        }

        private void OnDestroy()
        {
            foreach (var r in renderers)
                if (r != null) Destroy(r.gameObject);
            renderers.Clear();
        }
    }
}
