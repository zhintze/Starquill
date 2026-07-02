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
using Starquill.Quests;

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
        private AbilityTable abilityTable;
        private QuestZoneTable questZones;
        private readonly QuestLog questLog = new();
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
        public AbilityTable AbilityTable => abilityTable;
        public QuestLog QuestLog => questLog;
        public QuestZoneTable QuestZones => questZones;
        public event Action<QuestSpec> OnQuestOffered;
        public event Action<QuestSpec, double, List<EquipmentInstance>> OnQuestCompleted;
        public event Action OnQuestRetreated;
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

            abilityTable = new AbilityTable();
            abilityTable.LoadFromResources();

            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();

            equipmentFactory = new EquipmentFactory(equipmentCatalog, abilityTable, registry.Colors);

            var nameGen = new NameGenerator();
            nameGen.LoadFromResources();

            characterFactory = new CharacterFactory(registry, equipmentFactory, nameGen);
            roster = new CharacterRoster();

            party = new Party();
            verbPool = new VerbPool(economyConfig.verbSlotCount, economyConfig.verbDrawCooldown);
            combatProcessor = new CombatTickProcessor(advantageMatrix, economyConfig);
            exploration = new ExplorationManager(economyConfig);
            questZones = new QuestZoneTable();
            questZones.LoadFromResources();
            exploration.OnQuestDiscovered += HandleQuestDiscovered;
            pityTracker = new PityTracker();

            lootInventory = new LootInventory(50);
            lootDropper = new LootDropper(equipmentFactory, economyConfig, pityTracker);
        }

        private void Start()
        {
            var save = saveManager.Load();
            gold = save.gold;
            questLevel = save.currentQuestLevel;

            // Restore quest progression; active/retreated specs regenerate
            // deterministically from their indices + quest level.
            var savedPhase = (QuestPhase)save.questPhase;
            QuestSpec restoredSpec = null;
            if (savedPhase == QuestPhase.Active || savedPhase == QuestPhase.Retreated)
            {
                var zone = questZones.GetZone(save.questZoneIndex);
                if (zone != null)
                    restoredSpec = QuestGenerator.Generate(zone, save.questZoneIndex, save.questNextIndex, questLevel);
                else
                    savedPhase = QuestPhase.Idle;
            }
            questLog.Restore(save.questZoneIndex, save.questNextIndex, savedPhase,
                save.questActiveWave, save.questGoldEarned, restoredSpec);
            exploration.RestoreProgress(save.travelProgress, save.fragmentProgress);
            if (savedPhase == QuestPhase.Active)
                exploration.EnterQuest();

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
            // After domain reload, non-serialized fields are null
            if (verbPool == null) return;

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
                questLog.RecordGold(result.GoldEarned);
                OnGoldChanged?.Invoke(gold);
            }

            ProcessLootDrops(result.EnemiesKilled);

            TickAbilityXP();

            OnCombatTick?.Invoke(result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
                HandleWaveCleared();
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
                questLog.RecordGold(result.GoldEarned);
                OnGoldChanged?.Invoke(gold);
            }

            ProcessLootDrops(result.EnemiesKilled);

            OnVerbActivated?.Invoke(slotIndex, result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
                HandleWaveCleared();
            }
        }

        private void InitializeStarterRoster()
        {
            var rng = new System.Random();
            var characters = characterFactory.CreateStarterRoster(8, rng);
            foreach (var c in characters)
                roster.AddCharacter(c);
            roster.InitializeDefaultParty();

            // Seed inventory with starter items for new players
            var prefixes = new[] { "hd", "tr", "ar", "lg", "fe", "mc" };
            for (int i = 0; i < 5; i++)
            {
                var seedRng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
                var rarity = i < 3 ? Rarity.Uncommon : Rarity.Rare;
                var item = equipmentFactory.CreateRandom(prefixes[i % prefixes.Length], rarity, seedRng);
                if (item != null) lootInventory.AddItem(item);
            }
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

        private void HandleWaveCleared()
        {
            if (exploration.State == ExplorationState.InQuest && questLog.Phase == QuestPhase.Active)
            {
                if (questLog.IsOnLastWave)
                {
                    CompleteQuest();
                }
                else
                {
                    questLog.AdvanceWave();
                    SpawnWave();
                }
                return;
            }

            exploration.ProcessWaveCleared();
            SpawnWave();
        }

        private void HandleQuestDiscovered()
        {
            if (questLog.Phase != QuestPhase.Idle) return;

            var zone = questZones.GetZone(questLog.ZoneIndex);
            if (zone == null) return;

            var spec = QuestGenerator.Generate(zone, questLog.ZoneIndex, questLog.NextQuestIndex, questLevel);
            if (questLog.TryOffer(spec))
                OnQuestOffered?.Invoke(spec);
        }

        public bool AcceptQuest()
        {
            if (!questLog.Accept()) return false;
            exploration.EnterQuest();
            SpawnWave();
            return true;
        }

        public bool DeclineQuest() => questLog.Decline();

        public bool RetreatQuest()
        {
            if (questLog.Phase != QuestPhase.Active) return false;

            double deduct = questLog.Retreat();
            gold = Math.Max(0, gold - deduct);
            OnGoldChanged?.Invoke(gold);
            exploration.QuestRetreated();
            OnQuestRetreated?.Invoke();
            // Party resumes exploring; the Retreated phase persists as the retry handle.
            exploration.DismissRetry();
            SpawnWave();
            return true;
        }

        public bool RetryQuest()
        {
            if (!questLog.Retry()) return false;
            exploration.EnterQuest();
            SpawnWave();
            return true;
        }

        public bool DismissRetreatedQuest() => questLog.Dismiss();

        /// Preview of a quest's completion gold bonus — same formula
        /// CompleteQuest uses, exposed so the offer sheet never drifts.
        public double EstimateQuestGold(QuestSpec spec)
        {
            if (spec == null) return 0;
            return economyConfig.GoldPerKill(questLevel, 0f, economyConfig.prestigeMultiplierBase)
                * spec.TotalEnemies * spec.Reward.GoldMultiplier;
        }

        private void CompleteQuest()
        {
            var spec = questLog.ActiveSpec;
            var reward = spec.Reward;

            double goldBonus = economyConfig.GoldPerKill(questLevel, 0f, economyConfig.prestigeMultiplierBase)
                * spec.TotalEnemies * reward.GoldMultiplier;
            double goldBefore = gold;

            var lootRewards = new List<EquipmentInstance>();
            var rewardRng = new System.Random();
            int totalRolls = reward.LootRolls + reward.ExtraNoFloorRolls;
            for (int i = 0; i < totalRolls; i++)
            {
                var floor = i < reward.LootRolls ? reward.RarityFloor : null;
                var rarity = EquipmentFactory.RollRarityWithFloor(questLevel, floor, rewardRng);
                var item = rewardRng.NextDouble() < 0.7
                    ? equipmentFactory.CreateRandom(RandomArmorPrefix(rewardRng), rarity, rewardRng)
                    : equipmentFactory.CreateRandomWeapon(rarity, rewardRng);
                if (item == null) continue;
                if (lootInventory.AddItem(item))
                {
                    lootRewards.Add(item);
                    OnLootDropped?.Invoke(item);
                }
                else
                {
                    // Inventory full: guaranteed rewards convert to gold
                    // rather than vanish.
                    goldBonus += SellCalculator.GetSellValue(item, questLevel);
                }
            }
            gold = goldBefore + goldBonus;

            questLevel += spec.Tier == QuestTier.Boss ? 2 : 1;

            questLog.Complete();
            exploration.QuestCompleted();
            OnGoldChanged?.Invoke(gold);
            OnQuestCompleted?.Invoke(spec, goldBonus, lootRewards);
            SaveState();
            SpawnWave();
        }

        private static string RandomArmorPrefix(System.Random rng)
        {
            var prefixes = new[] { "hd", "tr", "ar", "lg", "fe", "mc" };
            return prefixes[rng.Next(prefixes.Length)];
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

            // Quest state (Offered saves as Idle; offers re-roll next session)
            var savePhase = questLog.Phase == QuestPhase.Offered ? QuestPhase.Idle : questLog.Phase;
            saveManager.CurrentSave.questZoneIndex = questLog.ZoneIndex;
            saveManager.CurrentSave.questNextIndex = questLog.NextQuestIndex;
            saveManager.CurrentSave.questPhase = (int)savePhase;
            saveManager.CurrentSave.questActiveWave = questLog.ActiveWaveIndex;
            saveManager.CurrentSave.questGoldEarned = questLog.GoldEarnedInQuest;
            saveManager.CurrentSave.travelProgress = exploration.TravelProgress;
            saveManager.CurrentSave.fragmentProgress = exploration.FragmentProgress;
            saveManager.Save();
        }

        private void SpawnWave()
        {
            currentEnemies.Clear();

            // Quest waves come from the generated spec (typed, HP-scaled).
            if (exploration.State == ExplorationState.InQuest
                && questLog.Phase == QuestPhase.Active
                && questLog.ActiveSpec != null
                && questLog.ActiveWaveIndex < questLog.ActiveSpec.Waves.Length)
            {
                var wave = questLog.ActiveSpec.Waves[questLog.ActiveWaveIndex];
                for (int i = 0; i < wave.EnemyCount; i++)
                {
                    var waveType = i < wave.EnemyTypes.Length
                        ? wave.EnemyTypes[i]
                        : wave.EnemyTypes[0];
                    // On boss waves the first enemy carries the full multiplier;
                    // adds fight at half of it.
                    float mult = wave.IsBossWave && i > 0 ? wave.HpMultiplier * 0.5f : wave.HpMultiplier;
                    float questHp = economyConfig.EnemyHP(questLevel) * mult;
                    string prefix = wave.IsBossWave && i == 0 ? "boss" : "quest_enemy";
                    currentEnemies.Add(new EnemyState($"{prefix}_{i}", waveType, questHp, questHp * 0.05f));
                }

                OnWaveStarted?.Invoke(currentEnemies);
                return;
            }

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

        private void TickAbilityXP()
        {
            float xp = economyConfig.abilityBaseXPRate * questLevel;
            foreach (var character in roster.Characters)
            {
                foreach (var equip in character.equipment)
                {
                    if (equip?.Ability != null)
                    {
                        equip.Ability.AddXP(xp,
                            economyConfig.abilityBaseXPThreshold,
                            economyConfig.abilityXPGrowthRate);
                    }
                }
            }
        }

        private static readonly float[] RarityGoldMultipliers = { 0.5f, 1.0f, 1.5f, 2.5f, 4.0f };

        public float GetAbilityLevelUpCost(EquipmentInstance item)
        {
            if (item?.Ability == null || item.Ability.Level >= item.Ability.MaxLevel)
                return float.MaxValue;

            float rarityMult = (int)item.Rarity < RarityGoldMultipliers.Length
                ? RarityGoldMultipliers[(int)item.Rarity] : 1.0f;
            return item.Ability.GoldCostToLevel(
                economyConfig.abilityBaseLevelUpCost,
                economyConfig.abilityCostGrowthRate,
                rarityMult);
        }

        public bool LevelUpAbility(EquipmentInstance item)
        {
            if (item?.Ability == null || item.Ability.Level >= item.Ability.MaxLevel) return false;

            float cost = GetAbilityLevelUpCost(item);

            if (gold < cost) return false;

            gold -= cost;
            item.Ability.Level++;
            item.Ability.CurrentXP = 0f;
            OnGoldChanged?.Invoke(gold);
            return true;
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
