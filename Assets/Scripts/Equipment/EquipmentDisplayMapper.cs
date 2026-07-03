using System.Collections.Generic;
using Starquill.Core;
using Starquill.Display;
using UnityEngine;

namespace Starquill.Equipment
{
    /// Maps equipped EquipmentInstances to the display infos the
    /// compositing pipeline consumes. Single home for the mapping (and for
    /// the bare-head rule) so UI call sites never duplicate it.
    public static class EquipmentDisplayMapper
    {
        /// Builds a fresh display list; never touches the source items.
        /// bareHead skips the Head slot so the face stays visible: it is
        /// presentation-only, the character keeps its head gear.
        public static List<EquipmentDisplayInfo> ToDisplayList(
            IEnumerable<EquipmentInstance> equipment, bool bareHead = false)
        {
            var list = new List<EquipmentDisplayInfo>();
            if (equipment == null) return list;

            foreach (var eq in equipment)
            {
                if (eq == null) continue;
                if (bareHead && eq.Slot == EquipmentSlot.Head) continue;
                list.Add(new EquipmentDisplayInfo
                {
                    ItemType = eq.ItemType,
                    ItemNum = eq.ItemNum,
                    BaseColor = eq.BaseColor,
                    VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                        ?? new Dictionary<int, Color>(eq.VarianceColors),
                    IsOffhand = eq.Slot == EquipmentSlot.OffHand,
                    LayerVariants = eq.LayerVariants
                });
            }
            return list;
        }
    }
}
