using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Characters
{
    [Serializable]
    public class CharacterInstance
    {
        public string id;
        public string displayName;
        public SpeciesDefinition species;
        public Stats baseStats;
        public int level = 1;
        public int xp;
        public int speciesKills;
        public EquipmentDefinition[] equipment = new EquipmentDefinition[11];
        public List<VerbDefinition> equippedVerbs = new();
        public Stats allocatedStats = new();

        public Stats GetTotalStats()
        {
            var total = baseStats.Clone();
            total = total + allocatedStats;
            foreach (var equip in equipment)
            {
                if (equip != null)
                    total = total + equip.statMods;
            }
            return total;
        }

        public int GetVerbSlotCount()
        {
            if (level >= 51) return 5;
            if (level >= 26) return 4;
            if (level >= 11) return 3;
            return 2;
        }

        public int GetSpeciesAbilityRank()
        {
            if (species?.ability == null) return 0;
            var thresholds = species.ability.rankThresholds;
            for (int i = thresholds.Length - 1; i >= 0; i--)
            {
                if (speciesKills >= thresholds[i]) return i;
            }
            return 0;
        }

        public void EquipItem(EquipmentDefinition item)
        {
            equipment[(int)item.slot] = item;
        }

        public void UnequipSlot(EquipmentSlot slot)
        {
            equipment[(int)slot] = null;
        }
    }
}
