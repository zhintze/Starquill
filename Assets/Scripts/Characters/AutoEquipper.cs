using System;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.Characters
{
    public struct AutoEquipResult
    {
        public int ItemsEquipped;
    }

    public static class AutoEquipper
    {
        public static AutoEquipResult AutoEquip(CharacterInstance character, LootInventory inventory)
        {
            var result = new AutoEquipResult();
            var slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

            foreach (var slot in slots)
            {
                var candidates = inventory.GetItemsForSlot(slot);
                if (candidates.Count == 0) continue;

                var current = character.equipment[(int)slot];
                var best = ItemComparer.FindBestForSlot(slot, candidates, current);

                if (best != null)
                {
                    inventory.RemoveItem(best);
                    if (current != null)
                        inventory.AddItem(current);
                    character.equipment[(int)slot] = best;
                    result.ItemsEquipped++;
                }
            }

            return result;
        }
    }
}
