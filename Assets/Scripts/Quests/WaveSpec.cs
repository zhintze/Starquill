using Starquill.Core;

namespace Starquill.Quests
{
    /// One wave of a generated quest. On boss waves the FIRST enemy is the
    /// boss (its HP multiplier is already folded into HpMultiplier by the
    /// generator); remaining entries are typed adds.
    public class WaveSpec
    {
        public int EnemyCount;
        public StatType[] EnemyTypes = System.Array.Empty<StatType>();
        public float HpMultiplier = 1f;
        public bool IsBossWave;
    }
}
