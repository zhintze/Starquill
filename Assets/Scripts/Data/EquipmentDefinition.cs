using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Equipment Definition")]
    public class EquipmentDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        public string description;
        public Sprite icon;
        public Rarity rarity;

        [Header("Slot")]
        public EquipmentSlot slot;

        [Header("Stats")]
        public Stats statMods;
        public float percentDamageBonus;

        [Header("Affixes")]
        public AffixDefinition[] possibleAffixes;
        public int maxAffixSlots;

        [Header("Visual Layers")]
        public int[] layerCodes;
        public int[] hiddenLayers;
        public int[] layerColorVariance;
        public int variantCount = 1;

        [Header("Set")]
        public EquipmentSetDefinition setMembership;
    }
}
