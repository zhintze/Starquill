using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Characters
{
    public class CharacterFactoryTests
    {
        private CharacterFactory factory;

        [SetUp]
        public void SetUp()
        {
            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();

            var catalog = new EquipmentCatalog();
            catalog.LoadFromResources();

            var affixTable = new AffixTable();
            var affixAsset = Resources.Load<TextAsset>("Data/affixes");
            if (affixAsset != null) affixTable.LoadFromJson(affixAsset.text);

            var equipFactory = new EquipmentFactory(catalog, affixTable, registry.Colors);

            var nameGen = new NameGenerator();
            nameGen.LoadFromResources();

            factory = new CharacterFactory(registry, equipFactory, nameGen);
        }

        [Test]
        public void CreateRandom_ProducesValidCharacter()
        {
            var rng = new System.Random(42);
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            var character = factory.CreateRandom(speciesKeys[0], 1, 1, rng);

            Assert.IsNotNull(character);
            Assert.IsFalse(string.IsNullOrEmpty(character.id));
            Assert.IsFalse(string.IsNullOrEmpty(character.displayName));
            Assert.IsFalse(string.IsNullOrEmpty(character.speciesId));
            Assert.AreEqual(1, character.level);
            Assert.Greater(character.baseStats.Total, 0);
        }

        [Test]
        public void CreateRandom_HasEquipmentInPrioritySlots()
        {
            var rng = new System.Random(42);
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            var character = factory.CreateRandom(speciesKeys[0], 1, 1, rng);

            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Torso], "Torso should be equipped");
            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Legs], "Legs should be equipped");
        }

        [Test]
        public void CreateRandom_HasVerbs()
        {
            var rng = new System.Random(42);
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            var character = factory.CreateRandom(speciesKeys[0], 1, 1, rng);

            Assert.AreEqual(2, character.equippedVerbs.Count);
        }

        [Test]
        public void CreateStarterRoster_ProducesRequestedCount()
        {
            var rng = new System.Random(42);
            var roster = factory.CreateStarterRoster(8, rng);
            Assert.AreEqual(8, roster.Count);
        }

        [Test]
        public void CreateStarterRoster_HasSpeciesVariety()
        {
            var rng = new System.Random(42);
            var roster = factory.CreateStarterRoster(8, rng);

            var speciesCounts = new Dictionary<string, int>();
            foreach (var c in roster)
            {
                if (!speciesCounts.ContainsKey(c.speciesId))
                    speciesCounts[c.speciesId] = 0;
                speciesCounts[c.speciesId]++;
            }

            foreach (var count in speciesCounts.Values)
                Assert.LessOrEqual(count, 2, "Max 2 of same species in starter roster");
        }

        [Test]
        public void CreateRandom_VerbMatchesHighestStat()
        {
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            int matchCount = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var rng = new System.Random(seed);
                var c = factory.CreateRandom(speciesKeys[0], 1, 1, rng);
                if (c.equippedVerbs.Count > 0)
                {
                    var highestStat = c.baseStats.HighestStat();
                    if (c.equippedVerbs[0].statType == highestStat)
                        matchCount++;
                }
            }
            Assert.Greater(matchCount, 25, "Primary verb should match highest stat most of the time");
        }
    }
}
