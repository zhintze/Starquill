using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Status Effect")]
    public class StatusEffectDefinition : ScriptableObject
    {
        public StatusEffectType effectType;
        public string displayName;
        public string description;
        public Sprite icon;
        public Color effectColor;

        [Header("Stacking Rules")]
        public bool canStack;
        public int maxStacks = 1;
        public bool refreshOnReapply = true;

        [Header("Interaction")]
        public StatusEffectType overrides;
        public StatusEffectType cleansedBy;
        public StatusEffectType reduces;
    }
}
