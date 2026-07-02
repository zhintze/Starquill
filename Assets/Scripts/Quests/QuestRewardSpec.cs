using Starquill.Core;

namespace Starquill.Quests
{
    /// What completing a quest pays out. Executed by GameManager (gold math
    /// and loot rolls live above this assembly).
    public class QuestRewardSpec
    {
        public float GoldMultiplier = 1f;
        public int LootRolls;
        public Rarity? RarityFloor;
        public int ExtraNoFloorRolls;

        // Key payouts (design §2). Plain ints/floats only: Quests must not
        // reference Destinations.
        public int KeyDrops;
        public float ExtraKeyChance;
        public int KeyDifficulty = 1;

        public static QuestRewardSpec ForTier(QuestTier tier)
        {
            return tier switch
            {
                QuestTier.Elite => new QuestRewardSpec
                    { GoldMultiplier = 2f, LootRolls = 1, RarityFloor = Rarity.Uncommon, KeyDrops = 1 },
                QuestTier.Hard => new QuestRewardSpec
                    { GoldMultiplier = 2.5f, LootRolls = 1, RarityFloor = Rarity.Rare, ExtraNoFloorRolls = 1,
                      KeyDrops = 1, ExtraKeyChance = 0.25f },
                QuestTier.Boss => new QuestRewardSpec
                    { GoldMultiplier = 4f, LootRolls = 2, RarityFloor = Rarity.Rare, KeyDrops = 1, KeyDifficulty = 2 },
                _ => new QuestRewardSpec
                    { GoldMultiplier = 1.5f, LootRolls = 1, RarityFloor = null }
            };
        }
    }
}
