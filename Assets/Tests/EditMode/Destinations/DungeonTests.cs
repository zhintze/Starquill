using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class DungeonTests
    {
        private static KeyInstance BrownWarriorHelm(int d) => new()
        { ColorFamily = ColorFamily.Brown, Slot = KeySlot.Head, ArchetypeId = 0, Difficulty = d };

        [Test]
        public void Generate_IsDeterministic()
        {
            var a = DungeonGenerator.Generate(BrownWarriorHelm(3), runCounter: 5, questLevel: 20,
                durationBase: 150f, durationPerD: 20f, enemyMultPerD: 0.6f, parSecondsPerWave: 15f);
            var b = DungeonGenerator.Generate(BrownWarriorHelm(3), runCounter: 5, questLevel: 20,
                durationBase: 150f, durationPerD: 20f, enemyMultPerD: 0.6f, parSecondsPerWave: 15f);
            Assert.AreEqual(a.Seed, b.Seed);
            Assert.AreEqual(a.DurationSeconds, b.DurationSeconds);
        }

        [Test]
        public void Generate_DifficultyMapping()
        {
            var spec = DungeonGenerator.Generate(BrownWarriorHelm(3), 0, 20, 150f, 20f, 0.6f, 15f);
            Assert.AreEqual(190f, spec.DurationSeconds, 0.01f);       // 150 + 20*2
            Assert.AreEqual(2.2f, spec.EnemyHpMultiplier, 0.001f);    // 1 + 0.6*2
            Assert.AreEqual(Rarity.Epic, spec.RarityFloor);
            Assert.AreEqual(5, spec.BaseRolls);                        // 2 + D
            // NOTE: corrected during implementation: the plan asserted 12, but
            // Math.Round(190/15) = Math.Round(12.667) = 13. Do not "fix" back.
            Assert.AreEqual(13, spec.ParWaves);                        // 190/15 rounded
        }

        [TestCase(1, Rarity.Uncommon, 1f)]
        [TestCase(2, Rarity.Rare, 1f)]
        [TestCase(4, Rarity.Epic, 2f)]     // D4: Epic floor + doubled legendary weight
        [TestCase(5, Rarity.Legendary, 1f)]
        [TestCase(6, Rarity.Legendary, 1f)]
        public void FloorForDifficulty(int d, Rarity floor, float legendaryMult)
        {
            Assert.AreEqual(floor, DungeonGenerator.FloorForDifficulty(d));
            Assert.AreEqual(legendaryMult, DungeonGenerator.LegendaryMultForDifficulty(d));
        }

        [Test]
        public void MakeWave_MiniBossEveryFifth_AndRampsHp()
        {
            var spec = DungeonGenerator.Generate(BrownWarriorHelm(1), 0, 10, 150f, 20f, 0.6f, 15f);
            Assert.IsFalse(DungeonGenerator.MakeWave(spec, 0).IsBossWave);
            Assert.IsTrue(DungeonGenerator.MakeWave(spec, 4).IsBossWave);  // waves 5,10,... (index 4,9,...)
            Assert.IsTrue(DungeonGenerator.MakeWave(spec, 9).IsBossWave);
            // Determinism: same index -> same wave
            var w1 = DungeonGenerator.MakeWave(spec, 3);
            var w2 = DungeonGenerator.MakeWave(spec, 3);
            Assert.AreEqual(w1.EnemyCount, w2.EnemyCount);
            Assert.AreEqual(w1.EnemyTypes[0], w2.EnemyTypes[0]);
        }

        [Test]
        public void MakeWave_ClassKey_ThemesEnemiesToArchetype()
        {
            var key = new KeyInstance { ArchetypeId = 0, Difficulty = 1 }; // Warrior STR/DEX
            var spec = DungeonGenerator.Generate(key, 0, 10, 150f, 20f, 0.6f, 15f);
            int themed = 0, total = 0;
            for (int w = 0; w < 40; w++)
            {
                var wave = DungeonGenerator.MakeWave(spec, w);
                foreach (var t in wave.EnemyTypes)
                { total++; if (t == StatType.STR || t == StatType.DEX) themed++; }
            }
            Assert.Greater(themed / (float)total, 0.45f); // 60% target, loose bound
        }

        [Test]
        public void DungeonRun_TicksDown_And_CountsBonusRolls()
        {
            var spec = DungeonGenerator.Generate(BrownWarriorHelm(1), 0, 10, 150f, 20f, 0.6f, 15f);
            var run = new DungeonRun(spec);
            Assert.IsFalse(run.IsOver);
            run.Tick(200f);
            Assert.IsTrue(run.IsOver);

            for (int i = 0; i < spec.ParWaves + 7; i++) run.WaveCleared();
            // 7 over par, 3 waves per roll -> 2, under cap 3
            Assert.AreEqual(2, run.BonusRolls(wavesPerRoll: 3, cap: 3));
        }
    }
}
