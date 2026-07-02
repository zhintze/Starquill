using System;
using System.Collections.Generic;

namespace Starquill.Combat
{
    /// Timed boost state. Time is always injected (unix seconds) so logic is
    /// deterministic and testable; persistence is just the expiry values.
    public class BoostManager
    {
        private readonly Dictionary<BoostType, double> expiries = new();

        /// Re-buying while active extends from the current expiry.
        public void Activate(BoostType type, double durationSeconds, double now)
        {
            double start = Math.Max(now, GetExpiry(type));
            expiries[type] = start + durationSeconds;
        }

        public bool IsActive(BoostType type, double now) => GetExpiry(type) > now;

        public double Remaining(BoostType type, double now) =>
            Math.Max(0, GetExpiry(type) - now);

        public double GetExpiry(BoostType type) =>
            expiries.TryGetValue(type, out var e) ? e : 0;

        public void RestoreExpiry(BoostType type, double expiry)
        {
            expiries[type] = expiry;
        }
    }
}
