using Starquill.Core;

namespace Starquill.Destinations
{
    /// Slots a type key can target (design §2). Weapon covers MainHand/OffHand;
    /// Misc covers Misc1-4.
    public enum KeySlot { Head, Torso, Arms, Legs, Feet, Weapon, Misc }

    /// A dungeon key: up to one modifier of each kind + difficulty (design §2).
    public class KeyInstance
    {
        public ColorFamily? ColorFamily;
        public KeySlot? Slot;
        public int ArchetypeId = -1;   // -1 = no class modifier
        public int Difficulty = 1;

        public ClassArchetype Archetype =>
            ArchetypeId >= 0 ? ClassArchetype.ById(ArchetypeId) : null;

        public int ModifierCount =>
            (ColorFamily.HasValue ? 1 : 0) + (Slot.HasValue ? 1 : 0) + (ArchetypeId >= 0 ? 1 : 0);

        private static string SlotWord(KeySlot slot) => slot switch
        {
            KeySlot.Head => "Helm",
            KeySlot.Torso => "Armor",
            KeySlot.Arms => "Gauntlet",
            KeySlot.Legs => "Greaves",
            KeySlot.Feet => "Boot",
            KeySlot.Weapon => "Weapon",
            KeySlot.Misc => "Trinket",
            _ => ""
        };

        public string DisplayName
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (ColorFamily.HasValue) parts.Add(ColorFamily.Value.ToString());
                if (Archetype != null) parts.Add(Archetype.Name);
                if (Slot.HasValue) parts.Add(SlotWord(Slot.Value));
                parts.Add("Key");
                return string.Join(" ", parts);
            }
        }
    }
}
