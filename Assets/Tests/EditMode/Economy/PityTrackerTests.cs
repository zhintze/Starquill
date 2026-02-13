using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Economy
{
    public class PityTrackerTests
    {
        private PityTracker tracker;
        private EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            tracker = new PityTracker();
            config = ScriptableObject.CreateInstance<EconomyConfig>();
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(config); }

        [Test]
        public void NoGuarantee_BeforePityThreshold()
        {
            for (int i = 0; i < 49; i++)
                Assert.IsNull(tracker.RegisterKill(config));
        }

        [Test]
        public void GuaranteesUncommon_At50Kills()
        {
            Rarity? result = null;
            for (int i = 0; i < 50; i++) result = tracker.RegisterKill(config);
            Assert.AreEqual(Rarity.Uncommon, result);
        }

        [Test]
        public void RegisterDrop_ResetsPityCounters()
        {
            for (int i = 0; i < 40; i++) tracker.RegisterKill(config);
            tracker.RegisterDrop(Rarity.Uncommon);
            Assert.AreEqual(0, tracker.killsSinceUncommon);
            for (int i = 0; i < 49; i++) Assert.IsNull(tracker.RegisterKill(config));
        }
    }
}
