using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Destinations
{
    /// The 15 two-stat build archetypes class keys target (design table §2).
    /// Ids are stable save-format contract: never reorder or reuse.
    public class ClassArchetype
    {
        public int Id { get; }
        public string Name { get; }
        public StatType StatA { get; }
        public StatType StatB { get; }

        private ClassArchetype(int id, string name, StatType a, StatType b)
        { Id = id; Name = name; StatA = a; StatB = b; }

        public static readonly IReadOnlyList<ClassArchetype> All = new[]
        {
            new ClassArchetype(0,  "Warrior",    StatType.STR, StatType.DEX),
            new ClassArchetype(1,  "Juggernaut", StatType.STR, StatType.CON),
            new ClassArchetype(2,  "Battlemage", StatType.STR, StatType.INT),
            new ClassArchetype(3,  "Warden",     StatType.STR, StatType.WIS),
            new ClassArchetype(4,  "Warlord",    StatType.STR, StatType.CHA),
            new ClassArchetype(5,  "Skirmisher", StatType.DEX, StatType.CON),
            new ClassArchetype(6,  "Saboteur",   StatType.DEX, StatType.INT),
            new ClassArchetype(7,  "Ranger",     StatType.DEX, StatType.WIS),
            new ClassArchetype(8,  "Trickster",  StatType.DEX, StatType.CHA),
            new ClassArchetype(9,  "Sentinel",   StatType.CON, StatType.INT),
            new ClassArchetype(10, "Guardian",   StatType.CON, StatType.WIS),
            new ClassArchetype(11, "Champion",   StatType.CON, StatType.CHA),
            new ClassArchetype(12, "Sage",       StatType.INT, StatType.WIS),
            new ClassArchetype(13, "Occultist",  StatType.INT, StatType.CHA),
            new ClassArchetype(14, "Oracle",     StatType.WIS, StatType.CHA),
        };

        public static ClassArchetype ById(int id) =>
            id >= 0 && id < All.Count ? All[id] : null;

        public static ClassArchetype ForPair(StatType a, StatType b)
        {
            foreach (var arch in All)
                if ((arch.StatA == a && arch.StatB == b) || (arch.StatA == b && arch.StatB == a))
                    return arch;
            return null;
        }
    }
}
