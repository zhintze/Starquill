using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyFusionTests
    {
        private static KeyInstance Color(ColorFamily f, int d = 1) => new() { ColorFamily = f, Difficulty = d };
        private static KeyInstance Slot(KeySlot s, int d = 1) => new() { Slot = s, Difficulty = d };
        private static KeyInstance Class(int id, int d = 1) => new() { ArchetypeId = id, Difficulty = d };

        [Test]
        public void DifferentKinds_CombineModifiers_SumDifficulty()
        {
            var result = KeyFusion.Fuse(Color(ColorFamily.Brown), Slot(KeySlot.Head));
            Assert.AreEqual(ColorFamily.Brown, result.ColorFamily);
            Assert.AreEqual(KeySlot.Head, result.Slot);
            Assert.AreEqual(2, result.Difficulty);
        }

        [Test]
        public void SameVariant_Merges_AsUpgrade()
        {
            var result = KeyFusion.Fuse(Color(ColorFamily.Brown), Color(ColorFamily.Brown));
            Assert.AreEqual(ColorFamily.Brown, result.ColorFamily);
            Assert.AreEqual(2, result.Difficulty);
            Assert.AreEqual(1, result.ModifierCount);
        }

        [Test]
        public void SameKind_DifferentVariant_IsInvalid()
        {
            Assert.IsFalse(KeyFusion.CanFuse(Color(ColorFamily.Brown), Color(ColorFamily.Red), maxDifficulty: 6));
            Assert.IsFalse(KeyFusion.CanFuse(Slot(KeySlot.Head), Slot(KeySlot.Feet), maxDifficulty: 6));
            Assert.IsFalse(KeyFusion.CanFuse(Class(0), Class(1), maxDifficulty: 6));
        }

        [Test]
        public void FusedKeys_WithDistinctKinds_FuseAgain()
        {
            var brownHelm = KeyFusion.Fuse(Color(ColorFamily.Brown), Slot(KeySlot.Head));
            Assert.IsTrue(KeyFusion.CanFuse(brownHelm, Class(0), maxDifficulty: 6));
            var triple = KeyFusion.Fuse(brownHelm, Class(0));
            Assert.AreEqual(3, triple.Difficulty);
            Assert.AreEqual(3, triple.ModifierCount);
        }

        [Test]
        public void DifficultyCap_BlocksFusion()
        {
            Assert.IsFalse(KeyFusion.CanFuse(Color(ColorFamily.Brown, 4), Slot(KeySlot.Head, 3), maxDifficulty: 6));
        }

        [Test]
        public void Cost_IsQuadraticInResultDifficulty()
        {
            // base 250, questLevel 40, D3 result -> 250*40*9 = 90,000 (design §2)
            double cost = KeyFusion.Cost(baseCost: 250f, questLevel: 40, resultDifficulty: 3);
            Assert.AreEqual(90000, cost, 0.01);
        }
    }
}
