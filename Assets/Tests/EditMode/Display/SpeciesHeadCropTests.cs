using NUnit.Framework;
using Starquill.Display;

namespace Starquill.Tests.Display
{
    public class SpeciesHeadCropTests
    {
        [Test]
        public void SpeciesDisplayData_HasHeadCropFields()
        {
            var registry = DisplayDataRegistry.Instance;
            registry.LoadAll();

            Assert.IsTrue(registry.Species.Count > 0, "Species should be loaded");

            foreach (var kvp in registry.Species)
            {
                var species = kvp.Value;
                Assert.GreaterOrEqual(species.HeadYOffset, -2f,
                    $"{kvp.Key} HeadYOffset out of range");
                Assert.LessOrEqual(species.HeadYOffset, 2f,
                    $"{kvp.Key} HeadYOffset out of range");
                Assert.Greater(species.HeadZoom, 0f,
                    $"{kvp.Key} HeadZoom should be positive");
            }
        }
    }
}
