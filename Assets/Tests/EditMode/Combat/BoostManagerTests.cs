using NUnit.Framework;
using Starquill.Combat;

namespace Starquill.Tests.EditMode.Combat
{
    public class BoostManagerTests
    {
        [Test]
        public void InactiveByDefault()
        {
            var bm = new BoostManager();
            Assert.IsFalse(bm.IsActive(BoostType.AutoFireVerbs, 1000));
            Assert.AreEqual(0, bm.Remaining(BoostType.AutoFireVerbs, 1000), 0.001);
        }

        [Test]
        public void Activate_MakesActiveUntilExpiry()
        {
            var bm = new BoostManager();
            bm.Activate(BoostType.VerbSpeedUp, 300, now: 1000);
            Assert.IsTrue(bm.IsActive(BoostType.VerbSpeedUp, 1000));
            Assert.IsTrue(bm.IsActive(BoostType.VerbSpeedUp, 1299));
            Assert.IsFalse(bm.IsActive(BoostType.VerbSpeedUp, 1300));
        }

        [Test]
        public void Activate_WhileActive_ExtendsFromExpiry()
        {
            var bm = new BoostManager();
            bm.Activate(BoostType.AutoFireVerbs, 300, now: 1000);
            bm.Activate(BoostType.AutoFireVerbs, 300, now: 1100); // 200s left
            Assert.AreEqual(500, bm.Remaining(BoostType.AutoFireVerbs, 1100), 0.001);
        }

        [Test]
        public void Activate_AfterExpiry_StartsFromNow()
        {
            var bm = new BoostManager();
            bm.Activate(BoostType.AutoFireVerbs, 300, now: 1000);
            bm.Activate(BoostType.AutoFireVerbs, 300, now: 2000);
            Assert.AreEqual(300, bm.Remaining(BoostType.AutoFireVerbs, 2000), 0.001);
        }

        [Test]
        public void Remaining_CountsDown()
        {
            var bm = new BoostManager();
            bm.Activate(BoostType.VerbSpeedUp, 300, now: 1000);
            Assert.AreEqual(100, bm.Remaining(BoostType.VerbSpeedUp, 1200), 0.001);
        }

        [Test]
        public void RestoreExpiry_RoundTrips()
        {
            var bm = new BoostManager();
            bm.RestoreExpiry(BoostType.AutoFireVerbs, 5000);
            Assert.AreEqual(5000, bm.GetExpiry(BoostType.AutoFireVerbs), 0.001);
            Assert.IsTrue(bm.IsActive(BoostType.AutoFireVerbs, 4999));
        }

        [Test]
        public void BoostTypes_Independent()
        {
            var bm = new BoostManager();
            bm.Activate(BoostType.AutoFireVerbs, 300, now: 1000);
            Assert.IsFalse(bm.IsActive(BoostType.VerbSpeedUp, 1000));
        }
    }
}
