using NUnit.Framework;
using Starquill.Quests;

namespace Starquill.Tests.Destinations
{
    public class QuestKeyRewardTests
    {
        [Test]
        public void ForTier_KeyPayouts_MatchDesign()
        {
            Assert.AreEqual(0, QuestRewardSpec.ForTier(QuestTier.Normal).KeyDrops);
            var elite = QuestRewardSpec.ForTier(QuestTier.Elite);
            Assert.AreEqual(1, elite.KeyDrops);
            Assert.AreEqual(1, elite.KeyDifficulty);
            var hard = QuestRewardSpec.ForTier(QuestTier.Hard);
            Assert.AreEqual(1, hard.KeyDrops);
            Assert.AreEqual(0.25f, hard.ExtraKeyChance, 0.001f);
            var boss = QuestRewardSpec.ForTier(QuestTier.Boss);
            Assert.AreEqual(1, boss.KeyDrops);
            Assert.AreEqual(2, boss.KeyDifficulty);
        }
    }
}
