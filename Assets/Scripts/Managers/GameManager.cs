using System;
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
        public static GameManager Instance { get; private set; }

        [Header("Config")]
        public EconomyConfig economyConfig;
        public AdvantageMatrix advantageMatrix;

        [Header("State")]
        public double gold;
        public int questLevel = 1;

        public event Action<CombatTickResult> OnCombatTick;
        public event Action<double> OnGoldChanged;
        public event Action<IReadOnlyList<EnemyState>> OnWaveStarted;
        public event Action OnWaveCleared;
        public event Action<int, CombatTickResult> OnVerbActivated;

        private Party party;
        private VerbPool verbPool;
        private CombatTickProcessor combatProcessor;
        private ExplorationManager exploration;
        private PityTracker pityTracker;
        private SaveManager saveManager;
        private List<EnemyState> currentEnemies = new();
        private float tickTimer;
        private float currentTime;
        private float verbLockTimer;

        public Party Party => party;
        public VerbPool VerbPool => verbPool;
        public ExplorationManager Exploration => exploration;
        public IReadOnlyList<EnemyState> CurrentEnemies => currentEnemies;
        public bool VerbsLocked => verbLockTimer > 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

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
            if (verbLockTimer > 0f) verbLockTimer -= Time.deltaTime;

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
            verbPool.FillSlots(currentTime);
            verbPool.TickCooldowns();

            var result = combatProcessor.ProcessTick(
                currentEnemies, party.GetAllStats(), null,
                questLevel, economyConfig.prestigeMultiplierBase);

            if (result.GoldEarned > 0)
            {
                gold += result.GoldEarned;
                OnGoldChanged?.Invoke(gold);
            }

            OnCombatTick?.Invoke(result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
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
            if (VerbsLocked) return;

            var activated = verbPool.ActivateVerb(slotIndex);
            if (activated == null) return;

            verbPool.IncrementPassCounts(slotIndex);
            verbPool.ReplaceStaleVerbs(currentTime);
            verbPool.FillSlots(currentTime);

            verbLockTimer = economyConfig.verbLockDuration;

            var result = combatProcessor.ProcessTick(
                currentEnemies, party.GetAllStats(), activated,
                questLevel, economyConfig.prestigeMultiplierBase);

            if (result.GoldEarned > 0)
            {
                gold += result.GoldEarned;
                OnGoldChanged?.Invoke(gold);
            }

            OnVerbActivated?.Invoke(slotIndex, result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
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

            OnWaveStarted?.Invoke(currentEnemies);
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
