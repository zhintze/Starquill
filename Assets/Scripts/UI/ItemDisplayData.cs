using System;
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
        public string StatSummary;
        public EquipmentSlot Slot;
        public Rarity Rarity;

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

        public static ItemDisplayData FromItem(EquipmentInstance item, int questLevel)
        {
            if (item == null)
            {
                return new ItemDisplayData
                {
                    DisplayName = "",
                    SlotName = "",
                    RarityName = "",
                    RarityColor = Color.white,
                    Score = 0f,
                    SellValue = 0,
                    StatSummary = "",
                    Slot = EquipmentSlot.Head,
                    Rarity = Rarity.Common
                };
            }

            var totalStats = item.GetTotalStatMods();
            string statSummary = BuildStatSummary(totalStats);

            return new ItemDisplayData
            {
                DisplayName = item.DisplayName,
                SlotName = GetSlotName(item.Slot),
                RarityName = item.Rarity.ToString(),
                RarityColor = GetRarityColor(item.Rarity),
                Score = ItemComparer.ScoreItem(item),
                SellValue = SellCalculator.GetSellValue(item, questLevel),
                StatSummary = statSummary,
                Slot = item.Slot,
                Rarity = item.Rarity
            };
        }

        private static string BuildStatSummary(Stats stats)
        {
            var parts = new System.Collections.Generic.List<string>();
            var statTypes = (StatType[])Enum.GetValues(typeof(StatType));

            foreach (var statType in statTypes)
            {
                int value = stats.GetStat(statType);
                if (value != 0)
                {
                    string sign = value > 0 ? "+" : "";
                    parts.Add($"{statType} {sign}{value}");
                }
            }

            return string.Join("  ", parts);
        }
    }
}
