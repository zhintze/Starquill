using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Equipment
{
    public struct StatDiff
    {
        public StatType PrimaryStat;
        public int PrimaryDelta;
        public StatType SecondaryStat;
        public int SecondaryDelta;
        public float TotalDelta;
        public bool IsUpgrade;
    }

    public static class ItemComparer
    {
        public static float ScoreItem(EquipmentInstance item)
        {
            if (item == null) return 0f;
            return item.PrimaryValue + item.SecondaryValue;
        }

        public static StatDiff Compare(EquipmentInstance newItem, EquipmentInstance current)
        {
            float newScore = ScoreItem(newItem);
            float currentScore = ScoreItem(current);
            float delta = newScore - currentScore;

            return new StatDiff
            {
                PrimaryStat = newItem != null ? newItem.PrimaryStat : default,
                PrimaryDelta = (newItem?.PrimaryValue ?? 0) - (current?.PrimaryValue ?? 0),
                SecondaryStat = newItem != null ? newItem.SecondaryStat : default,
                SecondaryDelta = (newItem?.SecondaryValue ?? 0) - (current?.SecondaryValue ?? 0),
                TotalDelta = delta,
                IsUpgrade = delta > 0
            };
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
