using System;
using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Equipment
{
    public class LootInventory
    {
        private readonly List<EquipmentInstance> items = new();

        public int Capacity { get; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public IReadOnlyList<EquipmentInstance> Items => items;

        public event Action<EquipmentInstance> OnItemAdded;
        public event Action<EquipmentInstance> OnItemRemoved;

        public LootInventory(int capacity = 50)
        {
            Capacity = capacity;
        }

        public bool AddItem(EquipmentInstance item)
        {
            if (item == null || IsFull) return false;
            items.Add(item);
            OnItemAdded?.Invoke(item);
            return true;
        }

        public bool RemoveItem(EquipmentInstance item)
        {
            if (!items.Remove(item)) return false;
            OnItemRemoved?.Invoke(item);
            return true;
        }

        public List<EquipmentInstance> GetItemsForSlot(EquipmentSlot slot)
        {
            var result = new List<EquipmentInstance>();
            foreach (var item in items)
                if (item.Slot == slot) result.Add(item);
            return result;
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
