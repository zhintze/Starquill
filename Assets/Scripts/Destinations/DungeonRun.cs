using System;

namespace Starquill.Destinations
{
    /// Live state of a timed rush. Pure (no UnityEngine time) for tests;
    /// GameManager feeds Tick(deltaTime).
    public class DungeonRun
    {
        public DungeonSpec Spec { get; }
        public float TimeLeft { get; private set; }
        public int WavesCleared { get; private set; }
        public bool IsOver => TimeLeft <= 0f;

        public DungeonRun(DungeonSpec spec)
        {
            Spec = spec;
            TimeLeft = spec.DurationSeconds;
        }

        public void Tick(float deltaSeconds) => TimeLeft = Math.Max(0f, TimeLeft - deltaSeconds);
        public void WaveCleared() => WavesCleared++;

        public int BonusRolls(int wavesPerRoll, int cap)
        {
            int over = WavesCleared - Spec.ParWaves;
            if (over <= 0) return 0;
            return Math.Min(cap, over / Math.Max(1, wavesPerRoll));
        }
    }
}
