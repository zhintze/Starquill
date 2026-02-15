using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Equipment
{
    [Serializable]
    public class SerializedEquipment
    {
        public string itemType;
        public int itemNum;
        public int slot;
        public int rarity;
        public float[] baseColor = new float[4];
        public int[] varianceColorKeys;
        public float[] varianceColorValuesFlat;
        public int[] statMods = new int[6];
        public SerializedAffix[] affixes;

        public static SerializedEquipment FromInstance(EquipmentInstance e)
        {
            var se = new SerializedEquipment
            {
                itemType = e.ItemType,
                itemNum = e.ItemNum,
                slot = (int)e.Slot,
                rarity = (int)e.Rarity,
                baseColor = new[] { e.BaseColor.r, e.BaseColor.g, e.BaseColor.b, e.BaseColor.a },
                statMods = new[] {
                    e.StatMods.STR, e.StatMods.DEX, e.StatMods.CON,
                    e.StatMods.INT, e.StatMods.WIS, e.StatMods.CHA
                }
            };

            var vcDict = e.VarianceColors;
            if (vcDict != null && vcDict.Count > 0)
            {
                se.varianceColorKeys = new int[vcDict.Count];
                se.varianceColorValuesFlat = new float[vcDict.Count * 4];
                int i = 0;
                foreach (var kvp in vcDict)
                {
                    se.varianceColorKeys[i] = kvp.Key;
                    se.varianceColorValuesFlat[i * 4] = kvp.Value.r;
                    se.varianceColorValuesFlat[i * 4 + 1] = kvp.Value.g;
                    se.varianceColorValuesFlat[i * 4 + 2] = kvp.Value.b;
                    se.varianceColorValuesFlat[i * 4 + 3] = kvp.Value.a;
                    i++;
                }
            }

            if (e.RolledAffixes != null && e.RolledAffixes.Count > 0)
            {
                se.affixes = new SerializedAffix[e.RolledAffixes.Count];
                for (int i = 0; i < e.RolledAffixes.Count; i++)
                {
                    var a = e.RolledAffixes[i];
                    se.affixes[i] = new SerializedAffix
                    {
                        affixId = a.AffixId,
                        statType = a.StatType.ToString(),
                        value = a.Value,
                        isPercentage = a.IsPercentage
                    };
                }
            }

            return se;
        }
    }

    [Serializable]
    public class SerializedAffix
    {
        public string affixId;
        public string statType;
        public float value;
        public bool isPercentage;
    }
}
