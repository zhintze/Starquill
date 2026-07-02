using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyInstanceTests
    {
        [Test]
        public void DisplayName_BaseColorKey()
        {
            var key = new KeyInstance { ColorFamily = ColorFamily.Brown, Difficulty = 1 };
            Assert.AreEqual("Brown Key", key.DisplayName);
        }

        [Test]
        public void DisplayName_TripleFused()
        {
            var key = new KeyInstance
            {
                ColorFamily = ColorFamily.Brown,
                Slot = KeySlot.Head,
                ArchetypeId = 0, // Warrior
                Difficulty = 3
            };
            Assert.AreEqual("Brown Warrior Helm Key", key.DisplayName);
        }

        [Test]
        public void ModifierCount_CountsSetModifiers()
        {
            Assert.AreEqual(1, new KeyInstance { Slot = KeySlot.Weapon }.ModifierCount);
            Assert.AreEqual(2, new KeyInstance { ColorFamily = ColorFamily.Red, ArchetypeId = 4 }.ModifierCount);
        }
    }
}
