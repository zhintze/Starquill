using System;
using System.Collections.Generic;
using System.Linq;

namespace Starquill.Characters
{
    /// A rotation of recruitable characters. Generated once per slot and
    /// persisted; never re-derived (keeps ids/appearances stable mid-slot).
    public class TavernStock
    {
        public List<CharacterInstance> Recruits;
        public double[] Prices;      // parallel to Recruits
        public bool[] Purchased;     // parallel to Recruits

        /// Global rotation slot index: floor(now / windowSeconds).
        public static long SlotId(double nowUnix, float rotationHours)
        {
            return (long)Math.Floor(nowUnix / ((double)rotationHours * 3600.0));
        }

        public static double SlotEndsAtUnix(long slotId, float rotationHours)
        {
            return (slotId + 1) * ((double)rotationHours * 3600.0);
        }

        /// Rolls a fresh rotation. Species picks use the same soft-variety
        /// rule as CreateStarterRoster (max 2 per species before reuse);
        /// recruits level up through the organic path so stat points
        /// auto-allocate exactly like played characters.
        public static TavernStock Generate(CharacterFactory factory, IReadOnlyList<string> speciesKeys,
            int count, int targetLevel, int questLevel, double basePrice, System.Random rng)
        {
            var stock = new TavernStock
            {
                Recruits = new List<CharacterInstance>(count),
                Prices = new double[count],
                Purchased = new bool[count]
            };
            if (speciesKeys == null || speciesKeys.Count == 0) return stock;

            var speciesUsage = new Dictionary<string, int>();
            for (int i = 0; i < count; i++)
            {
                var available = speciesKeys.Where(k =>
                    !speciesUsage.ContainsKey(k) || speciesUsage[k] < 2).ToList();
                if (available.Count == 0)
                    available = new List<string>(speciesKeys);

                var key = available[rng.Next(available.Count)];
                if (!speciesUsage.ContainsKey(key)) speciesUsage[key] = 0;
                speciesUsage[key]++;

                var recruit = factory.CreateRandom(key, 1, questLevel, rng);
                if (recruit == null) continue;

                // LevelUp consumes xp and increments level, so paying the
                // exact cost per step lands precisely on targetLevel.
                for (int lv = 1; lv < targetLevel; lv++)
                {
                    recruit.xp += recruit.XpToNextLevel();
                    recruit.LevelUp();
                }

                stock.Prices[stock.Recruits.Count] = basePrice * (0.85 + 0.3 * rng.NextDouble());
                stock.Recruits.Add(recruit);
            }

            // Trim parallels if any species failed to generate (defensive).
            if (stock.Recruits.Count != count)
            {
                Array.Resize(ref stock.Prices, stock.Recruits.Count);
                Array.Resize(ref stock.Purchased, stock.Recruits.Count);
            }

            return stock;
        }
    }
}
