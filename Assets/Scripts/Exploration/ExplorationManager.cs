using System;
using Starquill.Data;

namespace Starquill.Exploration
{
    public class ExplorationManager
    {
        public ExplorationState State { get; private set; } = ExplorationState.Exploring;
        public int CurrentWave { get; private set; }
        public int QuestLevel { get; set; } = 1;
        public float FragmentProgress { get; private set; }

        /// 0-1 travel toward a guaranteed quest discovery (the path indicator
        /// bar). Advances per explore wave; arrival forces a discovery if the
        /// random roll never fired. Resets on any discovery.
        public float TravelProgress { get; private set; }

        private readonly EconomyConfig config;
        private readonly Random rng;

        public event Action OnQuestDiscovered;
        public event Action<float> OnFragmentDropped;
        public event Action OnWaveCleared;

        public ExplorationManager(EconomyConfig config, int? seed = null)
        {
            this.config = config;
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void ProcessWaveCleared()
        {
            CurrentWave++;
            OnWaveCleared?.Invoke();

            if (State == ExplorationState.Exploring)
            {
                int wavesToDiscovery = Math.Max(1, config.travelWavesToDiscovery);
                TravelProgress += 1f / wavesToDiscovery;

                bool randomDiscovery = rng.NextDouble() < config.questDiscoveryRate;
                bool arrived = TravelProgress >= 1f;
                if (randomDiscovery || arrived)
                {
                    TravelProgress = 0f;
                    OnQuestDiscovered?.Invoke();
                }
            }

            if (rng.NextDouble() < config.fragmentDropRate)
            {
                float fragment = 1f;
                FragmentProgress += fragment;
                OnFragmentDropped?.Invoke(fragment);
            }
        }

        /// Save restore.
        public void RestoreProgress(float travelProgress, float fragmentProgress)
        {
            TravelProgress = Math.Clamp(travelProgress, 0f, 1f);
            FragmentProgress = Math.Max(0f, fragmentProgress);
        }

        public void EnterQuest() { State = ExplorationState.InQuest; }
        public void EnterDungeon() { State = ExplorationState.InDungeon; }
        public void DungeonEnded() { State = ExplorationState.Exploring; }
        public void QuestCompleted() { State = ExplorationState.Exploring; CurrentWave = 0; }
        public void QuestRetreated() { State = ExplorationState.QuestRetreat; }
        public void RetryQuest() { State = ExplorationState.InQuest; }
        public void DismissRetry() { State = ExplorationState.Exploring; }
    }
}
