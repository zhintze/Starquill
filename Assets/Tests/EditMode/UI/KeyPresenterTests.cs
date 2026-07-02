using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Destinations;
using Starquill.UI;
using UnityEngine;

namespace Starquill.Tests.EditMode.UI
{
    public class KeyPresenterTests
    {
        private EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            // Field initializers carry the shipped defaults (dungeonDurationBase
            // 150, dungeonDurationPerD 20, keySellBase 25, keyMaxDifficulty 6).
            config = ScriptableObject.CreateInstance<EconomyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null) Object.DestroyImmediate(config);
        }

        [Test]
        public void Title_PassesThroughDisplayName()
        {
            var key = new KeyInstance { ColorFamily = ColorFamily.Red, Slot = KeySlot.Weapon };
            Assert.AreEqual("Red Weapon Key", KeyPresenter.Title(key));
            Assert.AreEqual(key.DisplayName, KeyPresenter.Title(key));
        }

        [Test]
        public void SubLine_Difficulty1()
        {
            var key = new KeyInstance { Difficulty = 1 };
            Assert.AreEqual("Difficulty 1 · Uncommon floor · 2:30 rush",
                KeyPresenter.SubLine(key, config));
        }

        [Test]
        public void SubLine_Difficulty3()
        {
            var key = new KeyInstance { Difficulty = 3 };
            Assert.AreEqual("Difficulty 3 · Epic floor · 3:10 rush",
                KeyPresenter.SubLine(key, config));
        }

        [Test]
        public void AccentColor_SlotOnlyKey_FallsBackToGray()
        {
            var key = new KeyInstance { Slot = KeySlot.Head };
            var c = KeyPresenter.AccentColor(key);
            Assert.AreEqual(0.55f, c.r, 0.001f);
            Assert.AreEqual(0.55f, c.g, 0.001f);
            Assert.AreEqual(0.55f, c.b, 0.001f);
        }

        [Test]
        public void AccentColor_NeutralFamily_SameGray()
        {
            var key = new KeyInstance { ColorFamily = ColorFamily.Neutral };
            Assert.AreEqual(KeyPresenter.AccentColor(new KeyInstance()),
                KeyPresenter.AccentColor(key));
        }

        [Test]
        public void AccentColor_ColorFamily_NotGray()
        {
            var key = new KeyInstance { ColorFamily = ColorFamily.Green };
            var c = KeyPresenter.AccentColor(key);
            Assert.AreNotEqual(c.r, c.g);
        }

        [Test]
        public void DifficultyPips_PassesThroughDifficulty()
        {
            Assert.AreEqual(4, KeyPresenter.DifficultyPips(new KeyInstance { Difficulty = 4 }));
        }

        [Test]
        public void SellText_ThousandsSeparated()
        {
            // 25 base x questLevel 10 x D5 = 1,250
            var key = new KeyInstance { Difficulty = 5 };
            Assert.AreEqual("SELL +1,250g", KeyPresenter.SellText(key, 10, config));
        }
    }
}
