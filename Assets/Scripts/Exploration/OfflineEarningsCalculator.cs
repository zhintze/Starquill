using Starquill.Data;

namespace Starquill.Exploration
{
    /// Pure math for away-time rewards. The party "keeps exploring" at
    /// reduced efficiency (EconomyConfig.OfflineGold handles the 50%
    /// efficiency and 8-hour cap).
    public static class OfflineEarningsCalculator
    {
        public const float MinimumOfflineSeconds = 60f;

        public static double Gold(float offlineSeconds, int questLevel, EconomyConfig config)
        {
            if (offlineSeconds < MinimumOfflineSeconds) return 0;

            float goldPerSecond = (float)(config.GoldPerKill(questLevel)
                * config.exploreKillsPerMinute / 60f);
            return config.OfflineGold(goldPerSecond, offlineSeconds, config.prestigeMultiplierBase);
        }

        /// Chest payout: gold bundle + guaranteed-quality item rolls.
        public static (double gold, int itemRolls) ChestReward(int questLevel,
            EconomyConfig config, bool doubled)
        {
            double gold = config.GoldPerKill(questLevel) * config.chestGoldKillMultiple;
            int rolls = 1;
            if (doubled)
            {
                gold *= 2;
                rolls = 2;
            }
            return (gold, rolls);
        }
    }
}
