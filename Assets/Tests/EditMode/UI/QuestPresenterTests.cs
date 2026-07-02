using NUnit.Framework;
using Starquill.Quests;
using Starquill.UI;

namespace Starquill.Tests.EditMode.UI
{
    public class QuestPresenterTests
    {
        private QuestSpec Spec(int waves = 4, QuestTier tier = QuestTier.Normal, bool lastIsBoss = false)
        {
            var waveSpecs = new WaveSpec[waves];
            for (int i = 0; i < waves; i++) waveSpecs[i] = new WaveSpec();
            if (lastIsBoss) waveSpecs[waves - 1].IsBossWave = true;
            return new QuestSpec
            {
                Tier = tier,
                DisplayName = "Verdant Hollow 3/11",
                Waves = waveSpecs,
                Reward = QuestRewardSpec.ForTier(tier)
            };
        }

        [Test]
        public void BannerText_Offered()
        {
            Assert.AreEqual("Quest discovered · tap to view",
                QuestPresenter.BannerText(QuestPhase.Offered, Spec(), 0));
        }

        [Test]
        public void BannerText_Retreated_NamesQuest()
        {
            var text = QuestPresenter.BannerText(QuestPhase.Retreated, Spec(), 0);
            Assert.IsTrue(text.Contains("Retry"));
            Assert.IsTrue(text.Contains("Verdant Hollow 3/11"));
        }

        [Test]
        public void BannerText_Active_ShowsWaveProgress()
        {
            var text = QuestPresenter.BannerText(QuestPhase.Active, Spec(waves: 5), 1);
            Assert.IsTrue(text.Contains("Wave 2/5"), text);
        }

        [Test]
        public void BannerText_ActiveBossWave_SaysBoss()
        {
            var text = QuestPresenter.BannerText(QuestPhase.Active, Spec(waves: 3, lastIsBoss: true), 2);
            Assert.IsTrue(text.Contains("BOSS"), text);
        }

        [Test]
        public void BannerText_IdleOrNullSpec_Empty()
        {
            Assert.AreEqual("", QuestPresenter.BannerText(QuestPhase.Idle, Spec(), 0));
            Assert.AreEqual("", QuestPresenter.BannerText(QuestPhase.Active, null, 0));
        }

        [Test]
        public void WaveFraction_BoundsAndProgress()
        {
            var spec = Spec(waves: 5);
            Assert.AreEqual(0f, QuestPresenter.WaveFraction(spec, 0), 0.001f);
            Assert.AreEqual(0.5f, QuestPresenter.WaveFraction(spec, 2), 0.001f);
            Assert.AreEqual(1f, QuestPresenter.WaveFraction(spec, 4), 0.001f);
            Assert.AreEqual(0f, QuestPresenter.WaveFraction(null, 3), 0.001f);
        }

        [Test]
        public void RewardsPreview_WithFloor()
        {
            var text = QuestPresenter.RewardsPreview(QuestRewardSpec.ForTier(QuestTier.Elite), 450);
            Assert.IsTrue(text.Contains("Uncommon+"), text);
            Assert.IsTrue(text.Contains("450"), text);
        }

        [Test]
        public void RewardsPreview_NoFloor()
        {
            var text = QuestPresenter.RewardsPreview(QuestRewardSpec.ForTier(QuestTier.Normal), 100);
            Assert.IsFalse(text.Contains("+ item"), text);
            Assert.IsTrue(text.ToLower().Contains("loot"), text);
        }

        [Test]
        public void RewardsPreview_ExtraRoll_MentionsBonus()
        {
            var text = QuestPresenter.RewardsPreview(QuestRewardSpec.ForTier(QuestTier.Hard), 800);
            Assert.IsTrue(text.Contains("bonus"), text);
        }

        [Test]
        public void LadderStates_FreshZone()
        {
            var states = QuestPresenter.LadderStates(0, QuestPhase.Idle);
            Assert.AreEqual(11, states.Length);
            Assert.AreEqual(LadderNodeState.Current, states[0]);
            Assert.AreEqual(LadderNodeState.Ahead, states[1]);
            Assert.AreEqual(LadderNodeState.Boss, states[10]);
        }

        [Test]
        public void LadderStates_MidZone()
        {
            var states = QuestPresenter.LadderStates(4, QuestPhase.Active);
            for (int i = 0; i < 4; i++) Assert.AreEqual(LadderNodeState.Done, states[i], $"node {i}");
            Assert.AreEqual(LadderNodeState.Current, states[4]);
            Assert.AreEqual(LadderNodeState.Boss, states[10]);
        }

        [Test]
        public void LadderStates_BossCurrent()
        {
            var states = QuestPresenter.LadderStates(10, QuestPhase.Offered);
            Assert.AreEqual(LadderNodeState.BossCurrent, states[10]);
            for (int i = 0; i < 10; i++) Assert.AreEqual(LadderNodeState.Done, states[i]);
        }

        [Test]
        public void PickDialogue_ModuloAndSafety()
        {
            var lines = new[] { "a", "b", "c" };
            Assert.AreEqual("a", QuestPresenter.PickDialogue(lines, 0));
            Assert.AreEqual("b", QuestPresenter.PickDialogue(lines, 4));
            Assert.AreEqual("", QuestPresenter.PickDialogue(null, 2));
            Assert.AreEqual("", QuestPresenter.PickDialogue(new string[0], 2));
        }

        [Test]
        public void TierLabel_AllTiers()
        {
            Assert.AreEqual("Normal", QuestPresenter.TierLabel(QuestTier.Normal));
            Assert.AreEqual("Elite", QuestPresenter.TierLabel(QuestTier.Elite));
            Assert.AreEqual("Hard", QuestPresenter.TierLabel(QuestTier.Hard));
            Assert.AreEqual("BOSS", QuestPresenter.TierLabel(QuestTier.Boss));
        }
    }
}
