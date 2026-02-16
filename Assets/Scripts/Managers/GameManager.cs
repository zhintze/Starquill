using System;
using System.Collections.Generic;
using UnityEngine;
using Starquill.Characters;
using Starquill.Combat;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
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
        public event Action OnRosterChanged;

        private Party party;
        private VerbPool verbPool;
        private CombatTickProcessor combatProcessor;
        private ExplorationManager exploration;
        private PityTracker pityTracker;
        private SaveManager saveManager;
        private CharacterRoster roster;
        private CharacterFactory characterFactory;
        private EquipmentCatalog equipmentCatalog;
        private EquipmentFactory equipmentFactory;
        private LootInventory lootInventory;
        private LootDropper lootDropper;
        private List<EnemyState> currentEnemies = new();
        private float tickTimer;
        private float currentTime;
        private float verbLockTimer;

        public Party Party => party;
        public VerbPool VerbPool => verbPool;
        public ExplorationManager Exploration => exploration;
        public IReadOnlyList<EnemyState> CurrentEnemies => currentEnemies;
        public bool VerbsLocked => verbLockTimer > 0f;
        public CharacterRoster Roster => roster;
        public LootInventory LootInventory => lootInventory;
        public event Action<EquipmentInstance> OnLootDropped;

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

            // Initialize equipment system
            equipmentCatalog = new EquipmentCatalog();
            equipmentCatalog.LoadFromResources();

            var affixTable = new AffixTable();
            affixTable.LoadFromResources();

            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();

            equipmentFactory = new EquipmentFactory(equipmentCatalog, affixTable, registry.Colors);

            var nameGen = new NameGenerator();
            nameGen.LoadFromResources();

            characterFactory = new CharacterFactory(registry, equipmentFactory, nameGen);
            roster = new CharacterRoster();

            party = new Party();
            verbPool = new VerbPool(economyConfig.verbSlotCount, economyConfig.verbDrawCooldown);
            combatProcessor = new CombatTickProcessor(advantageMatrix, economyConfig);
            exploration = new ExplorationManager(economyConfig);
            pityTracker = new PityTracker();

            lootInventory = new LootInventory(50);
            lootDropper = new LootDropper(equipmentFactory, economyConfig, pityTracker);
        }

        private void Start()
        {
            var save = saveManager.Load();
            gold = save.gold;
            questLevel = save.currentQuestLevel;

            if (save.NeedsRosterInitialization())
                InitializeStarterRoster();
            else
                LoadRosterFromSave(save);

            if (save.inventory != null)
            {
                foreach (var si in save.inventory)
                {
                    if (si != null && !string.IsNullOrEmpty(si.itemType))
                        lootInventory.AddItem(equipmentFactory.Reconstruct(si));
                }
            }

            BuildPartyFromRoster();
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

            ProcessLootDrops(result.EnemiesKilled);

            OnCombatTick?.Invoke(result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
                exploration.ProcessWaveCleared();
                SpawnWave();
            }

            if ((int)currentTime % 30 == 0)
                SaveState();
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

            ProcessLootDrops(result.EnemiesKilled);

            OnVerbActivated?.Invoke(slotIndex, result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
                exploration.ProcessWaveCleared();
                SpawnWave();
            }
        }

        private void InitializeStarterRoster()
        {
            var rng = new System.Random();
            var characters = characterFactory.CreateStarterRoster(8, rng);
            foreach (var c in characters)
                roster.AddCharacter(c);
            roster.InitializeDefaultParty();
        }

        private void LoadRosterFromSave(SaveData save)
        {
            foreach (var sc in save.roster)
            {
                var character = sc.ToInstance();
                for (int i = 0; i < sc.equipment.Length; i++)
                {
                    if (sc.equipment[i] != null && !string.IsNullOrEmpty(sc.equipment[i].itemType))
                        character.equipment[i] = equipmentFactory.Reconstruct(sc.equipment[i]);
                }
                foreach (var verbId in sc.equippedVerbIds)
                {
                    var verb = CharacterFactory.CreateVerbById(verbId);
                    if (verb != null) character.equippedVerbs.Add(verb);
                }
                roster.AddCharacter(character);
            }
            for (int i = 0; i < save.activePartyIndices.Length && i < 4; i++)
                roster.ActivePartyIndices[i] = save.activePartyIndices[i];
        }

        public void BuildPartyFromRoster()
        {
            party = new Party();
            var activeParty = roster.GetActiveParty();
            foreach (var c in activeParty)
                if (c != null) party.AddMember(c);
        }

        private void SaveState()
        {
            if (saveManager == null || roster == null || lootInventory == null) return;
            saveManager.CurrentSave.gold = gold;
            saveManager.CurrentSave.currentQuestLevel = questLevel;
            saveManager.CurrentSave.roster.Clear();
            foreach (var c in roster.Characters)
                saveManager.CurrentSave.roster.Add(SerializedCharacter.FromInstance(c));
            saveManager.CurrentSave.activePartyIndices = (int[])roster.ActivePartyIndices.Clone();
            saveManager.CurrentSave.inventory.Clear();
            foreach (var item in lootInventory.Items)
                saveManager.CurrentSave.inventory.Add(SerializedEquipment.FromInstance(item));
            saveManager.Save();
        }

        private void SpawnWave()
        {
            currentEnemies.Clear();
            int enemyCount = 2 + questLevel / 10;
            if (enemyCount > 6) enemyCount = 6;

            var statTypes = new[] {
                StatType.STR, StatType.DEX, StatType.CON,
                StatType.INT, StatType.WIS, StatType.CHA
            };

            for (int i = 0; i < enemyCount; i++)
            {
                var type = statTypes[UnityEngine.Random.Range(0, statTypes.Length)];
                float hp = economyConfig.EnemyHP(questLevel);
                currentEnemies.Add(new EnemyState($"enemy_{i}", type, hp, hp * 0.05f));
            }

            OnWaveStarted?.Invoke(currentEnemies);
        }

        public void RebuildVerbPool()
        {
            verbPool.Clear();
            for (int i = 0; i < party.Members.Count; i++)
                verbPool.AddVerbs(i, party.Members[i].equippedVerbs);
            verbPool.FillSlots(currentTime);
        }

        private void ProcessLootDrops(int killCount)
        {
            for (int k = 0; k < killCount; k++)
            {
                var rng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
                var drop = lootDropper.TryDrop(questLevel, rng);
                if (drop != null && lootInventory.AddItem(drop))
                    OnLootDropped?.Invoke(drop);
            }
        }

        public void SellItem(EquipmentInstance item)
        {
            if (item == null || !lootInventory.RemoveItem(item)) return;
            double value = SellCalculator.GetSellValue(item, questLevel);
            gold += value;
            OnGoldChanged?.Invoke(gold);
        }

        public void EquipItemFromInventory(EquipmentInstance item, int characterIndex)
        {
            if (item == null || roster == null) return;
            if (characterIndex < 0 || characterIndex >= roster.Characters.Count) return;

            var character = roster.Characters[characterIndex];
            var oldItem = character.equipment[(int)item.Slot];

            if (!lootInventory.RemoveItem(item)) return;

            if (oldItem != null)
                lootInventory.AddItem(oldItem);

            character.EquipItem(item);
            BuildPartyFromRoster();
            OnRosterChanged?.Invoke();
        }

        public int AutoEquipCharacter(int characterIndex)
        {
            if (roster == null || characterIndex < 0 || characterIndex >= roster.Characters.Count) return 0;
            var character = roster.Characters[characterIndex];
            var result = AutoEquipper.AutoEquip(character, lootInventory);
            if (result.ItemsEquipped > 0)
            {
                BuildPartyFromRoster();
                OnRosterChanged?.Invoke();
            }
            return result.ItemsEquipped;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveState();
        }

        private void OnApplicationQuit()
        {
            SaveState();
        }
    }
}
