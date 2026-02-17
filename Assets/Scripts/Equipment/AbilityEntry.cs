using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Equipment
{
    public class AbilityEntry
    {
        public string Id;
        public string Name;
        public string Description;
        public string TriggerType;
        public string EffectType;
        public StatType BoostedStat;
        public float BasePotency;
        public float PotencyPerLevel;
        public float ProcChance;
        public List<string> ValidTypes = new();
        public int Weight;
    }
}
