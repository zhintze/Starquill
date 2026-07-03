using NUnit.Framework;
using Starquill.Core;

namespace Starquill.Tests.Destinations
{
    public class ColorFamilyTests
    {
        [TestCase(1f, 0f, 0f, ColorFamily.Red)]
        [TestCase(0.18f, 0.02f, 0.02f, ColorFamily.Red)]     // saturated dark red reads red, not black
        [TestCase(0.45f, 0.22f, 0.06f, ColorFamily.Brown)]   // dark saddle brown (dark warm)
        [TestCase(0.82f, 0.71f, 0.55f, ColorFamily.Brown)]   // tan: muted mid warm
        [TestCase(0.5f, 0.5f, 0f, ColorFamily.Brown)]        // olive: dark yellow hue reads brown/drab
        [TestCase(1f, 0.55f, 0f, ColorFamily.Orange)]        // bright orange
        [TestCase(1f, 0.9f, 0.1f, ColorFamily.Yellow)]
        [TestCase(0.1f, 0.7f, 0.2f, ColorFamily.Green)]
        [TestCase(0.1f, 0.3f, 0.9f, ColorFamily.Blue)]
        [TestCase(0.6f, 0.1f, 0.8f, ColorFamily.Purple)]
        [TestCase(0.5f, 0.5f, 0.5f, ColorFamily.Neutral)]    // true gray: low sat, mid value
        [TestCase(0.05f, 0.05f, 0.05f, ColorFamily.Black)]   // near-black: low value
        [TestCase(0.15f, 0.14f, 0.13f, ColorFamily.Black)]   // dark AND drab reads black
        [TestCase(1f, 1f, 1f, ColorFamily.White)]            // white
        [TestCase(0.8f, 0.8f, 0.82f, ColorFamily.White)]     // silver: low sat, high value
        public void Classify_KnownColors(float r, float g, float b, ColorFamily expected)
        {
            Assert.AreEqual(expected, ColorFamilyClassifier.Classify(r, g, b));
        }

        [Test]
        public void Classify_IsTotal_OverRandomColors()
        {
            var rng = new System.Random(42);
            for (int i = 0; i < 1000; i++)
            {
                float r = (float)rng.NextDouble(), g = (float)rng.NextDouble(), b = (float)rng.NextDouble();
                var family = ColorFamilyClassifier.Classify(r, g, b);
                Assert.IsTrue(System.Enum.IsDefined(typeof(ColorFamily), family));
            }
        }
    }
}
