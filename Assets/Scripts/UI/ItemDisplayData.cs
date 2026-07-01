using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.UI
{
    public struct ItemDisplayData
    {
        public string DisplayName;
        public string SlotName;
        public string RarityName;
        public Color RarityColor;
        public float Score;
        public double SellValue;
        public EquipmentSlot Slot;
        public Rarity Rarity;

        public StatType PrimaryStat;
        public int PrimaryValue;
        public StatType SecondaryStat;
        public int SecondaryValue;

        public bool HasAbility;
        public string AbilityName;
        public string AbilityDescription;
        public StatType AbilityStat;
        public float AbilityPotency;
        public int AbilityLevel;
        public int AbilityMaxLevel;
        public float AbilityXpFraction;

        private static readonly Color[] RarityColors =
        {
            new Color(0.7f, 0.7f, 0.7f), // Common - gray
            new Color(0.3f, 0.8f, 0.3f), // Uncommon - green
            new Color(0.3f, 0.5f, 1.0f), // Rare - blue
            new Color(0.7f, 0.3f, 0.9f), // Epic - purple
            new Color(1.0f, 0.6f, 0.1f)  // Legendary - orange
        };

        private static readonly string[] SlotNameLookup =
        {
            "Head",
            "Torso",
            "Arms",
            "Legs",
            "Feet",
            "Main Hand",
            "Off Hand",
            "Misc 1",
            "Misc 2",
            "Misc 3",
            "Misc 4"
        };

        public static string GetSlotName(EquipmentSlot slot)
        {
            int idx = (int)slot;
            if (idx >= 0 && idx < SlotNameLookup.Length)
                return SlotNameLookup[idx];
            return slot.ToString();
        }

        public static Color GetRarityColor(Rarity rarity)
        {
            int idx = (int)rarity;
            if (idx >= 0 && idx < RarityColors.Length)
                return RarityColors[idx];
            return RarityColors[0];
        }

        public static string StatLabel(StatType stat, int value)
        {
            string sign = value >= 0 ? "+" : "";
            return $"{stat} {sign}{value}";
        }

        public static string StatLabelColored(StatType stat, int value)
        {
            string hex = ColorUtility.ToHtmlStringRGB(StatTypeColors.GetColor(stat));
            return $"<color=#{hex}>{StatLabel(stat, value)}</color>";
        }

        public static ItemDisplayData FromItem(EquipmentInstance item, int questLevel,
            AbilityTable abilityTable = null, EconomyConfig economy = null)
        {
            if (item == null)
            {
                return new ItemDisplayData
                {
                    DisplayName = "",
                    SlotName = "",
                    RarityName = "",
                    RarityColor = Color.white,
                    Slot = EquipmentSlot.Head,
                    Rarity = Rarity.Common
                };
            }

            var data = new ItemDisplayData
            {
                DisplayName = item.DisplayName,
                SlotName = GetSlotName(item.Slot),
                RarityName = item.Rarity.ToString(),
                RarityColor = GetRarityColor(item.Rarity),
                Score = ItemComparer.ScoreItem(item),
                SellValue = SellCalculator.GetSellValue(item, questLevel),
                Slot = item.Slot,
                Rarity = item.Rarity,
                PrimaryStat = item.PrimaryStat,
                PrimaryValue = item.PrimaryValue,
                SecondaryStat = item.SecondaryStat,
                SecondaryValue = item.SecondaryValue
            };

            var ability = item.Ability;
            if (ability != null)
            {
                data.HasAbility = true;
                data.AbilityStat = ability.BoostedStat;
                data.AbilityPotency = ability.CurrentPotency;
                data.AbilityLevel = ability.Level;
                data.AbilityMaxLevel = ability.MaxLevel;

                var entry = abilityTable?.GetById(ability.AbilityId);
                data.AbilityName = entry != null ? entry.Name : ability.AbilityId;
                data.AbilityDescription = entry != null
                    ? AbilityTable.FormatDescription(entry, ability.CurrentPotency)
                    : "";

                if (ability.Level >= ability.MaxLevel)
                {
                    data.AbilityXpFraction = 1f;
                }
                else if (economy != null)
                {
                    float needed = ability.XPToNextLevel(
                        economy.abilityBaseXPThreshold, economy.abilityXPGrowthRate);
                    data.AbilityXpFraction = needed > 0f
                        ? Mathf.Clamp01(ability.CurrentXP / needed) : 0f;
                }
            }

            return data;
        }
    }
}
