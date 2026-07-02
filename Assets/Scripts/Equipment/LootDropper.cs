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

        public EquipmentInstance TryDrop(int questLevel, System.Random rng,
            DropModifiers modifiers = null)
        {
            // Check pity first
            var pityRarity = pity.RegisterKill(config);

            // Roll drop chance
            bool shouldDrop = pityRarity.HasValue || rng.NextDouble() < config.baseDropRate;
            if (!shouldDrop) return null;

            // Determine rarity
            Rarity rarity = pityRarity ?? EquipmentFactory.RollRarity(questLevel, rng);

            // Slot preference (dungeon keys) replaces the 70/30 armor/weapon split
            EquipmentInstance item;
            bool wantWeapon = modifiers?.PreferWeapon == true && rng.NextDouble() < modifiers.SlotWeight;
            string forcedPrefix = null;
            if (!wantWeapon && modifiers?.PreferredSlot != null && rng.NextDouble() < modifiers.SlotWeight)
                forcedPrefix = PrefixForSlot(modifiers.PreferredSlot.Value);

            if (wantWeapon || (forcedPrefix == null && rng.NextDouble() < 0.30))
            {
                item = factory.CreateRandomWeapon(rarity, rng, questLevel: questLevel, modifiers: modifiers);
            }
            else
            {
                string prefix = forcedPrefix ?? ArmorPrefixes[rng.Next(ArmorPrefixes.Length)];
                item = factory.CreateRandom(prefix, rarity, rng, questLevel, modifiers);
            }

            // Register the drop with pity tracker
            if (item != null)
                pity.RegisterDrop(item.Rarity);

            return item;
        }

        /// Catalog prefix for a preferred equipment slot. Public: the dungeon
        /// reward roller (Task 14) reuses this mapping.
        public static string PrefixForSlot(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => "hd",
                EquipmentSlot.Torso => "tr",
                EquipmentSlot.Arms => "ar",
                EquipmentSlot.Legs => "lg",
                EquipmentSlot.Feet => "fe",
                _ => "mc"
            };
        }
    }
}
