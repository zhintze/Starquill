using NUnit.Framework;
using Starquill.UI;

namespace Starquill.Tests.EditMode.UI
{
    public class ShopPresenterTests
    {
        [Test]
        public void Duration_MinutesAndSeconds()
        {
            Assert.AreEqual("4:32", ShopPresenter.Duration(272));
            Assert.AreEqual("0:59", ShopPresenter.Duration(59));
            Assert.AreEqual("0:00", ShopPresenter.Duration(0));
        }

        [Test]
        public void Duration_Hours()
        {
            Assert.AreEqual("2h 05m", ShopPresenter.Duration(2 * 3600 + 5 * 60));
        }

        [Test]
        public void BoostButtonLabel_States()
        {
            Assert.AreEqual("Buy · 2.5Kg", ShopPresenter.BoostButtonLabel(2500, false, 0));
            StringAssert.StartsWith("Active · ", ShopPresenter.BoostButtonLabel(2500, true, 120));
        }

        [Test]
        public void BoostBuyEnabled_Rules()
        {
            Assert.IsTrue(ShopPresenter.BoostBuyEnabled(100, 150, false));
            Assert.IsFalse(ShopPresenter.BoostBuyEnabled(100, 50, false), "unaffordable");
            Assert.IsFalse(ShopPresenter.BoostBuyEnabled(100, 500, true), "already active");
        }

        [Test]
        public void ChestLabel_ReadyAndCountdown()
        {
            Assert.AreEqual("READY", ShopPresenter.ChestLabel(1000, 1000));
            Assert.AreEqual("Opens in 1:40", ShopPresenter.ChestLabel(1100, 1000));
        }

        [Test]
        public void OfflineSummary_Format()
        {
            var s = ShopPresenter.OfflineSummary(2 * 3600 + 13 * 60, 1200);
            StringAssert.Contains("2h 13m", s);
            StringAssert.Contains("1.2K", s);
        }
    }
}
