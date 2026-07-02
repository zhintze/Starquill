using System;

namespace Starquill.Services
{
    public static class AdPlacement
    {
        public const string Offline2x = "offline2x";
        public const string ChestDouble = "chestDouble";
    }

    /// Rewarded-ad abstraction. UI asks IsReady, shows via ShowRewarded, and
    /// grants the reward only when the callback reports success.
    /// Implementations: MockAdService (editor), UnityAdsService (device),
    /// LevelPlay mediation post-MVP behind this same interface.
    public interface IAdService
    {
        bool IsReady(string placement);
        void ShowRewarded(string placement, Action<bool> onResult);
    }

    /// Editor/test implementation: always ready, always succeeds.
    public class MockAdService : IAdService
    {
        public bool IsReady(string placement) => true;

        public void ShowRewarded(string placement, Action<bool> onResult)
            => onResult?.Invoke(true);
    }
}
