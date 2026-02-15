using Starquill.Core;
using Starquill.Data;

namespace Starquill.Equipment
{
    public class LootDropper
    {
        private readonly EquipmentFactory factory;
        private readonly EconomyConfig config;
        private readonly PityTracker pity;

        private static readonly string[] ArmorPrefixes = { "hd", "tr", "ar", "lg", "fe", "mc" };

        public LootDropper(EquipmentFactory factory, EconomyConfig config, PityTracker pity)
        {
            this.factory = factory;
            this.config = config;
            this.pity = pity;
        }

        public EquipmentInstance TryDrop(int questLevel, System.Random rng)
        {
            // Check pity first
            var pityRarity = pity.RegisterKill(config);

            // Roll drop chance
            bool shouldDrop = pityRarity.HasValue || rng.NextDouble() < config.baseDropRate;
            if (!shouldDrop) return null;

            // Determine rarity
            Rarity rarity = pityRarity ?? EquipmentFactory.RollRarity(questLevel, rng);

            // Pick random slot type: 70% armor, 30% weapon
            EquipmentInstance item;
            if (rng.NextDouble() < 0.30)
            {
                item = factory.CreateRandomWeapon(rarity, rng);
            }
            else
            {
                string prefix = ArmorPrefixes[rng.Next(ArmorPrefixes.Length)];
                item = factory.CreateRandom(prefix, rarity, rng);
            }

            // Register the drop with pity tracker
            if (item != null)
                pity.RegisterDrop(item.Rarity);

            return item;
        }
    }
}
