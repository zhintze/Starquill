using NUnit.Framework;
using Starquill.Display;
using UnityEngine;

namespace Starquill.Tests.EditMode.Display
{
    public class WeaponPivotCompensationTests
    {
        [Test]
        public void ZeroRotation_NoCompensation()
        {
            var comp = DisplayBuilder.WeaponPivotCompensation("w01", 0f);
            Assert.AreEqual(0f, comp.x, 0.001f);
            Assert.AreEqual(0f, comp.y, 0.001f);
        }

        [Test]
        public void Compensation_KeepsCentroidFixed()
        {
            // After rotating the canvas by R and shifting by the compensation,
            // the weapon centroid must land exactly where it started.
            const float rot = -40f;
            var frame = ItemIconFraming.GetFrame("w01");
            var pivot = new Vector2((frame.center.x - 0.5f) * 200f, (frame.center.y - 0.5f) * 200f);

            var comp = DisplayBuilder.WeaponPivotCompensation("w01", rot);

            float rad = rot * Mathf.Deg2Rad;
            var rotated = new Vector2(
                pivot.x * Mathf.Cos(rad) - pivot.y * Mathf.Sin(rad),
                pivot.x * Mathf.Sin(rad) + pivot.y * Mathf.Cos(rad));

            Assert.AreEqual(pivot.x, rotated.x + comp.x, 0.001f);
            Assert.AreEqual(pivot.y, rotated.y + comp.y, 0.001f);
        }

        [Test]
        public void Compensation_BoundedByCanvas()
        {
            // Sanity: compensation should never exceed the canvas diagonal.
            foreach (var t in new[] { "w01", "w02", "w03", "w04", "w05", "w06", "w07" })
            {
                var comp = DisplayBuilder.WeaponPivotCompensation(t, -40f);
                Assert.Less(comp.magnitude, 283f, t);
            }
        }
    }
}
