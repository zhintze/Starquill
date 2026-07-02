using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyPouchTests
    {
        [Test]
        public void Add_Remove_Count()
        {
            var pouch = new KeyPouch();
            var key = new KeyInstance { ColorFamily = ColorFamily.Red };
            pouch.Add(key);
            Assert.AreEqual(1, pouch.Keys.Count);
            Assert.IsTrue(pouch.Remove(key));
            Assert.AreEqual(0, pouch.Keys.Count);
            Assert.IsFalse(pouch.Remove(key));
        }

        [Test]
        public void IsOverSoftCap()
        {
            var pouch = new KeyPouch();
            for (int i = 0; i < 30; i++) pouch.Add(new KeyInstance());
            Assert.IsTrue(pouch.IsOverSoftCap(30));
            Assert.IsFalse(pouch.IsOverSoftCap(31));
        }

        [Test]
        public void SellValue_ScalesWithDifficultyAndQuestLevel()
        {
            double d1 = KeyPouch.SellValue(new KeyInstance { Difficulty = 1 }, questLevel: 10, sellBase: 25f);
            double d3 = KeyPouch.SellValue(new KeyInstance { Difficulty = 3 }, questLevel: 10, sellBase: 25f);
            Assert.AreEqual(250, d1, 0.01);
            Assert.AreEqual(750, d3, 0.01);
        }
    }
}
