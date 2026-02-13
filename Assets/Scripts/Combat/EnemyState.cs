using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Combat
{
    public class EnemyState
    {
        public string Id { get; }
        public StatType StatType { get; }
        public float MaxHP { get; }
        public float CurrentHP { get; set; }
        public bool IsAlive => CurrentHP > 0;
        public float DamagePerTick { get; set; }
        private readonly Dictionary<StatusEffectType, int> activeEffects = new();

        public EnemyState(string id, StatType statType, float maxHP, float damagePerTick)
        {
            Id = id; StatType = statType; MaxHP = maxHP; CurrentHP = maxHP; DamagePerTick = damagePerTick;
        }

        public void TakeDamage(float amount)
        {
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }

        public void ApplyStatus(StatusEffectType type, int duration)
        {
            if (type == StatusEffectType.None) return;
            if (type == StatusEffectType.Confuse) activeEffects.Remove(StatusEffectType.Weaken);
            if (type == StatusEffectType.Reveal) activeEffects.Remove(StatusEffectType.Confuse);
            activeEffects[type] = duration;
        }

        public bool HasStatus(StatusEffectType type)
        {
            return activeEffects.ContainsKey(type) && activeEffects[type] > 0;
        }

        public void TickStatuses()
        {
            var keys = new List<StatusEffectType>(activeEffects.Keys);
            foreach (var key in keys)
            {
                activeEffects[key]--;
                if (activeEffects[key] <= 0) activeEffects.Remove(key);
            }
        }

        public float GetDamageOutput()
        {
            float dmg = DamagePerTick;
            if (HasStatus(StatusEffectType.Weaken)) dmg *= 0.8f;
            if (HasStatus(StatusEffectType.Stagger)) dmg = 0;
            return dmg;
        }
    }
}
