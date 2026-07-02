namespace Starquill.Destinations
{
    /// Fusion rules (design §2, D4): different kinds combine; same kind +
    /// same variant merges (the upgrade path); same kind + different
    /// variant is invalid. Difficulty always sums, capped.
    public static class KeyFusion
    {
        public static bool CanFuse(KeyInstance a, KeyInstance b, int maxDifficulty)
        {
            if (!IsValidCombination(a, b)) return false;
            if (a.Difficulty + b.Difficulty > maxDifficulty) return false;
            return true;
        }

        /// Modifier-conflict rules shared by CanFuse and Fuse: inputs must be
        /// non-null, distinct instances, with no same-kind different-variant clash.
        private static bool IsValidCombination(KeyInstance a, KeyInstance b)
        {
            if (a == null || b == null || a == b) return false;
            if (a.ColorFamily.HasValue && b.ColorFamily.HasValue && a.ColorFamily != b.ColorFamily)
                return false;
            if (a.Slot.HasValue && b.Slot.HasValue && a.Slot != b.Slot)
                return false;
            if (a.ArchetypeId >= 0 && b.ArchetypeId >= 0 && a.ArchetypeId != b.ArchetypeId)
                return false;
            return true;
        }

        /// Returns null on an invalid combination (null input, self-fuse, or a
        /// same-kind different-variant conflict). The difficulty cap is NOT
        /// checked here: it needs config the caller owns; enforce it via CanFuse.
        public static KeyInstance Fuse(KeyInstance a, KeyInstance b)
        {
            if (!IsValidCombination(a, b)) return null;

            return new KeyInstance
            {
                ColorFamily = a.ColorFamily ?? b.ColorFamily,
                Slot = a.Slot ?? b.Slot,
                ArchetypeId = a.ArchetypeId >= 0 ? a.ArchetypeId : b.ArchetypeId,
                Difficulty = a.Difficulty + b.Difficulty
            };
        }

        /// D5: quadratic gold cost.
        public static double Cost(float baseCost, int questLevel, int resultDifficulty)
            => baseCost * System.Math.Max(1, questLevel) * resultDifficulty * (double)resultDifficulty;
    }
}
