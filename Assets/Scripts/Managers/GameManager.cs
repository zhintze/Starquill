using System.Collections.Generic;
using UnityEngine;
using Starquill.Characters;
using Starquill.Combat;
using Starquill.Data;
using Starquill.Equipment;
using Starquill.Exploration;

namespace Starquill.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        public EconomyConfig economyConfig;
        public AdvantageMatrix advantageMatrix;

        [Header("State")]
        public double gold;
        public int questLevel = 1;

        private Party party;
        private VerbPool verbPool;
        private CombatTickProcessor combatProcessor;
        private ExplorationManager exploration;
        private PityTracker pityTracker;
        private SaveManager saveManager;
        private List<EnemyState> currentEnemies = new();
        private float tickTimer;
        private float currentTime;

        public Party Party => party;
        public VerbPool VerbPool => verbPool;
        public ExplorationManager Exploration => exploration;

        private void Awake()
        {
            saveManager = GetComponent<SaveManager>();
            if (saveManager == null)
                saveManager = gameObject.AddComponent<SaveManager>();

            party = new Party();
            verbPool = new VerbPool(economyConfig.verbSlotCount, economyConfig.verbDrawCooldown);
            combatProcessor = new CombatTickProcessor(advantageMatrix, economyConfig);
            exploration = new ExplorationManager(economyConfig);
            pityTracker = new PityTracker();
        }

        private void Start()
        {
            var save = saveManager.Load();
            gold = save.gold;
            questLevel = save.currentQuestLevel;

            float offlineSeconds = saveManager.GetOfflineSeconds();
            if (offlineSeconds > 0)
            {
                float goldPerSecond = economyConfig.GoldPerKill(questLevel) * economyConfig.exploreKillsPerMinute / 60f;
                float offlineGold = economyConfig.OfflineGold(goldPerSecond, offlineSeconds);
                // TODO: Show claim screen instead of auto-adding
            }

            SpawnWave();
            RebuildVerbPool();
        }

        private void Update()
        {
            tickTimer += Time.deltaTime;
            currentTime += Time.deltaTime;

            if (tickTimer >= 1f)
            {
                tickTimer -= 1f;
                ProcessTick();
            }
        }

        private void ProcessTick()
        {
            verbPool.RotateStaleVerbs(currentTime);
            verbPool.FillSlots(currentTime);
            verbPool.TickCooldowns();

            var result = combatProcessor.ProcessTick(
                currentEnemies, party.GetAllStats(), null,
                questLevel, economyConfig.prestigeMultiplierBase);

            gold += result.GoldEarned;

            if (result.WaveCleared)
            {
                exploration.ProcessWaveCleared();
                SpawnWave();
            }

            if ((int)currentTime % 30 == 0)
            {
                saveManager.CurrentSave.gold = gold;
                saveManager.CurrentSave.currentQuestLevel = questLevel;
                saveManager.Save();
            }
        }

        public void OnVerbTapped(int slotIndex)
        {
            var activated = verbPool.ActivateVerb(slotIndex);
            if (activated == null) return;

            var result = combatProcessor.ProcessTick(
                currentEnemies, party.GetAllStats(), activated,
                questLevel, economyConfig.prestigeMultiplierBase);

            gold += result.GoldEarned;

            if (result.WaveCleared)
            {
                exploration.ProcessWaveCleared();
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            currentEnemies.Clear();
            int enemyCount = 2 + questLevel / 10;
            if (enemyCount > 6) enemyCount = 6;

            var statTypes = new[] {
                Core.StatType.STR, Core.StatType.DEX, Core.StatType.CON,
                Core.StatType.INT, Core.StatType.WIS, Core.StatType.CHA
            };

            for (int i = 0; i < enemyCount; i++)
            {
                var type = statTypes[Random.Range(0, statTypes.Length)];
                float hp = economyConfig.EnemyHP(questLevel);
                currentEnemies.Add(new EnemyState($"enemy_{i}", type, hp, hp * 0.05f));
            }
        }

        private void RebuildVerbPool()
        {
            verbPool.Clear();
            for (int i = 0; i < party.Members.Count; i++)
                verbPool.AddVerbs(i, party.Members[i].equippedVerbs);
            verbPool.FillSlots(currentTime);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                saveManager.CurrentSave.gold = gold;
                saveManager.CurrentSave.currentQuestLevel = questLevel;
                saveManager.Save();
            }
        }

        private void OnApplicationQuit()
        {
            saveManager.CurrentSave.gold = gold;
            saveManager.CurrentSave.currentQuestLevel = questLevel;
            saveManager.Save();
        }
    }
}
