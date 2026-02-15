using System;
using System.Collections.Generic;
using UnityEngine;
using Starquill.Characters;
using Starquill.Combat;
using Starquill.Core;
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

            if (party.Members.Count == 0)
                InitializeStarterParty();

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

        private void InitializeStarterParty()
        {
            var warrior = new CharacterInstance
            {
                id = "starter_warrior",
                displayName = "Warrior",
                baseStats = new Stats { STR = 14, DEX = 10, CON = 12, INT = 8, WIS = 9, CHA = 10 },
                level = 1
            };
            warrior.equippedVerbs.Add(CreateVerb("slash", "Slash", StatType.STR, 50f, 2f));
            warrior.equippedVerbs.Add(CreateVerb("shield_bash", "Shield Bash", StatType.CON, 30f, 3f));

            var mage = new CharacterInstance
            {
                id = "starter_mage",
                displayName = "Mage",
                baseStats = new Stats { STR = 7, DEX = 9, CON = 8, INT = 15, WIS = 12, CHA = 10 },
                level = 1
            };
            mage.equippedVerbs.Add(CreateVerb("fireball", "Fireball", StatType.INT, 65f, 3f));
            mage.equippedVerbs.Add(CreateVerb("heal", "Heal", StatType.WIS, 0f, 4f, isHealing: true, healAmount: 40f));

            party.AddMember(warrior);
            party.AddMember(mage);
        }

        private static VerbDefinition CreateVerb(string id, string name, StatType type,
            float damage, float cooldown, bool isHealing = false, float healAmount = 0f)
        {
            var verb = ScriptableObject.CreateInstance<VerbDefinition>();
            verb.verbId = id;
            verb.displayName = name;
            verb.statType = type;
            verb.baseDamage = damage;
            verb.cooldownTicks = cooldown;
            verb.isHealingVerb = isHealing;
            verb.healAmount = healAmount;
            return verb;
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
                var type = statTypes[UnityEngine.Random.Range(0, statTypes.Length)];
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
