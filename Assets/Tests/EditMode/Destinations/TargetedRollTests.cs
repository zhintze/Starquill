using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.Tests.Destinations
{
    public class TargetedRollTests
    {
        [Test]
        public void GenerateStatPair_ForcedPair_UsesBothStats()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 50; i++)
            {
                var (p, pv, s, sv) = EquipmentFactory.GenerateStatPair(
                    "tr", Rarity.Rare, rng, questLevel: 10,
                    forcedStatA: StatType.INT, forcedStatB: StatType.CHA);
                Assert.IsTrue((p == StatType.INT && s == StatType.CHA)
                           || (p == StatType.CHA && s == StatType.INT));
                Assert.Greater(pv, sv);
            }
        }

        [Test]
        public void GenerateStatPair_NoForcing_UnchangedBehavior()
        {
            var rng = new System.Random(3);
            var (p, pv, s, sv) = EquipmentFactory.GenerateStatPair("tr", Rarity.Common, rng);
            Assert.AreNotEqual(p, s);
            Assert.Greater(pv, sv);
        }

        [Test]
        public void RollRarity_LegendaryWeightMultiplier_RaisesLegendaryRate()
        {
            int baseline = CountLegendaries(mult: 1f), boosted = CountLegendaries(mult: 10f);
            Assert.Greater(boosted, baseline);
        }

        private static int CountLegendaries(float mult)
        {
            var rng = new System.Random(9);
            int count = 0;
            for (int i = 0; i < 20000; i++)
                if (EquipmentFactory.RollRarity(50, rng, mult) == Rarity.Legendary) count++;
            return count;
        }
    }
}
