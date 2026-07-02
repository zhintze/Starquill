using System;

namespace Starquill.UI
{
    /// Pure formatting for the shop screen and offline claim sheet.
    public static class ShopPresenter
    {
        /// "4:32" under an hour, "2h 05m" above, "8h+" capped style not needed.
        public static string Duration(double seconds)
        {
            if (seconds <= 0) return "0:00";
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}h {t.Minutes:D2}m";
            return $"{t.Minutes}:{t.Seconds:D2}";
        }

        public static string BoostButtonLabel(double cost, bool active, double remainingSeconds)
        {
            return active
                ? $"Active · {Duration(remainingSeconds)}"
                : $"Buy · {NumberFormatter.FormatCompact(cost)}g";
        }

        public static bool BoostBuyEnabled(double cost, double gold, bool active) =>
            !active && gold >= cost;

        public static string ChestLabel(double readyAt, double now)
        {
            return now >= readyAt ? "READY" : $"Opens in {Duration(readyAt - now)}";
        }

        public static string OfflineSummary(float awaySeconds, double gold)
        {
            return $"Away {Duration(awaySeconds)} · +{NumberFormatter.FormatCompact(gold)} gold";
        }
    }
}
