using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Equipment
{
    public class EquipmentInstance
    {
        public string ItemType { get; }
        public int ItemNum { get; }
        public EquipmentSlot Slot { get; }
        public Rarity Rarity { get; }
        public Color BaseColor { get; }
        public IReadOnlyDictionary<int, Color> VarianceColors { get; }
        public StatType PrimaryStat { get; }
        public int PrimaryValue { get; }
        public StatType SecondaryStat { get; }
        public int SecondaryValue { get; }
        public AwakenedAbility Ability { get; }
        public int[] LayerCodes { get; }
        public int[] HiddenLayers { get; }
        public int[] LayerColorVariance { get; }
        public bool Modular { get; }
        public string HandType { get; }
        public string DisplayName { get; }
        public int[] LayerVariants { get; }

        public EquipmentInstance(
            string itemType, int itemNum, EquipmentSlot slot, Rarity rarity,
            Color baseColor, Dictionary<int, Color> varianceColors,
            StatType primaryStat, int primaryValue,
            StatType secondaryStat, int secondaryValue,
            AwakenedAbility ability,
            int[] layerCodes, int[] hiddenLayers, int[] layerColorVariance,
            bool modular, string handType, string displayName,
            int[] layerVariants = null)
        {
            ItemType = itemType;
            ItemNum = itemNum;
            Slot = slot;
            Rarity = rarity;
            BaseColor = baseColor;
            VarianceColors = varianceColors ?? new Dictionary<int, Color>();
            PrimaryStat = primaryStat;
            PrimaryValue = primaryValue;
            SecondaryStat = secondaryStat;
            SecondaryValue = secondaryValue;
            Ability = ability;
            LayerCodes = layerCodes ?? Array.Empty<int>();
            HiddenLayers = hiddenLayers ?? Array.Empty<int>();
            LayerColorVariance = layerColorVariance ?? Array.Empty<int>();
            Modular = modular;
            HandType = handType;
            DisplayName = displayName ?? "";
            LayerVariants = layerVariants;
        }

        public Stats GetTotalStatMods()
        {
            var stats = new Stats();
            stats.SetStat(PrimaryStat, PrimaryValue);
            stats.SetStat(SecondaryStat,
                stats.GetStat(SecondaryStat) + SecondaryValue);
            if (Ability != null)
            {
                int current = stats.GetStat(Ability.BoostedStat);
                stats.SetStat(Ability.BoostedStat, current + (int)Ability.CurrentPotency);
            }
            return stats;
        }
    }
}
