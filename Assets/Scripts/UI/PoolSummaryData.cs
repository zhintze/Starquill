using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Characters;

namespace Starquill.UI
{
    public struct PoolSummaryData
    {
        private readonly Dictionary<StatType, int> counts;

        public int TotalVerbs { get; private set; }

        private PoolSummaryData(Dictionary<StatType, int> counts, int total)
        {
            this.counts = counts;
            TotalVerbs = total;
        }

        public int GetCount(StatType stat) =>
            counts != null && counts.TryGetValue(stat, out int c) ? c : 0;

        public bool HasGap(StatType stat) => GetCount(stat) == 0;

        public static PoolSummaryData FromParty(IEnumerable<CharacterInstance> party)
        {
            var counts = new Dictionary<StatType, int>();
            int total = 0;

            foreach (var character in party)
            {
                if (character == null) continue;
                foreach (var verb in character.equippedVerbs)
                {
                    if (verb == null) continue;
                    if (!counts.ContainsKey(verb.statType))
                        counts[verb.statType] = 0;
                    counts[verb.statType]++;
                    total++;
                }
            }

            return new PoolSummaryData(counts, total);
        }
    }
}
