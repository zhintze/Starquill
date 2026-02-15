using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using Starquill.Managers;
using UnityEngine;

namespace Starquill.Tests.Save
{
    public class SaveRoundTripTests
    {
        [Test]
        public void SerializedEquipment_RoundTrips()
        {
            var original = new EquipmentInstance(
                "tr03", 5, EquipmentSlot.Torso, Rarity.Rare,
                new Color(0.5f, 0.3f, 0.1f, 1f),
                new Dictionary<int, Color> { { 48, new Color(0.2f, 0.4f, 0.6f, 1f) } },
                new Stats { STR = 3, CON = 2 },
                new List<RolledAffix>
                {
                    new RolledAffix("mighty", StatType.STR, 4f, false),
                    new RolledAffix("sturdy", StatType.CON, 3f, false)
                },
                new[] { 48 }, new int[0], new int[0],
                false, null, "Rare Sleeveless Shirt"
            );

            var serialized = SerializedEquipment.FromInstance(original);
            Assert.AreEqual("tr03", serialized.itemType);
            Assert.AreEqual(5, serialized.itemNum);
            Assert.AreEqual((int)Rarity.Rare, serialized.rarity);
            Assert.AreEqual(2, serialized.affixes.Length);
        }

        [Test]
        public void SerializedCharacter_RoundTrips()
        {
            var character = new CharacterInstance
            {
                id = "test123",
                displayName = "Theron the Bold",
                speciesId = "human",
                level = 3,
                xp = 150,
                speciesKills = 12,
                baseStats = new Stats { STR = 14, DEX = 10, CON = 12, INT = 8, WIS = 9, CHA = 10 },
                allocatedStats = new Stats { STR = 2 }
            };

            var serialized = SerializedCharacter.FromInstance(character);
            Assert.AreEqual("test123", serialized.id);
            Assert.AreEqual("Theron the Bold", serialized.displayName);
            Assert.AreEqual("human", serialized.speciesId);
            Assert.AreEqual(3, serialized.level);
        }

        [Test]
        public void OldSaveFormat_MigratesGracefully()
        {
            var oldSave = new SaveData { gold = 1000, currentQuestLevel = 5 };
            Assert.AreEqual(0, oldSave.roster.Count);
            Assert.IsTrue(oldSave.NeedsRosterInitialization());
        }

        [Test]
        public void SaveData_WithRoster_DoesNotNeedInit()
        {
            var save = new SaveData();
            save.roster.Add(new SerializedCharacter { id = "c1" });
            Assert.IsFalse(save.NeedsRosterInitialization());
        }

        [Test]
        public void Inventory_SurvivesRoundTrip()
        {
            var save = new SaveData();
            var equip = new SerializedEquipment
            {
                itemType = "hd01",
                itemNum = 2,
                slot = 0,
                rarity = 2,
                baseColor = new float[] { 1, 0, 0, 1 },
                statMods = new int[] { 5, 0, 0, 0, 0, 0 }
            };
            save.inventory.Add(equip);

            string json = JsonUtility.ToJson(save);
            var loaded = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(1, loaded.inventory.Count);
            Assert.AreEqual("hd01", loaded.inventory[0].itemType);
            Assert.AreEqual(2, loaded.inventory[0].rarity);
        }
    }
}
