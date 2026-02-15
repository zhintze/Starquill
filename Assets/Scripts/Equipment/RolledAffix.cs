using System;
using Starquill.Core;

namespace Starquill.Equipment
{
    [Serializable]
    public class RolledAffix
    {
        public string AffixId;
        public StatType StatType;
        public float Value;
        public bool IsPercentage;

        public RolledAffix(string affixId, StatType statType, float value, bool isPercentage)
        {
            AffixId = affixId;
            StatType = statType;
            Value = value;
            IsPercentage = isPercentage;
        }
    }
}
