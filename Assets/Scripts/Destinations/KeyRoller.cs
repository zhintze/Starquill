using System;
using Starquill.Core;

namespace Starquill.Destinations
{
    /// Rolls base key drops: kind 40/30/30 color/type/class, variant uniform,
    /// difficulty 85/13/2 (design §2, D2). Weights are intentionally compiled
    /// constants until the balance pass proves they need to be knobs.
    public static class KeyRoller
    {
        public static KeyInstance Roll(Random rng)
        {
            var key = new KeyInstance();

            double kind = rng.NextDouble();
            if (kind < 0.40)
                key.ColorFamily = (ColorFamily)rng.Next(Enum.GetValues(typeof(ColorFamily)).Length);
            else if (kind < 0.70)
                key.Slot = (KeySlot)rng.Next(Enum.GetValues(typeof(KeySlot)).Length);
            else
                key.ArchetypeId = rng.Next(ClassArchetype.All.Count);

            double d = rng.NextDouble();
            key.Difficulty = d < 0.85 ? 1 : d < 0.98 ? 2 : 3;
            return key;
        }
    }
}
