using UnityEngine;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Enemy Scaling")]
        public float baseHP = 50f;
        public float hpGrowthRate = 0.12f;

        [Header("Gold")]
        public float baseGold = 5f;
        public float goldGrowthRate = 0.10f;

        [Header("Upgrade Costs")]
        public float baseCost = 10f;
        public float costGrowthRate = 0.15f;

        [Header("Offline")]
        public float offlineEfficiency = 0.50f;
        public float maxOfflineSeconds = 28800f;
        public float offlineFragmentRate = 0.25f;

        [Header("Verb Draw")]
        public float verbDrawCooldown = 10f;
        public int verbSlotCount = 3;

        [Header("Combat")]
        public float autoAttackDPSFraction = 0.3f;
        public float verbLockDuration = 3.0f;

        [Header("Pity Timers")]
        public int pityUncommon = 50;
        public int pityRare = 200;
        public int pityEpic = 1000;
        public int pityLegendary = 5000;

        [Header("Exploration")]
        public float questRetreatGoldPenalty = 0.50f;
        public float exploreKillsPerMinute = 6f;
        public float questDiscoveryRate = 0.03f;
        public float fragmentDropRate = 0.01f;

        [Header("Prestige (Stub)")]
        public float prestigeMultiplierBase = 1.0f;

        public float EnemyHP(int questLevel)
        {
            return baseHP * Mathf.Pow(1f + hpGrowthRate, questLevel);
        }

        public float GoldPerKill(int questLevel, float chaBonus = 0f, float prestigeMult = 1f, float boostMult = 1f)
        {
            return baseGold * Mathf.Pow(1f + goldGrowthRate, questLevel) * (1f + chaBonus) * prestigeMult * boostMult;
        }

        public float UpgradeCost(int currentLevel)
        {
            return baseCost * Mathf.Pow(1f + costGrowthRate, currentLevel);
        }

        public float OfflineGold(float goldPerSecond, float elapsedSeconds, float prestigeMult = 1f)
        {
            float cappedTime = Mathf.Min(elapsedSeconds, maxOfflineSeconds);
            return goldPerSecond * cappedTime * offlineEfficiency * prestigeMult;
        }
    }
}
