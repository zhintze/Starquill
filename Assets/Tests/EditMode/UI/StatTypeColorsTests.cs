using NUnit.Framework;
using Starquill.Core;
using Starquill.UI;
using UnityEngine;

namespace Starquill.Tests.UI
{
    [TestFixture]
    public class StatTypeColorsTests
    {
        [Test]
        public void GetColor_STR_ReturnsRed()
        {
            Color color = StatTypeColors.GetColor(StatType.STR);
            Assert.AreEqual(0.9f, color.r, 0.01f);
            Assert.AreEqual(0.2f, color.g, 0.01f);
        }

        [Test]
        public void GetColor_AllTypesReturnDistinctColors()
        {
            StatType[] stats = { StatType.STR, StatType.DEX, StatType.CON, StatType.INT, StatType.WIS, StatType.CHA };
            var colors = new Color[stats.Length];

            for (int i = 0; i < stats.Length; i++)
                colors[i] = StatTypeColors.GetColor(stats[i]);

            for (int i = 0; i < colors.Length; i++)
            {
                for (int j = i + 1; j < colors.Length; j++)
                {
                    Assert.AreNotEqual(colors[i], colors[j],
                        $"{stats[i]} and {stats[j]} should have distinct colors");
                }
            }
        }

        [Test]
        public void GetAdvantageColor_Strong_ReturnsGreen()
        {
            Color color = StatTypeColors.GetAdvantageColor(Advantage.Strong);
            Assert.Greater(color.g, color.r);
        }

        [Test]
        public void GetAdvantageColor_Weak_ReturnsRed()
        {
            Color color = StatTypeColors.GetAdvantageColor(Advantage.Weak);
            Assert.Greater(color.r, color.g);
        }

        [Test]
        public void GetAdvantageColor_Neutral_ReturnsWhite()
        {
            Color color = StatTypeColors.GetAdvantageColor(Advantage.Neutral);
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(1f, color.g, 0.01f);
        }
    }
}
