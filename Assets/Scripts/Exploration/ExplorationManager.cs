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

            if (State == ExplorationState.Exploring && rng.NextDouble() < config.questDiscoveryRate)
                OnQuestDiscovered?.Invoke();

            if (rng.NextDouble() < config.fragmentDropRate)
            {
                float fragment = 1f;
                FragmentProgress += fragment;
                OnFragmentDropped?.Invoke(fragment);
            }
        }

        public void EnterQuest() { State = ExplorationState.InQuest; }
        public void QuestCompleted() { State = ExplorationState.Exploring; CurrentWave = 0; }
        public void QuestRetreated() { State = ExplorationState.QuestRetreat; }
        public void RetryQuest() { State = ExplorationState.InQuest; }
        public void DismissRetry() { State = ExplorationState.Exploring; }
    }
}
