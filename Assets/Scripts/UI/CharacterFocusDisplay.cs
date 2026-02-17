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
    public class CharacterFocusDisplay : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text speciesLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text xpLabel;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Image levelUpGlow;

        [Header("Paper Doll")]
        [SerializeField] private RawImage paperDollImage;

        [Header("Stats")]
        [SerializeField] private TMP_Text[] statLabels;

        private CharacterDisplay characterDisplay;
        private DisplayBuilder builder;
        private DisplayDataRegistry registry;

        public event Action OnLevelUpPressed;

        public void Initialize(DisplayDataRegistry reg, DisplayBuilder bld)
        {
            registry = reg;
            builder = bld;

            if (levelUpButton != null)
                levelUpButton.onClick.AddListener(() => OnLevelUpPressed?.Invoke());
        }

        public void ShowCharacter(CharacterInstance character)
        {
            if (character == null) return;

            if (nameLabel != null) nameLabel.text = character.displayName;
            if (speciesLabel != null) speciesLabel.text = character.speciesId;
            if (levelLabel != null) levelLabel.text = $"Lv {character.level}";
            UpdateXpDisplay(character);
            UpdateLevelUpButton(character);
            UpdateStatBar(character);
            RenderPaperDoll(character);
        }

        public void UpdateXpDisplay(CharacterInstance character)
        {
            if (xpLabel != null)
                xpLabel.text = $"{character.xp}/{character.XpToNextLevel()} XP";
        }

        public void UpdateLevelUpButton(CharacterInstance character)
        {
            bool canLevel = character.CanLevelUp();
            if (levelUpButton != null)
                levelUpButton.interactable = canLevel;
            if (levelUpGlow != null)
                levelUpGlow.enabled = canLevel;
        }

        public void UpdateStatBar(CharacterInstance character)
        {
            if (statLabels == null) return;

            var total = character.GetTotalStats();
            var baseS = character.baseStats;
            var alloc = character.allocatedStats;

            var types = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            var statTypes = (StatType[])Enum.GetValues(typeof(StatType));
            var colors = new[] {
                StatTypeColors.GetColor(StatType.STR),
                StatTypeColors.GetColor(StatType.DEX),
                StatTypeColors.GetColor(StatType.CON),
                StatTypeColors.GetColor(StatType.INT),
                StatTypeColors.GetColor(StatType.WIS),
                StatTypeColors.GetColor(StatType.CHA),
            };

            for (int i = 0; i < statLabels.Length && i < 6; i++)
            {
                if (statLabels[i] == null) continue;
                int basePlusAlloc = baseS.GetStat(statTypes[i]) + alloc.GetStat(statTypes[i]);
                int totalVal = total.GetStat(statTypes[i]);
                int equipBonus = totalVal - basePlusAlloc;

                statLabels[i].text = equipBonus > 0
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(colors[i])}>{types[i]}</color> {basePlusAlloc}<color=#4ADE80>+{equipBonus}</color>"
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(colors[i])}>{types[i]}</color> {basePlusAlloc}";
            }
        }

        private void RenderPaperDoll(CharacterInstance character)
        {
            if (paperDollImage == null || registry == null || builder == null) return;

            if (!registry.Species.TryGetValue(character.speciesId, out var speciesData)) return;
            var instance = character.GetOrCreateAppearance(speciesData, registry);

            var equipList = new List<EquipmentDisplayInfo>();
            foreach (var eq in character.equipment)
            {
                if (eq == null) continue;
                equipList.Add(new EquipmentDisplayInfo
                {
                    ItemType = eq.ItemType,
                    ItemNum = eq.ItemNum,
                    BaseColor = eq.BaseColor,
                    VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                        ?? new Dictionary<int, Color>(eq.VarianceColors),
                    IsOffhand = eq.Slot == EquipmentSlot.OffHand,
                    LayerVariants = eq.LayerVariants
                });
            }

            if (characterDisplay == null)
            {
                var displayObj = new GameObject("FocusPaperDoll");
                displayObj.transform.SetParent(transform);
                characterDisplay = displayObj.AddComponent<CharacterDisplay>();
                characterDisplay.Initialize(new ImageResolver());
            }

            characterDisplay.RebuildFromData(instance, speciesData, equipList, builder);
            paperDollImage.texture = characterDisplay.Texture;
        }

        private void OnDestroy()
        {
            if (characterDisplay != null)
                Destroy(characterDisplay.gameObject);
        }
    }
}
