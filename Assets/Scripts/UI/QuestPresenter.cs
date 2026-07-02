using Starquill.Quests;
using UnityEngine;

namespace Starquill.UI
{
    public enum LadderNodeState
    {
        Done,
        Current,
        Ahead,
        Boss,
        BossCurrent
    }

    /// Pure formatting for quest UI — the unit-testable layer between the
    /// Sprint 9 backend and the banner/sheets/screen.
    public static class QuestPresenter
    {
        public static string BannerText(QuestPhase phase, QuestSpec spec, int waveIndex)
        {
            switch (phase)
            {
                case QuestPhase.Offered:
                    return "Quest discovered · tap to view";
                case QuestPhase.Retreated:
                    return spec != null ? $"Retry {spec.DisplayName}" : "";
                case QuestPhase.Active:
                {
                    if (spec == null || spec.Waves.Length == 0) return "";
                    bool boss = waveIndex >= 0 && waveIndex < spec.Waves.Length
                        && spec.Waves[waveIndex].IsBossWave;
                    return boss
                        ? $"{spec.DisplayName} · BOSS"
                        : $"{spec.DisplayName} · Wave {waveIndex + 1}/{spec.Waves.Length}";
                }
                default:
                    return "";
            }
        }

        public static float WaveFraction(QuestSpec spec, int waveIndex)
        {
            if (spec == null || spec.Waves.Length <= 1) return 0f;
            return Mathf.Clamp01((float)waveIndex / (spec.Waves.Length - 1));
        }

        public static string RewardsPreview(QuestRewardSpec reward, double estimatedGold)
        {
            if (reward == null) return "";
            string gold = $"~{NumberFormatter.FormatCompact(estimatedGold)}g";
            string loot = reward.RarityFloor.HasValue
                ? $"Guaranteed {reward.RarityFloor.Value}+ item"
                : "Loot chance";
            string bonus = reward.ExtraNoFloorRolls > 0 ? " + bonus item" : "";
            return $"{loot}{bonus} · {gold}";
        }

        public static LadderNodeState[] LadderStates(int nextQuestIndex, QuestPhase phase)
        {
            var states = new LadderNodeState[QuestZone.QuestsPerZone];
            for (int i = 0; i < states.Length; i++)
            {
                bool isBossNode = i == QuestZone.QuestsPerZone - 1;
                if (i < nextQuestIndex)
                    states[i] = LadderNodeState.Done;
                else if (i == nextQuestIndex)
                    states[i] = isBossNode ? LadderNodeState.BossCurrent : LadderNodeState.Current;
                else
                    states[i] = isBossNode ? LadderNodeState.Boss : LadderNodeState.Ahead;
            }
            return states;
        }

        public static string PickDialogue(string[] lines, int questIndex)
        {
            if (lines == null || lines.Length == 0) return "";
            int idx = ((questIndex % lines.Length) + lines.Length) % lines.Length;
            return lines[idx];
        }

        public static string TierLabel(QuestTier tier)
        {
            return tier == QuestTier.Boss ? "BOSS" : tier.ToString();
        }
    }
}
