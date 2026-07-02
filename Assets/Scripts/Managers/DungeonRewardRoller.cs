using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;
using Starquill.Destinations;
using Starquill.Equipment;

namespace Starquill.Managers
{
    /// Rolls dungeon completion rewards (design §3). Pure creation only:
    /// GameManager applies inventory-or-mailbox delivery and gold.
    public static class DungeonRewardRoller
    {
        private static readonly string[] ArmorPrefixes = { "hd", "tr", "ar", "lg", "fe", "mc" };

        /// Single source for key -> drop targeting (design §2). KeySlot.Weapon
        /// maps to PreferWeapon, never PreferredSlot = MainHand.
        public static DropModifiers ModifiersFor(KeyInstance key)
        {
            var mods = new DropModifiers { ColorFamily = key.ColorFamily };

            if (key.Slot.HasValue)
            {
                if (key.Slot.Value == KeySlot.Weapon)
                    mods.PreferWeapon = true;
                else
                    mods.PreferredSlot = key.Slot.Value switch
                    {
                        KeySlot.Head => EquipmentSlot.Head,
                        KeySlot.Torso => EquipmentSlot.Torso,
                        KeySlot.Arms => EquipmentSlot.Arms,
                        KeySlot.Legs => EquipmentSlot.Legs,
                        KeySlot.Feet => EquipmentSlot.Feet,
                        _ => EquipmentSlot.Misc1
                    };
            }

            var arch = key.Archetype;
            if (arch != null)
            {
                mods.ForcedStatA = arch.StatA;
                mods.ForcedStatB = arch.StatB;
            }

            return mods;
        }

        public static List<EquipmentInstance> Roll(DungeonRun run, EquipmentFactory factory,
            EconomyConfig config, int questLevel, System.Random rng, out double goldBonus)
        {
            var spec = run.Spec;
            int rolls = spec.BaseRolls
                + run.BonusRolls(config.dungeonBonusWavesPerRoll, config.dungeonBonusRollCap);

            var items = new List<EquipmentInstance>();
            for (int i = 0; i < rolls; i++)
            {
                var item = RollOne(spec, factory, questLevel, rng);
                if (item != null) items.Add(item);
            }

            // 3 ~= avg enemies per wave; revisit in balance pass.
            goldBonus = config.GoldPerKill(questLevel, 0f, config.prestigeMultiplierBase)
                * run.WavesCleared * 3;
            return items;
        }

        /// One floored, key-modified roll: shared by completion rewards and
        /// mini-boss immediate drops.
        public static EquipmentInstance RollOne(DungeonSpec spec, EquipmentFactory factory,
            int questLevel, System.Random rng)
        {
            var mods = ModifiersFor(spec.Key);
            var rarity = EquipmentFactory.RollRarityWithFloor(
                questLevel, spec.RarityFloor, rng, spec.LegendaryWeightMult);

            // Slot bias mirrors LootDropper.TryDrop: preferred slot wins
            // SlotWeight of the time, otherwise 70/30 armor/weapon.
            bool wantWeapon = mods.PreferWeapon && rng.NextDouble() < mods.SlotWeight;
            string forcedPrefix = null;
            if (!wantWeapon && mods.PreferredSlot != null && rng.NextDouble() < mods.SlotWeight)
                forcedPrefix = LootDropper.PrefixForSlot(mods.PreferredSlot.Value);

            if (wantWeapon || (forcedPrefix == null && rng.NextDouble() < 0.30))
                return factory.CreateRandomWeapon(rarity, rng, questLevel: questLevel, modifiers: mods);

            string prefix = forcedPrefix ?? ArmorPrefixes[rng.Next(ArmorPrefixes.Length)];
            return factory.CreateRandom(prefix, rarity, rng, questLevel, mods);
        }
    }
}
