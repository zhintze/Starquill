using NUnit.Framework;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    [TestFixture]
    public class NumberFormatterTests
    {
        [Test]
        public void Format_SmallNumber_NoSuffix()
        {
            Assert.AreEqual("999", NumberFormatter.FormatCompact(999));
        }

        [Test]
        public void Format_Thousands_UseK()
        {
            Assert.AreEqual("1.2K", NumberFormatter.FormatCompact(1234));
        }

        [Test]
        public void Format_Millions_UseM()
        {
            Assert.AreEqual("5.6M", NumberFormatter.FormatCompact(5600000));
        }

        [Test]
        public void Format_Billions_UseB()
        {
            Assert.AreEqual("1.0B", NumberFormatter.FormatCompact(1000000000));
        }

        [Test]
        public void Format_ExactThousand()
        {
            Assert.AreEqual("1.0K", NumberFormatter.FormatCompact(1000));
        }

        [Test]
        public void Format_Zero()
        {
            Assert.AreEqual("0", NumberFormatter.FormatCompact(0));
        }

        [Test]
        public void FormatGold_AddsPrefix()
        {
            Assert.AreEqual("+5g", NumberFormatter.FormatGoldDrop(5));
        }

        [Test]
        public void FormatGold_LargeAmount()
        {
            Assert.AreEqual("+1.2Kg", NumberFormatter.FormatGoldDrop(1200));
        }
    }
}
