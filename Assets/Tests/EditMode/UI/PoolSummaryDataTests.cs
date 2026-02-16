using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Starquill.Core;
using Starquill.Data;
using Starquill.Characters;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    public class PoolSummaryDataTests
    {
        private VerbDefinition MakeVerb(StatType stat)
        {
            var verb = ScriptableObject.CreateInstance<VerbDefinition>();
            verb.verbId = $"test_{stat}";
            verb.displayName = stat.ToString();
            verb.statType = stat;
            return verb;
        }

        private CharacterInstance MakeChar(string id, params StatType[] verbStats)
        {
            var c = new CharacterInstance { id = id, displayName = id, level = 1 };
            foreach (var stat in verbStats)
            {
                var verb = MakeVerb(stat);
                c.unlockedVerbs.Add(verb);
                c.equippedVerbs.Add(verb);
            }
            return c;
        }

        [Test]
        public void FromParty_CountsVerbsByStatType()
        {
            var party = new[] { MakeChar("a", StatType.STR, StatType.STR), MakeChar("b", StatType.INT) };
            var summary = PoolSummaryData.FromParty(party);
            Assert.AreEqual(2, summary.GetCount(StatType.STR));
            Assert.AreEqual(1, summary.GetCount(StatType.INT));
            Assert.AreEqual(0, summary.GetCount(StatType.WIS));
        }

        [Test]
        public void FromParty_DetectsGaps()
        {
            var party = new[] { MakeChar("a", StatType.STR) };
            var summary = PoolSummaryData.FromParty(party);
            Assert.IsFalse(summary.HasGap(StatType.STR));
            Assert.IsTrue(summary.HasGap(StatType.WIS));
        }

        [Test]
        public void FromParty_EmptyParty_AllGaps()
        {
            var party = new CharacterInstance[0];
            var summary = PoolSummaryData.FromParty(party);
            Assert.IsTrue(summary.HasGap(StatType.STR));
            Assert.IsTrue(summary.HasGap(StatType.INT));
            Assert.AreEqual(0, summary.TotalVerbs);
        }

        [Test]
        public void FromParty_TotalVerbsCorrect()
        {
            var party = new[] { MakeChar("a", StatType.STR, StatType.DEX), MakeChar("b", StatType.WIS) };
            var summary = PoolSummaryData.FromParty(party);
            Assert.AreEqual(3, summary.TotalVerbs);
        }

        [Test]
        public void FromParty_NullCharactersSkipped()
        {
            var party = new CharacterInstance[] { MakeChar("a", StatType.STR), null };
            var summary = PoolSummaryData.FromParty(party);
            Assert.AreEqual(1, summary.TotalVerbs);
        }
    }
}
