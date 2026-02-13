using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Tests.Data
{
    public class StatsTests
    {
        [Test]
        public void GetStat_ReturnsCorrectValue()
        {
            var stats = new Stats { STR = 12, DEX = 3, CON = 8, INT = 2, WIS = 3, CHA = 2 };
            Assert.AreEqual(12, stats.GetStat(StatType.STR));
            Assert.AreEqual(3, stats.GetStat(StatType.DEX));
            Assert.AreEqual(8, stats.GetStat(StatType.CON));
        }

        [Test]
        public void Total_SumsAllStats()
        {
            var stats = new Stats { STR = 12, DEX = 3, CON = 8, INT = 2, WIS = 3, CHA = 2 };
            Assert.AreEqual(30, stats.Total);
        }

        [Test]
        public void HighestStat_ReturnsCorrectType()
        {
            var stats = new Stats { STR = 12, DEX = 3, CON = 8, INT = 2, WIS = 3, CHA = 2 };
            Assert.AreEqual(StatType.STR, stats.HighestStat());
        }

        [Test]
        public void Addition_CombinesStats()
        {
            var a = new Stats { STR = 10, DEX = 5, CON = 3, INT = 2, WIS = 1, CHA = 1 };
            var b = new Stats { STR = 2, DEX = 0, CON = 5, INT = 0, WIS = 0, CHA = 3 };
            var result = a + b;
            Assert.AreEqual(12, result.STR);
            Assert.AreEqual(5, result.DEX);
            Assert.AreEqual(8, result.CON);
            Assert.AreEqual(4, result.CHA);
        }
    }
}
