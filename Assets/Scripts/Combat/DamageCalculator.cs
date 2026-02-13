namespace Starquill.Combat
{
    public static class DamageCalculator
    {
        public static float Calculate(
            float baseDamage, float statScaling, int characterStat, float advantageMultiplier,
            bool targetIsExposed = false, float equipmentDamageBonus = 0f,
            float prestigeMultiplier = 1f, float boostMultiplier = 1f)
        {
            float statMult = 1f + characterStat * statScaling * 0.1f;
            float exposeMult = targetIsExposed ? 1.25f : 1f;
            float equipMult = 1f + equipmentDamageBonus;
            return baseDamage * statMult * advantageMultiplier * exposeMult * equipMult * prestigeMultiplier * boostMultiplier;
        }

        public static float GetStatProcModifier(int statValue)
        {
            if (statValue >= 8) return 1.0f;
            if (statValue >= 5) return 0.75f;
            if (statValue >= 3) return 0.5f;
            return 0.25f;
        }

        public static float GetStatScalingModifier(int statValue)
        {
            if (statValue >= 5) return 1.0f;
            if (statValue >= 3) return 0.75f;
            return 0.5f;
        }
    }
}
