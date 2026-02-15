using NUnit.Framework;
using Starquill.Characters;
using Starquill.Data;

namespace Starquill.Tests.Characters
{
    public class CharacterInstanceTests
    {
        [Test]
        public void XpToNextLevel_Level1_Returns118()
        {
            var c = CreateCharacter(1);
            Assert.AreEqual(118, c.XpToNextLevel());
        }

        [Test]
        public void XpToNextLevel_Level10_Returns523()
        {
            var c = CreateCharacter(10);
            Assert.AreEqual(523, c.XpToNextLevel());
        }

        [Test]
        public void CanLevelUp_NotEnoughXp_ReturnsFalse()
        {
            var c = CreateCharacter(1);
            c.xp = 50;
            Assert.IsFalse(c.CanLevelUp());
        }

        [Test]
        public void CanLevelUp_EnoughXp_ReturnsTrue()
        {
            var c = CreateCharacter(1);
            c.xp = 200;
            Assert.IsTrue(c.CanLevelUp());
        }

        [Test]
        public void LevelUp_IncrementsLevel()
        {
            var c = CreateCharacter(1);
            c.xp = 200;
            c.LevelUp();
            Assert.AreEqual(2, c.level);
        }

        [Test]
        public void LevelUp_SubtractsXpCost()
        {
            var c = CreateCharacter(1);
            c.xp = 200;
            int cost = c.XpToNextLevel(); // 118
            c.LevelUp();
            Assert.AreEqual(200 - cost, c.xp);
        }

        [Test]
        public void LevelUp_DistributesStatsWeightedByBase()
        {
            // STR-heavy species: STR=10, DEX=5, others=3. Total=30
            var c = new CharacterInstance
            {
                id = "test", displayName = "Test", level = 1, xp = 9999,
                baseStats = new Stats { STR = 10, DEX = 5, CON = 3, INT = 3, WIS = 6, CHA = 3 }
            };

            // Level up 30 times to get statistically meaningful distribution
            for (int i = 0; i < 30; i++)
            {
                c.xp = 999999;
                c.LevelUp();
            }

            // STR should get the most allocation, DEX and WIS moderate, others least
            Assert.Greater(c.allocatedStats.STR, c.allocatedStats.CON,
                "STR (weight 10) should gain more than CON (weight 3)");
            Assert.Greater(c.allocatedStats.STR, c.allocatedStats.DEX,
                "STR (weight 10) should gain more than DEX (weight 5)");
        }

        [Test]
        public void LevelUp_NotEnoughXp_DoesNothing()
        {
            var c = CreateCharacter(1);
            c.xp = 10;
            c.LevelUp();
            Assert.AreEqual(1, c.level);
            Assert.AreEqual(10, c.xp);
        }

        [Test]
        public void LevelUp_DistributesAbout3PointsPerLevel()
        {
            var c = CreateCharacter(1);
            int levels = 10;
            var totalBefore = c.allocatedStats.Total;
            for (int i = 0; i < levels; i++)
            {
                c.xp = 999999;
                c.LevelUp();
            }
            var totalAfter = c.allocatedStats.Total;
            int totalGained = totalAfter - totalBefore;
            // Over 10 levels, should gain ~30 points (3 per level)
            Assert.GreaterOrEqual(totalGained, 25, $"Expected ~30 points over {levels} levels, got {totalGained}");
            Assert.LessOrEqual(totalGained, 35, $"Expected ~30 points over {levels} levels, got {totalGained}");
        }

        private CharacterInstance CreateCharacter(int level)
        {
            return new CharacterInstance
            {
                id = "test", displayName = "Test", level = level,
                baseStats = new Stats { STR = 5, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
        }
    }
}
