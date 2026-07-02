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

        [Test]
        public void FusionPreview_ValidPair_TitleAndCost()
        {
            var a = new KeyInstance { ColorFamily = ColorFamily.Red, Difficulty = 1 };
            var b = new KeyInstance { Slot = KeySlot.Weapon, Difficulty = 2 };
            var p = KeyPresenter.FusionPreview(a, b, questLevel: 4, config);
            Assert.IsTrue(p.Valid);
            Assert.AreEqual("Red Weapon Key", p.ResultTitle);
            Assert.AreEqual(3, p.ResultDifficulty);
            // 250 base x questLevel 4 x D3^2 = 9,000
            Assert.AreEqual("9,000g", p.CostText);
            Assert.AreEqual("", p.InvalidReason);
        }

        [Test]
        public void FusionPreview_ModifierConflict_Invalid()
        {
            var a = new KeyInstance { ColorFamily = ColorFamily.Red };
            var b = new KeyInstance { ColorFamily = ColorFamily.Blue };
            var p = KeyPresenter.FusionPreview(a, b, 1, config);
            Assert.IsFalse(p.Valid);
            Assert.AreEqual("Same kind, different variant", p.InvalidReason);
        }

        [Test]
        public void FusionPreview_DifficultyCap_Invalid()
        {
            var a = new KeyInstance { Difficulty = 3 };
            var b = new KeyInstance { Difficulty = 4 };
            var p = KeyPresenter.FusionPreview(a, b, 1, config);
            Assert.IsFalse(p.Valid);
            Assert.AreEqual("Result would exceed difficulty 6", p.InvalidReason);
        }

        [Test]
        public void FusionPreview_Unaffordable_StillPreviewable()
        {
            // Affordability is the view's concern: a huge quest level keeps
            // the preview valid regardless of the player's gold.
            var a = new KeyInstance { ColorFamily = ColorFamily.Green, Difficulty = 3 };
            var b = new KeyInstance { ColorFamily = ColorFamily.Green, Difficulty = 3 };
            var p = KeyPresenter.FusionPreview(a, b, questLevel: 1000, config);
            Assert.IsTrue(p.Valid);
            Assert.Greater(p.Cost, 1_000_000d);
            Assert.AreEqual("9,000,000g", p.CostText);
        }
    }
}
