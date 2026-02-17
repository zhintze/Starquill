using System;
using System.Collections.Generic;
using System.Linq;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Characters
{
    public class CharacterFactory
    {
        private readonly DisplayDataRegistry registry;
        private readonly EquipmentFactory equipFactory;
        private readonly NameGenerator nameGen;

        private static readonly (string id, string name, StatType type, float dmg, float cd, bool heal, float healAmt)[] StarterVerbs =
        {
            ("slash", "Slash", StatType.STR, 50f, 2f, false, 0f),
            ("shield_bash", "Shield Bash", StatType.CON, 30f, 3f, false, 0f),
            ("fireball", "Fireball", StatType.INT, 65f, 3f, false, 0f),
            ("heal", "Heal", StatType.WIS, 0f, 4f, true, 40f),
        };

        public CharacterFactory(DisplayDataRegistry registry, EquipmentFactory equipFactory, NameGenerator nameGen)
        {
            this.registry = registry;
            this.equipFactory = equipFactory;
            this.nameGen = nameGen;
        }

        public CharacterInstance CreateRandom(string speciesKey, int level, int questLevel, System.Random rng)
        {
            if (!registry.Species.TryGetValue(speciesKey, out var speciesData))
                return null;

            var character = new CharacterInstance
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 8),
                speciesId = speciesKey,
                level = level,
                displayName = nameGen.Generate(rng),
                baseStats = GenerateBaseStats(rng)
            };

            var loadout = questLevel <= 1
                ? equipFactory.CreateStarterLoadout(rng)
                : equipFactory.CreateRandomLoadout(questLevel, rng);
            character.EquipLoadout(loadout);

            AssignVerbs(character, rng);

            return character;
        }

        public List<CharacterInstance> CreateStarterRoster(int count, System.Random rng)
        {
            var speciesKeys = new List<string>(registry.Species.Keys);
            if (speciesKeys.Count == 0) return new List<CharacterInstance>();

            var roster = new List<CharacterInstance>();
            var speciesUsage = new Dictionary<string, int>();

            for (int i = 0; i < count; i++)
            {
                var available = speciesKeys.Where(k =>
                    !speciesUsage.ContainsKey(k) || speciesUsage[k] < 2).ToList();
                if (available.Count == 0)
                    available = speciesKeys;

                var key = available[rng.Next(available.Count)];
                if (!speciesUsage.ContainsKey(key)) speciesUsage[key] = 0;
                speciesUsage[key]++;

                var character = CreateRandom(key, 1, 1, rng);
                if (character != null)
                    roster.Add(character);
            }

            return roster;
        }

        public static VerbDefinition CreateVerbById(string verbId)
        {
            foreach (var v in StarterVerbs)
                if (v.id == verbId) return CreateVerb(v.id, v.name, v.type, v.dmg, v.cd, v.heal, v.healAmt);
            return null;
        }

        private Stats GenerateBaseStats(System.Random rng)
        {
            int budget = 30;
            var stats = new Stats();
            var types = (StatType[])Enum.GetValues(typeof(StatType));

            foreach (var type in types)
            {
                stats.SetStat(type, 3);
                budget -= 3;
            }

            for (int i = 0; i < budget; i++)
            {
                var type = types[rng.Next(types.Length)];
                stats.SetStat(type, stats.GetStat(type) + 1);
            }

            return stats;
        }

        private void AssignVerbs(CharacterInstance character, System.Random rng)
        {
            var highestStat = character.baseStats.HighestStat();

            var primary = StarterVerbs.Where(v => v.type == highestStat).ToArray();
            if (primary.Length == 0)
                primary = StarterVerbs;

            var (id1, name1, type1, dmg1, cd1, heal1, ha1) = primary[rng.Next(primary.Length)];
            var verb1 = CreateVerb(id1, name1, type1, dmg1, cd1, heal1, ha1);
            character.unlockedVerbs.Add(verb1);
            character.equippedVerbs.Add(verb1);

            var remaining = StarterVerbs.Where(v => v.id != id1).ToArray();
            if (remaining.Length > 0)
            {
                var (id2, name2, type2, dmg2, cd2, heal2, ha2) = remaining[rng.Next(remaining.Length)];
                var verb2 = CreateVerb(id2, name2, type2, dmg2, cd2, heal2, ha2);
                character.unlockedVerbs.Add(verb2);
                character.equippedVerbs.Add(verb2);
            }
        }

        private static VerbDefinition CreateVerb(string id, string name, StatType type,
            float damage, float cooldown, bool isHealing, float healAmount)
        {
            var verb = ScriptableObject.CreateInstance<VerbDefinition>();
            verb.verbId = id;
            verb.displayName = name;
            verb.statType = type;
            verb.baseDamage = damage;
            verb.cooldownTicks = cooldown;
            verb.isHealingVerb = isHealing;
            verb.healAmount = healAmount;
            return verb;
        }
    }
}
