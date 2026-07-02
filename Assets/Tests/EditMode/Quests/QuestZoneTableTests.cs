using NUnit.Framework;
using Starquill.Core;
using Starquill.Quests;
using UnityEngine;

namespace Starquill.Tests.EditMode.Quests
{
    public class QuestZoneTableTests
    {
        private QuestZoneTable table;

        [SetUp]
        public void SetUp()
        {
            table = new QuestZoneTable();
            var asset = Resources.Load<TextAsset>("Data/quest_zones");
            Assert.IsNotNull(asset, "quest_zones.json not found in Resources");
            table.LoadFromJson(asset.text);
        }

        [Test]
        public void LoadFromJson_ParsesBothZones()
        {
            Assert.AreEqual(2, table.Zones.Count);
            Assert.AreEqual("verdant-hollow", table.Zones[0].Id);
            Assert.AreEqual("gloomspire-ruins", table.Zones[1].Id);
        }

        [Test]
        public void Zones_HaveValidDominantTypes()
        {
            foreach (var zone in table.Zones)
            {
                Assert.GreaterOrEqual(zone.DominantTypes.Length, 1, zone.Id);
                foreach (var t in zone.DominantTypes)
                    Assert.IsTrue(System.Enum.IsDefined(typeof(StatType), t), zone.Id);
            }
        }

        [Test]
        public void Zones_HaveDialogue()
        {
            foreach (var zone in table.Zones)
            {
                Assert.Greater(zone.IntroDialogue.Length, 0, zone.Id);
                Assert.Greater(zone.CompletionDialogue.Length, 0, zone.Id);
            }
        }

        [Test]
        public void GetZone_ClampsOutOfRange()
        {
            Assert.AreEqual(table.Zones[1], table.GetZone(99));
            Assert.AreEqual(table.Zones[0], table.GetZone(-1));
        }

        [Test]
        public void GetZone_EmptyTable_ReturnsNull()
        {
            Assert.IsNull(new QuestZoneTable().GetZone(0));
        }
    }
}
