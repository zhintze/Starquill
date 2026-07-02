using System;
using System.Collections.Generic;

namespace Starquill.Destinations
{
    /// The player's key inventory. Soft cap halves the drop rate above it
    /// (enforced by the drop caller): never blocks (design §2).
    public class KeyPouch
    {
        private readonly List<KeyInstance> keys = new();
        public IReadOnlyList<KeyInstance> Keys => keys;

        public event Action OnChanged;

        public void Add(KeyInstance key)
        {
            if (key == null) return;
            keys.Add(key);
            OnChanged?.Invoke();
        }

        public bool Remove(KeyInstance key)
        {
            if (!keys.Remove(key)) return false;
            OnChanged?.Invoke();
            return true;
        }

        public bool IsOverSoftCap(int softCap) => keys.Count >= softCap;

        /// D6: difficulty-scaled sell pricing.
        public static double SellValue(KeyInstance key, int questLevel, float sellBase)
            => sellBase * Math.Max(1, questLevel) * Math.Max(1, key.Difficulty);
    }
}
