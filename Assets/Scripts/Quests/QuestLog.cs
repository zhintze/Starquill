namespace Starquill.Quests
{
    public enum QuestPhase
    {
        Idle,
        Offered,
        Active,
        Retreated
    }

    /// Quest progression state machine. Pure C#: invalid transitions return
    /// false (or 0 for Retreat) and never mutate state or throw.
    ///
    /// Idle → Offered → Active(wave n) → Completed → Idle (next quest)
    ///          │Decline      │Retreat ⇄ Retry (wave 1)
    ///          ▼             ▼Dismiss
    ///        Idle          Idle
    public class QuestLog
    {
        public int ZoneIndex { get; private set; }
        public int NextQuestIndex { get; private set; }
        public QuestPhase Phase { get; private set; } = QuestPhase.Idle;
        public int ActiveWaveIndex { get; private set; }
        public double GoldEarnedInQuest { get; private set; }
        public QuestSpec ActiveSpec { get; private set; }

        public bool IsOnLastWave =>
            Phase == QuestPhase.Active && ActiveSpec != null
            && ActiveWaveIndex == ActiveSpec.Waves.Length - 1;

        public bool TryOffer(QuestSpec spec)
        {
            if (Phase != QuestPhase.Idle || spec == null) return false;
            ActiveSpec = spec;
            Phase = QuestPhase.Offered;
            return true;
        }

        public bool Accept()
        {
            if (Phase != QuestPhase.Offered) return false;
            Phase = QuestPhase.Active;
            ActiveWaveIndex = 0;
            GoldEarnedInQuest = 0;
            return true;
        }

        public bool Decline()
        {
            if (Phase != QuestPhase.Offered) return false;
            ActiveSpec = null;
            Phase = QuestPhase.Idle;
            return true;
        }

        /// Advances to the next wave; false when already on the last wave
        /// (the caller should Complete instead).
        public bool AdvanceWave()
        {
            if (Phase != QuestPhase.Active || ActiveSpec == null) return false;
            if (ActiveWaveIndex >= ActiveSpec.Waves.Length - 1) return false;
            ActiveWaveIndex++;
            return true;
        }

        public void RecordGold(double amount)
        {
            if (Phase != QuestPhase.Active) return;
            GoldEarnedInQuest += amount;
        }

        /// Active → Retreated. Returns the gold to DEDUCT from the player
        /// (half of what was earned during the attempt); 0 when not active.
        public double Retreat()
        {
            if (Phase != QuestPhase.Active) return 0;
            Phase = QuestPhase.Retreated;
            return GoldEarnedInQuest * 0.5;
        }

        public bool Retry()
        {
            if (Phase != QuestPhase.Retreated) return false;
            Phase = QuestPhase.Active;
            ActiveWaveIndex = 0;
            GoldEarnedInQuest = 0;
            return true;
        }

        public bool Dismiss()
        {
            if (Phase != QuestPhase.Retreated) return false;
            ActiveSpec = null;
            Phase = QuestPhase.Idle;
            return true;
        }

        public bool Complete()
        {
            if (!IsOnLastWave) return false;

            bool wasBoss = ActiveSpec.Tier == QuestTier.Boss;
            ActiveSpec = null;
            Phase = QuestPhase.Idle;
            ActiveWaveIndex = 0;
            GoldEarnedInQuest = 0;

            if (wasBoss)
            {
                ZoneIndex++;
                NextQuestIndex = 0;
            }
            else
            {
                NextQuestIndex++;
            }
            return true;
        }

        public void Restore(int zoneIndex, int nextQuestIndex, QuestPhase phase,
            int activeWaveIndex, double goldEarned, QuestSpec spec)
        {
            ZoneIndex = zoneIndex;
            NextQuestIndex = nextQuestIndex;
            Phase = phase;
            ActiveWaveIndex = activeWaveIndex;
            GoldEarnedInQuest = goldEarned;
            ActiveSpec = spec;
        }
    }
}
