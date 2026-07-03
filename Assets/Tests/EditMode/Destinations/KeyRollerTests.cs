using NUnit.Framework;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyRollerTests
    {
        [Test]
        public void Roll_AlwaysProducesExactlyOneModifier()
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 200; i++)
                Assert.AreEqual(1, KeyRoller.Roll(rng).ModifierCount);
        }

        [Test]
        public void Roll_DifficultyDistribution_MatchesWeights()
        {
            var rng = new System.Random(11);
            int d1 = 0;
            for (int i = 0; i < 2000; i++)
                if (KeyRoller.Roll(rng).Difficulty == 1) d1++;
            Assert.Greater(d1, 1500); // 85% expected, generous tolerance
        }

        [Test]
        public void Roll_ProducesAllThreeKinds()
        {
            var rng = new System.Random(11);
            bool color = false, slot = false, cls = false;
            for (int i = 0; i < 500; i++)
            {
                var key = KeyRoller.Roll(rng);
                color |= key.ColorFamily.HasValue;
                slot |= key.Slot.HasValue;
                cls |= key.ArchetypeId >= 0;
            }
            Assert.IsTrue(color && slot && cls);
        }

        [Test]
        public void Roll_NeverMintsPastelKeys()
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 2000; i++)
            {
                var key = KeyRoller.Roll(rng);
                if (key.ColorFamily.HasValue)
                    Assert.AreNotEqual(Starquill.Core.ColorFamily.Pastel, key.ColorFamily.Value);
            }
        }
    }
}
