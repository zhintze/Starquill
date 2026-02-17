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
        public Stats StatMods { get; }
        public IReadOnlyList<RolledAffix> RolledAffixes { get; }
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
            Stats statMods, List<RolledAffix> rolledAffixes,
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
            StatMods = statMods ?? new Stats();
            RolledAffixes = rolledAffixes ?? new List<RolledAffix>();
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
            var total = StatMods.Clone();
            foreach (var affix in RolledAffixes)
            {
                if (!affix.IsPercentage)
                {
                    int current = total.GetStat(affix.StatType);
                    total.SetStat(affix.StatType, current + (int)affix.Value);
                }
            }
            return total;
        }
    }
}
