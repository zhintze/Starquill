using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;
using Starquill.Managers;

namespace Starquill.Tests.Destinations
{
    public class KeySaveTests
    {
        [Test]
        public void SerializedKey_RoundTrips()
        {
            var key = new KeyInstance
            { ColorFamily = ColorFamily.Brown, Slot = KeySlot.Head, ArchetypeId = 3, Difficulty = 4 };
            var restored = SerializedKey.FromInstance(key).ToInstance();
            Assert.AreEqual(key.ColorFamily, restored.ColorFamily);
            Assert.AreEqual(key.Slot, restored.Slot);
            Assert.AreEqual(key.ArchetypeId, restored.ArchetypeId);
            Assert.AreEqual(key.Difficulty, restored.Difficulty);
        }

        [Test]
        public void SerializedKey_NullModifiers_RoundTripAsNull()
        {
            var key = new KeyInstance { ArchetypeId = 7 };
            var restored = SerializedKey.FromInstance(key).ToInstance();
            Assert.IsFalse(restored.ColorFamily.HasValue);
            Assert.IsFalse(restored.Slot.HasValue);
        }
    }
}
