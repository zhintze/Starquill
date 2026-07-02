using System;

namespace Starquill.Services
{
    /// IAP abstraction. Only one product for MVP: Remove Ads (gates
    /// interstitials; rewarded placements remain). Real Unity Purchasing 5.x
    /// store wiring is a Sprint 12 device task behind this interface —
    /// ownership persistence lives with the caller (save data).
    public interface IIapService
    {
        void PurchaseRemoveAds(Action<bool> onResult);
        void RestorePurchases(Action<bool> onOwned);
    }

    /// Editor/test implementation: purchases always succeed.
    public class MockIapService : IIapService
    {
        public void PurchaseRemoveAds(Action<bool> onResult) => onResult?.Invoke(true);
        public void RestorePurchases(Action<bool> onOwned) => onOwned?.Invoke(false);
    }
}
