using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Equipment
{
    public struct StatDiff
    {
        public float TotalDelta;
        public bool IsUpgrade;
    }

    public static class ItemComparer
    {
        private const float PercentageAffixWeight = 5f;

        public static float ScoreItem(EquipmentInstance item)
        {
            if (item == null) return 0f;

            float score = item.StatMods.Total;
            foreach (var affix in item.RolledAffixes)
            {
                if (affix.IsPercentage)
                    score += affix.Value * PercentageAffixWeight;
                else
                    score += affix.Value;
            }
            return score;
        }

        public static StatDiff Compare(EquipmentInstance newItem, EquipmentInstance current)
        {
            float newScore = ScoreItem(newItem);
            float currentScore = ScoreItem(current);
            float delta = newScore - currentScore;
            return new StatDiff { TotalDelta = delta, IsUpgrade = delta > 0 };
        }

        public static EquipmentInstance FindBestForSlot(EquipmentSlot slot,
            IEnumerable<EquipmentInstance> candidates, EquipmentInstance currentlyEquipped)
        {
            float currentScore = ScoreItem(currentlyEquipped);
            EquipmentInstance best = null;
            float bestScore = currentScore;

            foreach (var item in candidates)
            {
                if (item.Slot != slot) continue;
                float score = ScoreItem(item);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = item;
                }
            }
            return best;
        }
    }
}
