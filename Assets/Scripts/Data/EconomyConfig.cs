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
        // Training ladder cost curve (sim-tuned 2026-07-02: wall ~Q75 at
        // ~2 weeks casual; see tools/balance_sim.py)
        public float baseCost = 50f;
        public float costGrowthRate = 0.25f;

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
        public int travelWavesToDiscovery = 20;
        public float fragmentDropRate = 0.01f;

        [Header("Loot")]
        public float baseDropRate = 0.15f;

        [Header("Boosts & Chest & Character XP")]
        public float boostAutoFireCostPerLevel = 500f;
        public float boostSpeedUpCostPerLevel = 300f;
        public float boostDurationSeconds = 300f;
        public float chestIntervalSeconds = 14400f;
        public float chestGoldKillMultiple = 25f;
        public int charXpBase = 3;
        public float benchXpShare = 0.5f;

        [Header("Training & Scaling")]
        public float trainingDamagePerLevel = 0.05f;
        public float trainingCatchUpDiscount = 0.5f;
        public float charLevelDamageBonus = 0.015f;
        public float gearBudgetPerLevel = 0.015f;
        public float boostAutoFireIncomeMinutes = 5f;
        public float boostSpeedUpIncomeMinutes = 3f;

        [Header("Destinations: Keys")]
        public float keyDropRate = 0.004f;      // per active kill
        public int keySoftCap = 30;             // over: drop rate halves
        public int keyMaxDifficulty = 6;
        public float fusionBaseCost = 250f;     // x questLevel x D^2
        public float keySellBase = 25f;         // x questLevel x D

        [Header("Destinations: Dungeons")]
        public float dungeonDurationBase = 150f;    // seconds, +dungeonDurationPerD per D above 1
        public float dungeonDurationPerD = 20f;
        public float dungeonEnemyMultPerD = 0.6f;   // D3 steep curve
        public float dungeonWaveHpRamp = 0.02f;     // +2% enemy HP per wave cleared
        public int dungeonBonusWavesPerRoll = 3;    // waves over par per bonus roll
        public int dungeonBonusRollCap = 3;
        public float dungeonParSecondsPerWave = 15f;

        [Header("Tavern")]
        public int tavernSize = 6;
        public float tavernRotationHours = 6f;
        public float tavernCostMinutes = 25f;   // recruit price in minutes of current gold income

        [Header("Ability Leveling")]
        public float abilityBaseXPRate = 1f;
        public float abilityBaseXPThreshold = 100f;
        public float abilityXPGrowthRate = 1.8f;
        public float abilityBaseLevelUpCost = 100f;
        public float abilityCostGrowthRate = 2.0f;

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
