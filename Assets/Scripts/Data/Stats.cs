using System;
using Starquill.Core;

namespace Starquill.Data
{
    [Serializable]
    public class Stats
    {
        public int STR;
        public int DEX;
        public int CON;
        public int INT;
        public int WIS;
        public int CHA;

        public int GetStat(StatType type)
        {
            return type switch
            {
                StatType.STR => STR,
                StatType.DEX => DEX,
                StatType.CON => CON,
                StatType.INT => INT,
                StatType.WIS => WIS,
                StatType.CHA => CHA,
                _ => 0
            };
        }

        public void SetStat(StatType type, int value)
        {
            switch (type)
            {
                case StatType.STR: STR = value; break;
                case StatType.DEX: DEX = value; break;
                case StatType.CON: CON = value; break;
                case StatType.INT: INT = value; break;
                case StatType.WIS: WIS = value; break;
                case StatType.CHA: CHA = value; break;
            }
        }

        public int Total => STR + DEX + CON + INT + WIS + CHA;

        public StatType HighestStat()
        {
            int max = STR;
            StatType result = StatType.STR;
            if (DEX > max) { max = DEX; result = StatType.DEX; }
            if (CON > max) { max = CON; result = StatType.CON; }
            if (INT > max) { max = INT; result = StatType.INT; }
            if (WIS > max) { max = WIS; result = StatType.WIS; }
            if (CHA > max) { max = CHA; result = StatType.CHA; }
            return result;
        }

        public Stats Clone()
        {
            return new Stats { STR = this.STR, DEX = this.DEX, CON = this.CON, INT = this.INT, WIS = this.WIS, CHA = this.CHA };
        }

        public static Stats operator +(Stats a, Stats b)
        {
            return new Stats { STR = a.STR + b.STR, DEX = a.DEX + b.DEX, CON = a.CON + b.CON, INT = a.INT + b.INT, WIS = a.WIS + b.WIS, CHA = a.CHA + b.CHA };
        }
    }
}
