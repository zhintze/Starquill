using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;

namespace Starquill.Characters
{
    [Serializable]
    public class CharacterInstance
    {
        public string id;
        public string displayName;
        public string speciesId;
        public Stats baseStats;
        public int level = 1;
        public int xp;
        public int speciesKills;
        public EquipmentInstance[] equipment = new EquipmentInstance[11];
        public List<VerbDefinition> equippedVerbs = new();
        public Stats allocatedStats = new();

        public Stats GetTotalStats()
        {
            var total = baseStats.Clone();
            total = total + allocatedStats;
            foreach (var equip in equipment)
            {
                if (equip != null)
                    total = total + equip.GetTotalStatMods();
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

        public void EquipItem(EquipmentInstance item)
        {
            equipment[(int)item.Slot] = item;
        }

        public void UnequipSlot(EquipmentSlot slot)
        {
            equipment[(int)slot] = null;
        }

        public void EquipLoadout(EquipmentInstance[] loadout)
        {
            for (int i = 0; i < loadout.Length && i < equipment.Length; i++)
                equipment[i] = loadout[i];
        }
    }
}
