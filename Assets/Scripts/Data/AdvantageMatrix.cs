using System;
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Advantage Matrix")]
    public class AdvantageMatrix : ScriptableObject
    {
        private static readonly float[,] DamageMatrix = {
            { 1.0f, 1.5f,  0.67f, 1.2f, 1.2f, 1.2f },
            { 0.67f, 1.0f, 1.5f,  1.2f, 1.2f, 1.2f },
            { 1.5f, 0.67f, 1.0f,  1.2f, 1.2f, 1.2f },
            { 0.8f, 0.8f,  0.8f,  1.0f, 1.5f, 0.67f },
            { 0.8f, 0.8f,  0.8f,  0.67f, 1.0f, 1.5f },
            { 0.8f, 0.8f,  0.8f,  1.5f, 0.67f, 1.0f },
        };

        private static readonly float[,] ProcMatrix = {
            { 0.5f, 0.5f,  0.5f,  0.0f, 0.0f, 0.0f },
            { 0.5f, 0.5f,  0.5f,  0.0f, 0.0f, 0.0f },
            { 0.5f, 0.5f,  0.5f,  0.0f, 0.0f, 0.0f },
            { 1.0f, 1.0f,  1.0f,  0.5f, 0.5f, 0.5f },
            { 1.0f, 1.0f,  1.0f,  0.5f, 0.5f, 0.5f },
            { 1.0f, 1.0f,  1.0f,  0.5f, 0.5f, 0.5f },
        };

        public MatchupResult GetMatchup(StatType attacker, StatType defender)
        {
            int a = (int)attacker;
            int d = (int)defender;
            float dmgMult = DamageMatrix[a, d];
            float procMod = ProcMatrix[a, d];

            Advantage advantage;
            if (dmgMult >= 1.4f) advantage = Advantage.Strong;
            else if (dmgMult <= 0.7f) advantage = Advantage.Weak;
            else advantage = Advantage.Neutral;

            return new MatchupResult { damageMultiplier = dmgMult, statusProcModifier = procMod, advantage = advantage };
        }
    }

    [Serializable]
    public struct MatchupResult
    {
        public float damageMultiplier;
        public float statusProcModifier;
        public Advantage advantage;
    }
}
