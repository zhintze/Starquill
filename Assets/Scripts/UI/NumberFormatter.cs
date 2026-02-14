namespace Starquill.UI
{
    public static class NumberFormatter
    {
        public static string FormatCompact(double value)
        {
            if (value == 0) return "0";
            if (value < 1000) return ((int)value).ToString();
            if (value < 1_000_000) return $"{value / 1_000:0.0}K";
            if (value < 1_000_000_000) return $"{value / 1_000_000:0.0}M";
            return $"{value / 1_000_000_000:0.0}B";
        }

        public static string FormatGoldDrop(double value)
        {
            if (value < 1000) return $"+{(int)value}g";
            return $"+{FormatCompact(value)}g";
        }
    }
}
