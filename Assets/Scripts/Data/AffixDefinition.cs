using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Affix Definition")]
    public class AffixDefinition : ScriptableObject
    {
        public string affixId;
        public string displayName;
        public StatType statModified;
        public float minValue;
        public float maxValue;
        public bool isPercentage;
        public EquipmentSlot[] validSlots;
    }
}
