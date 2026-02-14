using Starquill.Core;
using UnityEngine;

namespace Starquill.UI
{
    public static class StatTypeColors
    {
        public static Color GetColor(StatType stat)
        {
            return stat switch
            {
                StatType.STR => new Color(0.9f, 0.2f, 0.2f),
                StatType.DEX => new Color(0.2f, 0.8f, 0.3f),
                StatType.CON => new Color(0.7f, 0.5f, 0.2f),
                StatType.INT => new Color(0.3f, 0.4f, 0.9f),
                StatType.WIS => new Color(0.6f, 0.3f, 0.8f),
                StatType.CHA => new Color(0.9f, 0.8f, 0.2f),
                _ => Color.white
            };
        }

        public static Color GetAdvantageColor(Advantage advantage)
        {
            return advantage switch
            {
                Advantage.Strong => new Color(0.2f, 0.9f, 0.3f),
                Advantage.Weak => new Color(0.9f, 0.3f, 0.2f),
                _ => Color.white
            };
        }
    }
}
