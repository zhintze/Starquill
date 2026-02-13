using NUnit.Framework;
using Starquill.Combat;

namespace Starquill.Tests.Combat
{
    public class DamageCalculatorTests
    {
        [Test]
        public void BasicDamage_NoModifiers()
        {
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 1.0f);
            Assert.AreEqual(121f, result, 0.1f);
        }

        [Test]
        public void LowStat_ReducesDamage()
        {
            float result = DamageCalculator.Calculate(55f, 1.0f, 2, 1.0f);
            Assert.AreEqual(66f, result, 0.1f);
        }

        [Test]
        public void Advantage_Multiplies_1_5x()
        {
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 1.5f);
            Assert.AreEqual(181.5f, result, 0.1f);
        }

        [Test]
        public void Disadvantage_Multiplies_0_67x()
        {
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 0.67f);
            Assert.AreEqual(81.07f, result, 0.1f);
        }

        [Test]
        public void Expose_Adds_25_Percent()
        {
            float normal = DamageCalculator.Calculate(100f, 1.0f, 10, 1.0f);
            float exposed = DamageCalculator.Calculate(100f, 1.0f, 10, 1.0f, targetIsExposed: true);
            Assert.AreEqual(normal * 1.25f, exposed, 0.1f);
        }

        [Test]
        public void FullCombo_Advantage_Plus_Expose()
        {
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 1.5f, targetIsExposed: true);
            Assert.AreEqual(226.875f, result, 0.1f);
        }

        [Test] public void StatProcMod_High() => Assert.AreEqual(1.0f, DamageCalculator.GetStatProcModifier(8));
        [Test] public void StatProcMod_Moderate() => Assert.AreEqual(0.75f, DamageCalculator.GetStatProcModifier(6));
        [Test] public void StatProcMod_Weak() => Assert.AreEqual(0.5f, DamageCalculator.GetStatProcModifier(4));
        [Test] public void StatProcMod_Dump() => Assert.AreEqual(0.25f, DamageCalculator.GetStatProcModifier(1));
    }
}
