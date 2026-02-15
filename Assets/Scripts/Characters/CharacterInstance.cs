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
        public List<VerbDefinition> unlockedVerbs = new();
        public List<VerbDefinition> equippedVerbs = new();
        public Stats allocatedStats = new();

        // Fractional stat accumulation for weighted distribution
        private float[] statAccumulator = new float[6];

        public int XpToNextLevel()
        {
            return (int)(100 * System.Math.Pow(1.18, level));
        }

        public bool CanLevelUp()
        {
            return xp >= XpToNextLevel();
        }

        public void LevelUp()
        {
            int cost = XpToNextLevel();
            if (xp < cost) return;

            xp -= cost;
            level++;

            DistributeStats();
        }

        private void DistributeStats()
        {
            float totalBase = baseStats.Total;
            if (totalBase <= 0) return;

            float pointsPerLevel = 3f;
            var types = (StatType[])System.Enum.GetValues(typeof(StatType));

            for (int i = 0; i < types.Length; i++)
            {
                float weight = baseStats.GetStat(types[i]) / totalBase;
                statAccumulator[i] += weight * pointsPerLevel;

                int whole = (int)statAccumulator[i];
                if (whole > 0)
                {
                    allocatedStats.SetStat(types[i], allocatedStats.GetStat(types[i]) + whole);
                    statAccumulator[i] -= whole;
                }
            }
        }

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

        public bool EquipVerb(VerbDefinition verb)
        {
            if (!unlockedVerbs.Contains(verb)) return false;
            if (equippedVerbs.Count >= GetVerbSlotCount()) return false;
            if (equippedVerbs.Contains(verb)) return false;
            equippedVerbs.Add(verb);
            return true;
        }

        public void UnequipVerb(VerbDefinition verb)
        {
            equippedVerbs.Remove(verb);
        }

        public bool SwapVerb(int equippedIndex, VerbDefinition newVerb)
        {
            if (equippedIndex < 0 || equippedIndex >= equippedVerbs.Count) return false;
            if (!unlockedVerbs.Contains(newVerb)) return false;
            if (equippedVerbs.Contains(newVerb) && equippedVerbs[equippedIndex] != newVerb) return false;
            equippedVerbs[equippedIndex] = newVerb;
            return true;
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
