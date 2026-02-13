using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Verb Definition")]
    public class VerbDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string verbId;
        public string displayName;
        public string description;
        public Sprite icon;

        [Header("Stat Typing")]
        public StatType statType;
        public VerbCategory Category => statType.GetCategory();

        [Header("Damage")]
        public float baseDamage;
        public int hitCount = 1;
        public TargetMode targetMode;

        [Header("Status Effect")]
        public StatusEffectType effect;
        public float effectProcChance = 0.5f;
        public int effectDuration = 2;
        public float effectPotency = 1f;

        [Header("Scaling")]
        public float statScaling = 1f;
        public int levelRequirement;
        public Rarity rarity;

        [Header("Cooldown")]
        public float cooldownTicks = 2f;
        public int verbPoolPriority;

        [Header("Healing (WIS only)")]
        public bool isHealingVerb;
        public float healAmount;
    }
}
