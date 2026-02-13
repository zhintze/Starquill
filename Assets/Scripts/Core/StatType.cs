namespace Starquill.Core
{
    public enum StatType
    {
        STR,
        DEX,
        CON,
        INT,
        WIS,
        CHA
    }

    public static class StatTypeExtensions
    {
        public static VerbCategory GetCategory(this StatType stat)
        {
            return stat switch
            {
                StatType.STR or StatType.DEX or StatType.CON => VerbCategory.Physical,
                StatType.INT or StatType.WIS or StatType.CHA => VerbCategory.Mental,
                _ => VerbCategory.Physical
            };
        }
    }
}
