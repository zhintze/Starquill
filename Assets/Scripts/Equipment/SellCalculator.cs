using Starquill.Core;

namespace Starquill.Equipment
{
    public static class SellCalculator
    {
        private static readonly double[] RarityBaseValues = { 10, 25, 75, 200, 500 };

        public static double GetSellValue(EquipmentInstance item, int questLevel)
        {
            int rarityIdx = (int)item.Rarity;
            double baseValue = rarityIdx < RarityBaseValues.Length
                ? RarityBaseValues[rarityIdx] : RarityBaseValues[0];

            double value = baseValue * questLevel;

            if (item.Ability != null)
                value *= 1.0 + item.Ability.Level * 0.15;

            return value;
        }
    }
}
