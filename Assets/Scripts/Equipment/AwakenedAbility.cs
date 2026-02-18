using System;
using Starquill.Core;

namespace Starquill.Equipment
{
    [Serializable]
    public class AwakenedAbility
    {
        public string AbilityId;
        public StatType BoostedStat;
        public float BasePotency;
        public float PotencyPerLevel;
        public int Level;
        public int MaxLevel;
        public float CurrentXP;

        public float CurrentPotency => BasePotency + (Level - 1) * PotencyPerLevel;

        public float XPToNextLevel(float baseThreshold, float growthRate)
        {
            return baseThreshold * (float)Math.Pow(growthRate, Level - 1);
        }

        public bool CanLevelUp(float baseThreshold, float growthRate)
        {
            return Level < MaxLevel && CurrentXP >= XPToNextLevel(baseThreshold, growthRate);
        }

        public bool TryLevelUp(float baseThreshold, float growthRate)
        {
            if (!CanLevelUp(baseThreshold, growthRate)) return false;
            CurrentXP -= XPToNextLevel(baseThreshold, growthRate);
            Level++;
            return true;
        }

        public void AddXP(float amount, float baseThreshold, float growthRate)
        {
            if (Level >= MaxLevel) return;
            CurrentXP += amount;
            while (CanLevelUp(baseThreshold, growthRate))
                TryLevelUp(baseThreshold, growthRate);
        }

        public float GoldCostToLevel(float baseCost, float costGrowthRate, float rarityMultiplier)
        {
            if (Level >= MaxLevel) return float.MaxValue;
            return baseCost * (float)Math.Pow(costGrowthRate, Level - 1) * rarityMultiplier;
        }

        public static int MaxLevelForRarity(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 2,
                Rarity.Uncommon => 3,
                Rarity.Rare => 4,
                Rarity.Epic => 5,
                Rarity.Legendary => 7,
                _ => 2
            };
        }

        public static float PotencyModifierForRarity(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 0.8f,
                Rarity.Uncommon => 1.0f,
                Rarity.Rare => 1.2f,
                Rarity.Epic => 1.5f,
                Rarity.Legendary => 2.0f,
                _ => 1.0f
            };
        }

        public static AwakenedAbility Create(AbilityEntry entry, Rarity rarity)
        {
            return new AwakenedAbility
            {
                AbilityId = entry.Id,
                BoostedStat = entry.BoostedStat,
                BasePotency = entry.BasePotency * PotencyModifierForRarity(rarity),
                PotencyPerLevel = entry.PotencyPerLevel * PotencyModifierForRarity(rarity),
                Level = 1,
                MaxLevel = MaxLevelForRarity(rarity),
                CurrentXP = 0f
            };
        }
    }
}
