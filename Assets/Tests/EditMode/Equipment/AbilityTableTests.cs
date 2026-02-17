using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class AbilityTableTests
    {
        private AbilityTable table;

        [SetUp]
        public void SetUp()
        {
            table = new AbilityTable();
            var asset = Resources.Load<TextAsset>("Data/abilities");
            Assert.IsNotNull(asset, "abilities.json not found in Resources");
            table.LoadFromJson(asset.text);
        }

        [Test]
        public void LoadFromJson_ParsesAll14Abilities()
        {
            Assert.AreEqual(14, table.AllAbilities.Count);
        }

        [Test]
        public void GetValidAbilities_ForTorso_ReturnsIronhide()
        {
            var valid = table.GetValidAbilities("tr01");
            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual("ironhide", valid[0].Id);
        }

        [Test]
        public void GetValidAbilities_ForAccessory_ReturnsFour()
        {
            var valid = table.GetValidAbilities("mc01");
            Assert.AreEqual(4, valid.Count);
        }

        [Test]
        public void GetValidAbilities_ForWeapon_ReturnsTwoNonShield()
        {
            var valid = table.GetValidAbilities("w01");
            Assert.AreEqual(2, valid.Count);
            var ids = new HashSet<string>();
            foreach (var a in valid) ids.Add(a.Id);
            Assert.IsTrue(ids.Contains("keen_edge"));
            Assert.IsTrue(ids.Contains("arcane_edge"));
        }

        [Test]
        public void GetValidAbilities_ForShield_ReturnsBulwark()
        {
            var valid = table.GetValidAbilities("w08");
            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual("bulwark", valid[0].Id);
        }

        [Test]
        public void RollAbility_ReturnsValidEntry()
        {
            var rng = new System.Random(42);
            var ability = table.RollAbility("tr01", rng);
            Assert.IsNotNull(ability);
            Assert.AreEqual("ironhide", ability.Id);
        }

        [Test]
        public void RollAbility_ForAccessory_ProducesVariety()
        {
            var uniqueIds = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                var rng = new System.Random(i);
                var ability = table.RollAbility("mc01", rng);
                Assert.IsNotNull(ability);
                uniqueIds.Add(ability.Id);
            }
            Assert.Greater(uniqueIds.Count, 1, "Expected more than 1 unique ability from 100 rolls");
        }

        [Test]
        public void AllAbilities_HaveValidBoostedStat()
        {
            foreach (var ability in table.AllAbilities)
            {
                Assert.IsTrue(
                    System.Enum.IsDefined(typeof(StatType), ability.BoostedStat),
                    $"Ability {ability.Id} has invalid BoostedStat: {ability.BoostedStat}"
                );
            }
        }

        [Test]
        public void AllAbilities_HavePositivePotency()
        {
            foreach (var ability in table.AllAbilities)
            {
                Assert.Greater(ability.BasePotency, 0f,
                    $"Ability {ability.Id} has non-positive BasePotency: {ability.BasePotency}");
                Assert.Greater(ability.PotencyPerLevel, 0f,
                    $"Ability {ability.Id} has non-positive PotencyPerLevel: {ability.PotencyPerLevel}");
            }
        }
    }
}
