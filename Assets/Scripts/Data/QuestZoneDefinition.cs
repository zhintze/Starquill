using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Quest Zone")]
    public class QuestZoneDefinition : ScriptableObject
    {
        public string zoneId;
        public string displayName;
        public string description;
        public int zoneLevel;
        public StatType[] dominantEnemyTypes;
        public int questCount = 10;
        public Sprite backgroundArt;

        [Header("Dialogue")]
        public string[] discoveryDialogue;
        public string[] completionDialogue;

        [Header("Milestone Reward")]
        public string milestoneUnlockDescription;
    }
}
