using NUnit.Framework;
using UnityEngine;
using Starquill.UI;

namespace Starquill.Tests.EditMode.UI
{
    public class ItemIconFramingTests
    {
        [Test]
        public void GetFrame_ArmorPrefixes_ReturnCategoryFrame()
        {
            foreach (var itemType in new[] { "hd01", "tr03", "ar02", "lg01", "fe01", "mc04" })
            {
                var frame = ItemIconFraming.GetFrame(itemType);
                Assert.Less(frame.width, 1f, $"{itemType} should crop tighter than full frame");
                Assert.AreEqual(frame.width, frame.height, 0.0001f, $"{itemType} crop must be square");
            }
        }

        [Test]
        public void GetFrame_WeaponType_MatchesFullWeaponPrefix()
        {
            var sword = ItemIconFraming.GetFrame("w01");
            var shield = ItemIconFraming.GetFrame("w08");
            Assert.AreNotEqual(sword, shield, "weapon types frame independently");
        }

        [Test]
        public void GetFrame_StaysInsideUnitSquare()
        {
            foreach (var itemType in new[] { "hd01", "tr03", "ar02", "lg01", "fe01", "mc04",
                "w01", "w02", "w03", "w04", "w05", "w06", "w07", "w08", "w09" })
            {
                var f = ItemIconFraming.GetFrame(itemType);
                Assert.GreaterOrEqual(f.xMin, 0f, itemType);
                Assert.GreaterOrEqual(f.yMin, 0f, itemType);
                Assert.LessOrEqual(f.xMax, 1.0001f, itemType);
                Assert.LessOrEqual(f.yMax, 1.0001f, itemType);
            }
        }

        [Test]
        public void GetFrame_UnknownOrNull_ReturnsFullFrame()
        {
            Assert.AreEqual(new Rect(0, 0, 1, 1), ItemIconFraming.GetFrame("zz99"));
            Assert.AreEqual(new Rect(0, 0, 1, 1), ItemIconFraming.GetFrame(null));
            Assert.AreEqual(new Rect(0, 0, 1, 1), ItemIconFraming.GetFrame(""));
        }

        [Test]
        public void GetFrame_BootsZoomTighterThanTorso()
        {
            // Boots are a small sliver at the bottom of the canvas; torso fills the middle.
            Assert.Less(ItemIconFraming.GetFrame("fe01").width,
                ItemIconFraming.GetFrame("tr01").width);
        }
    }
}
