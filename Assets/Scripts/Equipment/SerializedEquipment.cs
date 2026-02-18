using System;
using System.Collections.Generic;
using Starquill.Core;
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
        public string primaryStatType;
        public int primaryValue;
        public string secondaryStatType;
        public int secondaryValue;
        public SerializedAbility ability;
        public int[] layerVariants;

        public static SerializedEquipment FromInstance(EquipmentInstance e)
        {
            var se = new SerializedEquipment
            {
                itemType = e.ItemType,
                itemNum = e.ItemNum,
                slot = (int)e.Slot,
                rarity = (int)e.Rarity,
                baseColor = new[] { e.BaseColor.r, e.BaseColor.g, e.BaseColor.b, e.BaseColor.a },
                primaryStatType = e.PrimaryStat.ToString(),
                primaryValue = e.PrimaryValue,
                secondaryStatType = e.SecondaryStat.ToString(),
                secondaryValue = e.SecondaryValue,
                layerVariants = e.LayerVariants
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

            if (e.Ability != null)
            {
                se.ability = new SerializedAbility
                {
                    abilityId = e.Ability.AbilityId,
                    level = e.Ability.Level,
                    currentXP = e.Ability.CurrentXP
                };
            }

            return se;
        }
    }

    [Serializable]
    public class SerializedAbility
    {
        public string abilityId;
        public int level;
        public float currentXP;
    }
}
