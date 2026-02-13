using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Tests.Data
{
    public class AdvantageMatrixTests
    {
        private AdvantageMatrix matrix;

        [SetUp]
        public void SetUp() { matrix = ScriptableObject.CreateInstance<AdvantageMatrix>(); }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(matrix); }

        [Test] public void STR_Beats_DEX() => AssertStrong(StatType.STR, StatType.DEX, 1.5f);
        [Test] public void DEX_Beats_CON() => AssertStrong(StatType.DEX, StatType.CON, 1.5f);
        [Test] public void CON_Beats_STR() => AssertStrong(StatType.CON, StatType.STR, 1.5f);
        [Test] public void DEX_Weak_To_STR() => AssertWeak(StatType.DEX, StatType.STR, 0.67f);
        [Test] public void CON_Weak_To_DEX() => AssertWeak(StatType.CON, StatType.DEX, 0.67f);
        [Test] public void STR_Weak_To_CON() => AssertWeak(StatType.STR, StatType.CON, 0.67f);
        [Test] public void INT_Beats_WIS() => AssertStrong(StatType.INT, StatType.WIS, 1.5f);
        [Test] public void WIS_Beats_CHA() => AssertStrong(StatType.WIS, StatType.CHA, 1.5f);
        [Test] public void CHA_Beats_INT() => AssertStrong(StatType.CHA, StatType.INT, 1.5f);

        [Test]
        public void Physical_To_Mental_Deals_1_2x()
        {
            var result = matrix.GetMatchup(StatType.STR, StatType.INT);
            Assert.AreEqual(1.2f, result.damageMultiplier, 0.01f);
            Assert.AreEqual(0.0f, result.statusProcModifier, 0.01f);
        }

        [Test]
        public void Mental_To_Physical_Deals_0_8x_With_Guaranteed_Proc()
        {
            var result = matrix.GetMatchup(StatType.INT, StatType.STR);
            Assert.AreEqual(0.8f, result.damageMultiplier, 0.01f);
            Assert.AreEqual(1.0f, result.statusProcModifier, 0.01f);
        }

        [Test]
        public void Same_Type_Is_Neutral()
        {
            var result = matrix.GetMatchup(StatType.STR, StatType.STR);
            Assert.AreEqual(1.0f, result.damageMultiplier, 0.01f);
            Assert.AreEqual(Advantage.Neutral, result.advantage);
        }

        private void AssertStrong(StatType atk, StatType def, float expectedMult)
        {
            var result = matrix.GetMatchup(atk, def);
            Assert.AreEqual(expectedMult, result.damageMultiplier, 0.01f);
            Assert.AreEqual(Advantage.Strong, result.advantage);
        }

        private void AssertWeak(StatType atk, StatType def, float expectedMult)
        {
            var result = matrix.GetMatchup(atk, def);
            Assert.AreEqual(expectedMult, result.damageMultiplier, 0.01f);
            Assert.AreEqual(Advantage.Weak, result.advantage);
        }
    }
}
