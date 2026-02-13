using UnityEngine;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Species Ability")]
    public class SpeciesAbilityDefinition : ScriptableObject
    {
        public string abilityId;
        public string displayName;
        public string description;
        public Sprite icon;

        [Header("Rank Thresholds (kills needed)")]
        public int[] rankThresholds = { 0, 100, 500, 2000, 10000, 50000 };

        [Header("Effect Per Rank (percentage bonus)")]
        public float[] effectPerRank = { 5f, 7f, 10f, 12f, 15f, 20f };
    }
}
