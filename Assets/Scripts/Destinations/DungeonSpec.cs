using Starquill.Core;

namespace Starquill.Destinations
{
    /// A generated dungeon run: deterministic from (key, runCounter,
    /// questLevel): regenerate on load, never serialize (quest pattern).
    public class DungeonSpec
    {
        public KeyInstance Key;
        public int Seed;
        public int QuestLevel;
        public float DurationSeconds;
        public float EnemyHpMultiplier;
        public Rarity RarityFloor;
        public float LegendaryWeightMult;
        public int BaseRolls;
        public int ParWaves;
    }
}
