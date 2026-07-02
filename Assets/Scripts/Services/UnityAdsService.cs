using System;
using System.Collections.Generic;
using UnityEngine.Advertisements;

namespace Starquill.Services
{
    /// Unity Ads (com.unity.ads) rewarded implementation. Game IDs are
    /// placeholders until store setup (Sprint 12); test mode stays on until
    /// then. Placement IDs must exist in the Unity Ads dashboard.
    public class UnityAdsService : IAdService,
        IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        private const string AndroidGameId = "0000000"; // TODO ship: real game id
        private const bool TestMode = true;             // TODO ship: false

        private readonly HashSet<string> loaded = new();
        private readonly Dictionary<string, Action<bool>> pending = new();

        public UnityAdsService()
        {
            if (!Advertisement.isInitialized && Advertisement.isSupported)
                Advertisement.Initialize(AndroidGameId, TestMode, this);
        }

        public bool IsReady(string placement) => loaded.Contains(placement);

        public void ShowRewarded(string placement, Action<bool> onResult)
        {
            if (!loaded.Contains(placement))
            {
                Advertisement.Load(placement, this);
                onResult?.Invoke(false);
                return;
            }

            pending[placement] = onResult;
            loaded.Remove(placement);
            Advertisement.Show(placement, this);
        }

        public void ShowInterstitial(string placement)
        {
            if (!loaded.Contains(placement))
            {
                Advertisement.Load(placement, this);
                return;
            }
            loaded.Remove(placement);
            Advertisement.Show(placement, this);
        }

        // --- initialization ---
        public void OnInitializationComplete()
        {
            Advertisement.Load(AdPlacement.Offline2x, this);
            Advertisement.Load(AdPlacement.ChestDouble, this);
            Advertisement.Load(AdPlacement.Interstitial, this);
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message) { }

        // --- load ---
        public void OnUnityAdsAdLoaded(string placementId) => loaded.Add(placementId);
        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message) { }

        // --- show ---
        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state)
        {
            Resolve(placementId, state == UnityAdsShowCompletionState.COMPLETED);
            Advertisement.Load(placementId, this); // reload for next time
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
            => Resolve(placementId, false);

        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }

        private void Resolve(string placementId, bool success)
        {
            if (pending.TryGetValue(placementId, out var cb))
            {
                pending.Remove(placementId);
                cb?.Invoke(success);
            }
        }
    }

    public static class AdServiceFactory
    {
        public static IAdService Create()
        {
#if UNITY_EDITOR
            return new MockAdService();
#else
            return new UnityAdsService();
#endif
        }
    }
}
