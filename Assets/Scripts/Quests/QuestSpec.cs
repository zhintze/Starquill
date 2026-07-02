namespace Starquill.Quests
{
    /// A fully generated quest: deterministic from (zoneIndex, questIndex,
    /// questLevel), so specs never need serialization — regenerate on load.
    public class QuestSpec
    {
        public int ZoneIndex;
        public int QuestIndex;
        public QuestTier Tier;
        public string DisplayName = "";
        public WaveSpec[] Waves = System.Array.Empty<WaveSpec>();
        public QuestRewardSpec Reward = new();

        public int TotalEnemies
        {
            get
            {
                int total = 0;
                foreach (var wave in Waves) total += wave.EnemyCount;
                return total;
            }
        }
    }
}
