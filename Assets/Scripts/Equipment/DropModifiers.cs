using Starquill.Core;

namespace Starquill.Equipment
{
    /// Loot targeting applied by dungeon keys (design §3). GameManager
    /// translates a KeyInstance into this; the factory and dropper stay
    /// ignorant of the Destinations assembly.
    public class DropModifiers
    {
        public ColorFamily? ColorFamily;
        public EquipmentSlot? PreferredSlot;   // Head/Torso/Arms/Legs/Feet/MainHand/Misc1
        public bool PreferWeapon;              // KeySlot.Weapon maps here
        public float SlotWeight = 0.8f;        // chance the preferred slot is used
        public StatType? ForcedStatA;          // class archetype pair; primary rolls
        public StatType? ForcedStatB;          //   50/50 between A and B
    }
}
