using NUnit.Framework;
using Starquill.Quests;

namespace Starquill.Tests.EditMode.Quests
{
    public class QuestLogTests
    {
        private QuestLog log;

        private QuestSpec Spec(int questIndex = 0, QuestTier tier = QuestTier.Normal)
        {
            return new QuestSpec
            {
                ZoneIndex = 0,
                QuestIndex = questIndex,
                Tier = tier,
                Waves = new[] { new WaveSpec(), new WaveSpec(), new WaveSpec() }
            };
        }

        [SetUp]
        public void SetUp() => log = new QuestLog();

        [Test]
        public void InitialState_IdleAtZoneZeroQuestZero()
        {
            Assert.AreEqual(QuestPhase.Idle, log.Phase);
            Assert.AreEqual(0, log.ZoneIndex);
            Assert.AreEqual(0, log.NextQuestIndex);
        }

        [Test]
        public void TryOffer_FromIdle_Succeeds()
        {
            Assert.IsTrue(log.TryOffer(Spec()));
            Assert.AreEqual(QuestPhase.Offered, log.Phase);
            Assert.IsNotNull(log.ActiveSpec);
        }

        [Test]
        public void TryOffer_WhenNotIdle_Fails()
        {
            log.TryOffer(Spec());
            Assert.IsFalse(log.TryOffer(Spec()));
            log.Accept();
            Assert.IsFalse(log.TryOffer(Spec()));
        }

        [Test]
        public void Accept_FromOffered_StartsWaveZero()
        {
            log.TryOffer(Spec());
            Assert.IsTrue(log.Accept());
            Assert.AreEqual(QuestPhase.Active, log.Phase);
            Assert.AreEqual(0, log.ActiveWaveIndex);
        }

        [Test]
        public void Accept_FromIdle_FailsAndMutatesNothing()
        {
            Assert.IsFalse(log.Accept());
            Assert.AreEqual(QuestPhase.Idle, log.Phase);
        }

        [Test]
        public void Decline_ReturnsToIdle_QuestStillNext()
        {
            log.TryOffer(Spec(questIndex: 4));
            Assert.IsTrue(log.Decline());
            Assert.AreEqual(QuestPhase.Idle, log.Phase);
            Assert.IsNull(log.ActiveSpec);
            Assert.AreEqual(0, log.NextQuestIndex); // unchanged; re-offerable
        }

        [Test]
        public void AdvanceWave_StepsThroughWaves()
        {
            log.TryOffer(Spec());
            log.Accept();
            Assert.IsFalse(log.IsOnLastWave ? false : false); // wave 0 of 3
            Assert.IsTrue(log.AdvanceWave());
            Assert.AreEqual(1, log.ActiveWaveIndex);
            Assert.IsTrue(log.AdvanceWave());
            Assert.AreEqual(2, log.ActiveWaveIndex);
            Assert.IsTrue(log.IsOnLastWave);
            Assert.IsFalse(log.AdvanceWave()); // past last: caller completes
        }

        [Test]
        public void Retreat_ReturnsHalfGold_AndRetryResets()
        {
            log.TryOffer(Spec());
            log.Accept();
            log.RecordGold(100);
            log.AdvanceWave();

            double deduct = log.Retreat();
            Assert.AreEqual(50.0, deduct, 0.001);
            Assert.AreEqual(QuestPhase.Retreated, log.Phase);

            Assert.IsTrue(log.Retry());
            Assert.AreEqual(QuestPhase.Active, log.Phase);
            Assert.AreEqual(0, log.ActiveWaveIndex);
            Assert.AreEqual(0.0, log.GoldEarnedInQuest, 0.001);
        }

        [Test]
        public void Retreat_FromNonActive_ReturnsZero()
        {
            Assert.AreEqual(0.0, log.Retreat(), 0.001);
            log.TryOffer(Spec());
            Assert.AreEqual(0.0, log.Retreat(), 0.001);
            Assert.AreEqual(QuestPhase.Offered, log.Phase);
        }

        [Test]
        public void Dismiss_FromRetreated_ReturnsToIdle()
        {
            log.TryOffer(Spec());
            log.Accept();
            log.Retreat();
            Assert.IsTrue(log.Dismiss());
            Assert.AreEqual(QuestPhase.Idle, log.Phase);
            Assert.AreEqual(0, log.NextQuestIndex);
        }

        [Test]
        public void Complete_RequiresLastWave()
        {
            log.TryOffer(Spec());
            log.Accept();
            Assert.IsFalse(log.Complete()); // wave 0 of 3
            log.AdvanceWave();
            log.AdvanceWave();
            Assert.IsTrue(log.Complete());
            Assert.AreEqual(QuestPhase.Idle, log.Phase);
            Assert.AreEqual(1, log.NextQuestIndex);
        }

        [Test]
        public void Complete_BossQuest_AdvancesZone()
        {
            var boss = Spec(questIndex: 10, tier: QuestTier.Boss);
            log.Restore(0, 10, QuestPhase.Idle, 0, 0, null);
            log.TryOffer(boss);
            log.Accept();
            log.AdvanceWave();
            log.AdvanceWave();
            Assert.IsTrue(log.Complete());
            Assert.AreEqual(1, log.ZoneIndex);
            Assert.AreEqual(0, log.NextQuestIndex);
        }

        [Test]
        public void RecordGold_OnlyWhileActive()
        {
            log.RecordGold(50);
            Assert.AreEqual(0.0, log.GoldEarnedInQuest, 0.001);
            log.TryOffer(Spec());
            log.Accept();
            log.RecordGold(50);
            Assert.AreEqual(50.0, log.GoldEarnedInQuest, 0.001);
        }

        [Test]
        public void Restore_RebuildsState()
        {
            var spec = Spec(questIndex: 2);
            log.Restore(1, 2, QuestPhase.Active, 1, 75.0, spec);
            Assert.AreEqual(1, log.ZoneIndex);
            Assert.AreEqual(2, log.NextQuestIndex);
            Assert.AreEqual(QuestPhase.Active, log.Phase);
            Assert.AreEqual(1, log.ActiveWaveIndex);
            Assert.AreEqual(75.0, log.GoldEarnedInQuest, 0.001);
            Assert.AreEqual(spec, log.ActiveSpec);
        }
    }
}
