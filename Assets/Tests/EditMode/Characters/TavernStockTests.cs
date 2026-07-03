using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Characters
{
    public class TavernStockTests
    {
        private CharacterFactory factory;
        private List<string> speciesKeys;

        private const float Hours = 6f;
        private const double Window = 6.0 * 3600.0; // 21600s

        [SetUp]
        public void SetUp()
        {
            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();

            var catalog = new EquipmentCatalog();
            catalog.LoadFromResources();

            var abilityTable = new AbilityTable();
            var abilityAsset = Resources.Load<TextAsset>("Data/abilities");
            if (abilityAsset != null) abilityTable.LoadFromJson(abilityAsset.text);

            var equipFactory = new EquipmentFactory(catalog, abilityTable, registry.Colors);

            var nameGen = new NameGenerator();
            nameGen.LoadFromResources();

            factory = new CharacterFactory(registry, equipFactory, nameGen);
            speciesKeys = new List<string>(registry.Species.Keys);
        }

        // ---------- Slot math ----------

        [Test]
        public void SlotId_FloorsByRotationWindow()
        {
            Assert.AreEqual(0, TavernStock.SlotId(0, Hours));
            Assert.AreEqual(0, TavernStock.SlotId(Window - 1, Hours));
            Assert.AreEqual(2, TavernStock.SlotId(Window * 2 + 5, Hours));
        }

        [Test]
        public void SlotId_BoundaryAtExactWindow()
        {
            Assert.AreEqual(1, TavernStock.SlotId(Window, Hours));
        }

        [Test]
        public void SlotEndsAt_IsAfterNowInsideSlot()
        {
            double now = Window + 1234;
            long slot = TavernStock.SlotId(now, Hours);
            double end = TavernStock.SlotEndsAtUnix(slot, Hours);
            Assert.Greater(end, now);
            Assert.AreEqual(Window * 2, end, 0.001);
        }

        // ---------- Generate ----------

        [Test]
        public void Generate_HonorsCountAndTargetLevel()
        {
            var stock = TavernStock.Generate(factory, speciesKeys, 6, 5, 3, 1000.0,
                new System.Random(42));

            Assert.AreEqual(6, stock.Recruits.Count);
            Assert.AreEqual(6, stock.Prices.Length);
            Assert.AreEqual(6, stock.Purchased.Length);
            foreach (var recruit in stock.Recruits)
            {
                Assert.IsNotNull(recruit);
                Assert.AreEqual(5, recruit.level);
            }
        }

        [Test]
        public void Generate_AllocatesStatsLikeOrganicLeveling()
        {
            var stock = TavernStock.Generate(factory, speciesKeys, 6, 5, 3, 1000.0,
                new System.Random(7));

            // LevelUp allocates ~3 points/level with fractional accumulation;
            // after 4 level-ups expect close to 12, loosely >= 8 and <= 12.
            foreach (var recruit in stock.Recruits)
            {
                Assert.GreaterOrEqual(recruit.allocatedStats.Total, 2 * 4);
                Assert.LessOrEqual(recruit.allocatedStats.Total, 3 * 4);
            }
        }

        [Test]
        public void Generate_PricesWithinBand()
        {
            const double basePrice = 1000.0;
            var stock = TavernStock.Generate(factory, speciesKeys, 6, 3, 2, basePrice,
                new System.Random(11));

            foreach (var price in stock.Prices)
            {
                Assert.GreaterOrEqual(price, basePrice * 0.85);
                Assert.LessOrEqual(price, basePrice * 1.15);
            }
        }

        [Test]
        public void Generate_PurchasedAllFalse()
        {
            var stock = TavernStock.Generate(factory, speciesKeys, 6, 1, 1, 500.0,
                new System.Random(3));
            foreach (var purchased in stock.Purchased)
                Assert.IsFalse(purchased);
        }

        [Test]
        public void Generate_SoftSpeciesVariety()
        {
            if (speciesKeys.Count < 3)
                Assert.Ignore("Needs at least 3 species for variety check");

            var stock = TavernStock.Generate(factory, speciesKeys, 6, 1, 1, 500.0,
                new System.Random(21));

            var counts = new Dictionary<string, int>();
            foreach (var recruit in stock.Recruits)
            {
                counts.TryGetValue(recruit.speciesId, out int n);
                counts[recruit.speciesId] = n + 1;
            }
            foreach (var count in counts.Values)
                Assert.LessOrEqual(count, 2, "Max 2 per species before reuse");
        }

        [Test]
        public void Generate_DeterministicForSameSeed()
        {
            var a = TavernStock.Generate(factory, speciesKeys, 6, 4, 3, 800.0,
                new System.Random(99));
            var b = TavernStock.Generate(factory, speciesKeys, 6, 4, 3, 800.0,
                new System.Random(99));

            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(a.Recruits[i].displayName, b.Recruits[i].displayName);
                Assert.AreEqual(a.Recruits[i].speciesId, b.Recruits[i].speciesId);
                Assert.AreEqual(a.Prices[i], b.Prices[i], 0.0001);
            }
        }

        [Test]
        public void Generate_RecruitsCarryGearAndVerbs()
        {
            var stock = TavernStock.Generate(factory, speciesKeys, 6, 2, 2, 500.0,
                new System.Random(5));
            foreach (var recruit in stock.Recruits)
            {
                Assert.AreEqual(2, recruit.equippedVerbs.Count);
                bool hasGear = false;
                foreach (var item in recruit.equipment)
                    if (item != null) { hasGear = true; break; }
                Assert.IsTrue(hasGear, "Recruit should come with a loadout");
            }
        }
    }
}
