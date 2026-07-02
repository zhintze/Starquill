using NUnit.Framework;
using Starquill.Data;
using Starquill.Exploration;
using UnityEngine;

namespace Starquill.Tests.EditMode.Exploration
{
    public class ExplorationManagerTests
    {
        private EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<EconomyConfig>();
            config.questDiscoveryRate = 0f; // isolate travel mechanics
            config.fragmentDropRate = 0f;
            config.travelWavesToDiscovery = 4;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void TravelProgress_AdvancesPerExploreWave()
        {
            var em = new ExplorationManager(config, seed: 1);
            em.ProcessWaveCleared();
            Assert.AreEqual(0.25f, em.TravelProgress, 0.001f);
            em.ProcessWaveCleared();
            Assert.AreEqual(0.5f, em.TravelProgress, 0.001f);
        }

        [Test]
        public void TravelProgress_ArrivalForcesDiscoveryAndResets()
        {
            var em = new ExplorationManager(config, seed: 1);
            int discoveries = 0;
            em.OnQuestDiscovered += () => discoveries++;

            for (int i = 0; i < 4; i++) em.ProcessWaveCleared();
            Assert.AreEqual(1, discoveries, "arrival must force a discovery");
            Assert.AreEqual(0f, em.TravelProgress, 0.001f, "resets after discovery");

            for (int i = 0; i < 3; i++) em.ProcessWaveCleared();
            Assert.AreEqual(1, discoveries, "no early discovery on the next leg");
        }

        [Test]
        public void TravelProgress_RandomDiscoveryResetsTravel()
        {
            config.questDiscoveryRate = 1f; // always fires
            var em = new ExplorationManager(config, seed: 1);
            int discoveries = 0;
            em.OnQuestDiscovered += () => discoveries++;

            em.ProcessWaveCleared();
            Assert.AreEqual(1, discoveries);
            Assert.AreEqual(0f, em.TravelProgress, 0.001f);
        }

        [Test]
        public void TravelProgress_FrozenWhileInQuest()
        {
            var em = new ExplorationManager(config, seed: 1);
            em.ProcessWaveCleared();
            float before = em.TravelProgress;

            em.EnterQuest();
            em.ProcessWaveCleared();
            Assert.AreEqual(before, em.TravelProgress, 0.001f);
        }

        [Test]
        public void RestoreProgress_ClampsTravel()
        {
            var em = new ExplorationManager(config, seed: 1);
            em.RestoreProgress(1.7f, 12f);
            Assert.AreEqual(1f, em.TravelProgress, 0.001f);
            Assert.AreEqual(12f, em.FragmentProgress, 0.001f);
        }
    }
}
