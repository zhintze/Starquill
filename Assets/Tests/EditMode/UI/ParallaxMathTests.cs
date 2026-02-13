using NUnit.Framework;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    [TestFixture]
    public class ParallaxMathTests
    {
        [Test]
        public void CalculateUVWidth_ReturnsRatioOfDisplayToTexture()
        {
            float result = ParallaxMath.CalculateUVWidth(1080f, 2160f);
            Assert.AreEqual(0.5f, result, 0.001f);
        }

        [Test]
        public void CalculateUVWidth_SameSizeReturnsOne()
        {
            float result = ParallaxMath.CalculateUVWidth(1080f, 1080f);
            Assert.AreEqual(1f, result, 0.001f);
        }

        [Test]
        public void CalculateUVWidth_ZeroTextureWidthReturnsOne()
        {
            float result = ParallaxMath.CalculateUVWidth(1080f, 0f);
            Assert.AreEqual(1f, result, 0.001f);
        }

        [Test]
        public void AdvanceOffset_MovesForward()
        {
            float result = ParallaxMath.AdvanceOffset(0f, 30f, 1f, 2160f);
            Assert.AreEqual(30f / 2160f, result, 0.0001f);
        }

        [Test]
        public void AdvanceOffset_WrapsAtOne()
        {
            float result = ParallaxMath.AdvanceOffset(0.95f, 2160f, 1f, 2160f);
            Assert.AreEqual(0.95f, result, 0.001f);
        }

        [Test]
        public void AdvanceOffset_HandlesNegativeSpeed()
        {
            float result = ParallaxMath.AdvanceOffset(0.1f, -30f, 1f, 2160f);
            float expected = 0.1f - 30f / 2160f;
            Assert.AreEqual(expected, result, 0.0001f);
        }

        [Test]
        public void AdvanceOffset_NegativeWrapsToPositive()
        {
            float result = ParallaxMath.AdvanceOffset(0.01f, -2160f, 1f, 2160f);
            Assert.AreEqual(0.01f, result, 0.001f);
        }

        [Test]
        public void AdvanceOffset_ZeroTextureWidthReturnsCurrentOffset()
        {
            float result = ParallaxMath.AdvanceOffset(0.5f, 30f, 1f, 0f);
            Assert.AreEqual(0.5f, result, 0.001f);
        }
    }
}
