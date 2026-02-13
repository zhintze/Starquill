using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Species Definition")]
    public class SpeciesDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string speciesId;
        public string displayName;
        public string description;
        public Sprite portrait;

        [Header("Base Stats (must total 30)")]
        public Stats baseStats;

        [Header("Species Ability")]
        public SpeciesAbilityDefinition ability;

        [Header("Visual")]
        public string[] bodyPartIds;
        public Color skinColor = Color.white;
        public Color hairColor = Color.white;
        public float xScale = 1f;
        public float yScale = 1f;

        [Header("Restrictions")]
        public string[] itemRestrictions;
    }
}
