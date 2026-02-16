using Starquill.Core;
using Starquill.Data;

namespace Starquill.UI
{
    public struct ActionCardData
    {
        public string DisplayName;
        public StatType StatType;
        public string DamageText;
        public string CooldownText;
        public string TargetModeText;
        public string StatAbbreviation;
        public bool IsHealing;

        public static ActionCardData FromVerb(VerbDefinition verb)
        {
            float displayValue = verb.isHealingVerb ? verb.healAmount : verb.baseDamage;

            return new ActionCardData
            {
                DisplayName = verb.displayName,
                StatType = verb.statType,
                DamageText = ((int)displayValue).ToString(),
                CooldownText = $"{(int)verb.cooldownTicks}s",
                TargetModeText = verb.targetMode.ToString(),
                StatAbbreviation = verb.statType.ToString(),
                IsHealing = verb.isHealingVerb
            };
        }
    }
}
