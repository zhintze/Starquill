using System;
using System.Collections.Generic;
using UnityEngine;
using Starquill.Characters;
using Starquill.Combat;
using Starquill.Core;
using Starquill.Data;
using Starquill.Destinations;
using Starquill.Display;
using Starquill.Equipment;
using Starquill.Exploration;
using Starquill.Quests;
using Starquill.Services;

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
        private readonly BoostManager boosts = new();
        private readonly KeyPouch keyPouch = new();
        private DungeonRun activeDungeon;
        private int dungeonRunCounter;
        private IAdService adService;
        private IIapService iapService;
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
        public BoostManager Boosts => boosts;
        public KeyPouch KeyPouch => keyPouch;
        public DungeonRun ActiveDungeon => activeDungeon;
        public IAdService Ads => adService;
        public bool RemoveAdsOwned { get; private set; }
        public float PendingOfflineSeconds { get; private set; }
        public double PendingOfflineGold { get; private set; }
        private readonly List<EquipmentInstance> rewardMailbox = new();
        public IReadOnlyList<EquipmentInstance> RewardMailbox => rewardMailbox;
        public event Action OnMailboxChanged;
        public event Action OnBoostsChanged;
        public event Action<double, List<EquipmentInstance>> OnChestClaimed;

        public static double UnixNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public event Action<QuestSpec> OnQuestOffered;
        public event Action<QuestSpec, double, List<EquipmentInstance>> OnQuestCompleted;
        public event Action OnQuestRetreated;
        public event Action<EquipmentInstance> OnLootDropped;
        public event Action OnKeysChanged;
        public event Action<DungeonSpec> OnDungeonStarted;
        public event Action<DungeonRun, List<EquipmentInstance>, double> OnDungeonEnded;

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

            adService = AdServiceFactory.Create();
            iapService = new MockIapService(); // real store wiring: Sprint 12 device pass
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

            boosts.RestoreExpiry(BoostType.AutoFireVerbs, save.boostAutoFireExpiry);
            boosts.RestoreExpiry(BoostType.VerbSpeedUp, save.boostSpeedUpExpiry);
            RemoveAdsOwned = save.removeAdsOwned;
            chestReadyAt = save.chestReadyAtTimestamp > 0
                ? save.chestReadyAtTimestamp
                : UnixNow + economyConfig.chestIntervalSeconds;

            float offlineSeconds = saveManager.GetOfflineSeconds();
            double offlineGold = OfflineEarningsCalculator.Gold(offlineSeconds, questLevel, economyConfig);
            if (offlineGold > 0)
            {
                PendingOfflineSeconds = offlineSeconds;
                PendingOfflineGold = offlineGold;
            }
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

            if (save.rewardMailbox != null)
            {
                foreach (var si in save.rewardMailbox)
                {
                    if (si != null && !string.IsNullOrEmpty(si.itemType))
                        rewardMailbox.Add(equipmentFactory.Reconstruct(si));
                }
            }

            keyPouch.OnChanged += () => OnKeysChanged?.Invoke();
            if (save.keys != null)
            {
                foreach (var sk in save.keys)
                    if (sk != null) keyPouch.Add(sk.ToInstance());
            }
            dungeonRunCounter = save.dungeonRunCounter;

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

            if (activeDungeon != null)
            {
                activeDungeon.Tick(Time.deltaTime);
                if (activeDungeon.IsOver) EndDungeon();
            }

            TickBoostEffects();
        }

        private double chestReadyAt;
        private bool speedUpApplied;

        private void TickBoostEffects()
        {
            double now = UnixNow;

            bool speedUp = boosts.IsActive(BoostType.VerbSpeedUp, now);
            if (speedUp != speedUpApplied)
            {
                verbPool.SetRotationTime(speedUp ? economyConfig.verbDrawCooldown * 0.5f
                                                 : economyConfig.verbDrawCooldown);
                speedUpApplied = speedUp;
            }

            if (boosts.IsActive(BoostType.AutoFireVerbs, now) && !VerbsLocked)
            {
                var slots = verbPool.DrawnSlots;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null && currentTime - slots[i].DrawTime >= 2f)
                    {
                        OnVerbTapped(i);
                        break;
                    }
                }
            }
        }

        public double BoostCost(BoostType type)
        {
            // Priced as minutes of current income so cost tracks the gold curve.
            float minutes = type == BoostType.AutoFireVerbs
                ? economyConfig.boostAutoFireIncomeMinutes
                : economyConfig.boostSpeedUpIncomeMinutes;
            return economyConfig.GoldPerKill(questLevel)
                * economyConfig.exploreKillsPerMinute * minutes;
        }

        public bool BuyBoost(BoostType type)
        {
            double cost = BoostCost(type);
            if (gold < cost) return false;
            gold -= cost;
            boosts.Activate(type, economyConfig.boostDurationSeconds, UnixNow);
            OnGoldChanged?.Invoke(gold);
            OnBoostsChanged?.Invoke();
            SaveState();
            return true;
        }

        public double ChestReadyAt => chestReadyAt;
        public bool ChestReady => UnixNow >= chestReadyAt;

        public bool ClaimChest(bool doubled)
        {
            if (!ChestReady) return false;

            var (chestGold, itemRolls) = OfflineEarningsCalculator.ChestReward(questLevel, economyConfig, doubled);
            gold += chestGold;

            var items = new List<EquipmentInstance>();
            var rng = new System.Random();
            for (int i = 0; i < itemRolls; i++)
            {
                var rarity = EquipmentFactory.RollRarityWithFloor(questLevel, Rarity.Uncommon, rng);
                var item = rng.NextDouble() < 0.7
                    ? equipmentFactory.CreateRandom(RandomArmorPrefix(rng), rarity, rng, questLevel)
                    : equipmentFactory.CreateRandomWeapon(rarity, rng, questLevel: questLevel);
                if (item == null) continue;
                if (lootInventory.AddItem(item))
                {
                    items.Add(item);
                    OnLootDropped?.Invoke(item);
                }
                else
                {
                    rewardMailbox.Add(item);
                    OnMailboxChanged?.Invoke();
                }
            }

            chestReadyAt = UnixNow + economyConfig.chestIntervalSeconds;
            OnGoldChanged?.Invoke(gold);
            OnChestClaimed?.Invoke(chestGold, items);
            SaveState();
            return true;
        }

        public void ClaimOfflineEarnings(bool doubled)
        {
            if (PendingOfflineGold <= 0) return;
            gold += PendingOfflineGold * (doubled ? 2 : 1);
            PendingOfflineGold = 0;
            PendingOfflineSeconds = 0;
            OnGoldChanged?.Invoke(gold);
            SaveState();
        }

        public void PurchaseRemoveAds(Action<bool> onResult)
        {
            iapService.PurchaseRemoveAds(success =>
            {
                if (success)
                {
                    RemoveAdsOwned = true;
                    SaveState();
                }
                onResult?.Invoke(success);
            });
        }

        private float[] BuildPartyDamageMultipliers()
        {
            var members = party.Members;
            var mults = new float[members.Count];
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                mults[i] = m == null ? 1f
                    : Mathf.Pow(1f + economyConfig.trainingDamagePerLevel, m.trainingLevel)
                      * Mathf.Pow(1f + economyConfig.charLevelDamageBonus, m.level);
            }
            return mults;
        }

        public int HighestTrainingLevel()
        {
            int max = 0;
            foreach (var c in roster.Characters)
                if (c.trainingLevel > max) max = c.trainingLevel;
            return max;
        }

        public double TrainingCost(CharacterInstance character)
        {
            double cost = economyConfig.UpgradeCost(character.trainingLevel);
            if (character.trainingLevel < HighestTrainingLevel())
                cost *= economyConfig.trainingCatchUpDiscount;
            return cost;
        }

        public bool TrainCharacter(int rosterIndex)
        {
            if (roster == null || rosterIndex < 0 || rosterIndex >= roster.Characters.Count) return false;
            var character = roster.Characters[rosterIndex];
            double cost = TrainingCost(character);
            if (gold < cost) return false;
            gold -= cost;
            character.trainingLevel++;
            OnGoldChanged?.Invoke(gold);
            OnRosterChanged?.Invoke();
            SaveState();
            return true;
        }

        private void GrantPartyXp(int kills, int bonusPerMember = 0)
        {
            if (kills <= 0 && bonusPerMember <= 0) return;
            int perKill = economyConfig.charXpBase + questLevel;
            int fullGain = kills * perKill + bonusPerMember;

            var actives = new HashSet<CharacterInstance>();
            foreach (var member in party.Members)
            {
                if (member == null) continue;
                actives.Add(member);
                member.xp += fullGain;
            }

            // Benched roster members keep progressing at a reduced share so
            // party swaps never reset a character's growth.
            int benchGain = (int)(fullGain * economyConfig.benchXpShare);
            if (benchGain > 0 && roster != null)
            {
                foreach (var c in roster.Characters)
                    if (!actives.Contains(c)) c.xp += benchGain;
            }

            OnRosterChanged?.Invoke();
        }

        private void ProcessTick()
        {
            verbPool.FillSlots(currentTime);
            verbPool.TickCooldowns();

            var result = combatProcessor.ProcessTick(
                currentEnemies, party.GetAllStats(), null,
                questLevel, economyConfig.prestigeMultiplierBase,
                memberDamageMultipliers: BuildPartyDamageMultipliers());

            if (result.GoldEarned > 0)
            {
                gold += result.GoldEarned;
                questLog.RecordGold(result.GoldEarned);
                OnGoldChanged?.Invoke(gold);
            }

            ProcessLootDrops(result.EnemiesKilled);
            GrantPartyXp(result.EnemiesKilled);

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
                questLevel, economyConfig.prestigeMultiplierBase,
                memberDamageMultipliers: BuildPartyDamageMultipliers());

            if (result.GoldEarned > 0)
            {
                gold += result.GoldEarned;
                questLog.RecordGold(result.GoldEarned);
                OnGoldChanged?.Invoke(gold);
            }

            ProcessLootDrops(result.EnemiesKilled);
            GrantPartyXp(result.EnemiesKilled);

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
            if (exploration.State == ExplorationState.InDungeon && activeDungeon != null)
            {
                activeDungeon.WaveCleared();
                // MakeWave marks index i a mini-boss when (i+1) % 5 == 0; after
                // WaveCleared() the count equals i+1, so % 5 == 0 flags a
                // just-cleared boss wave (5, 10, ...).
                bool miniBossCleared = activeDungeon.WavesCleared % 5 == 0;
                if (miniBossCleared) GrantMiniBossDrop();
                if (!activeDungeon.IsOver) SpawnWave();
                return;
            }

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
                    ? equipmentFactory.CreateRandom(RandomArmorPrefix(rewardRng), rarity, rewardRng, questLevel)
                    : equipmentFactory.CreateRandomWeapon(rarity, rewardRng, questLevel: questLevel);
                if (item == null) continue;
                if (lootInventory.AddItem(item))
                {
                    lootRewards.Add(item);
                    OnLootDropped?.Invoke(item);
                }
                else
                {
                    // Inventory full: guaranteed rewards wait in the mailbox
                    // until the player makes room (never lost, never
                    // silently converted).
                    rewardMailbox.Add(item);
                    OnMailboxChanged?.Invoke();
                }
            }

            for (int i = 0; i < reward.KeyDrops; i++)
            {
                var key = KeyRoller.Roll(rewardRng);
                key.Difficulty = Math.Max(key.Difficulty, reward.KeyDifficulty);
                keyPouch.Add(key);
            }
            if (reward.ExtraKeyChance > 0 && rewardRng.NextDouble() < reward.ExtraKeyChance)
                keyPouch.Add(KeyRoller.Roll(rewardRng));

            gold = goldBefore + goldBonus;

            int xpBonus = 10 * questLevel * (spec.Tier == QuestTier.Boss ? 2 : 1);
            GrantPartyXp(0, xpBonus);

            questLevel += spec.Tier == QuestTier.Boss ? 2 : 1;

            questLog.Complete();
            exploration.QuestCompleted();
            OnGoldChanged?.Invoke(gold);
            OnQuestCompleted?.Invoke(spec, goldBonus, lootRewards);

            if (!RemoveAdsOwned)
                adService.ShowInterstitial(AdPlacement.Interstitial);

            SaveState();
            SpawnWave();
        }

        /// Moves mailbox rewards into the inventory while space allows.
        /// Returns how many were collected; leftovers stay in the mailbox.
        public int CollectMailbox()
        {
            int collected = 0;
            for (int i = 0; i < rewardMailbox.Count;)
            {
                if (lootInventory.AddItem(rewardMailbox[i]))
                {
                    OnLootDropped?.Invoke(rewardMailbox[i]);
                    rewardMailbox.RemoveAt(i);
                    collected++;
                }
                else
                {
                    break; // inventory full again
                }
            }
            if (collected > 0)
            {
                OnMailboxChanged?.Invoke();
                SaveState();
            }
            return collected;
        }

        private static string RandomArmorPrefix(System.Random rng)
        {
            var prefixes = new[] { "hd", "tr", "ar", "lg", "fe", "mc" };
            return prefixes[rng.Next(prefixes.Length)];
        }

        public bool StartDungeon(KeyInstance key)
        {
            if (activeDungeon != null) return false;
            if (exploration.State != ExplorationState.Exploring) return false;
            if (questLog.Phase == QuestPhase.Offered) return false;
            if (!keyPouch.Remove(key)) return false;

            var spec = DungeonGenerator.Generate(key, dungeonRunCounter++, questLevel,
                economyConfig.dungeonDurationBase, economyConfig.dungeonDurationPerD,
                economyConfig.dungeonEnemyMultPerD, economyConfig.dungeonParSecondsPerWave);
            activeDungeon = new DungeonRun(spec);
            exploration.EnterDungeon();
            SpawnWave();
            OnDungeonStarted?.Invoke(spec);
            SaveState();
            return true;
        }

        private void EndDungeon()
        {
            var run = activeDungeon;
            activeDungeon = null;

            var rng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var rolled = DungeonRewardRoller.Roll(run, equipmentFactory, economyConfig,
                questLevel, rng, out double goldBonus);
            var rewards = new List<EquipmentInstance>();
            foreach (var item in rolled)
            {
                if (lootInventory.AddItem(item))
                {
                    rewards.Add(item);
                    OnLootDropped?.Invoke(item);
                }
                else
                {
                    // Inventory full: rewards wait in the mailbox (never lost).
                    rewardMailbox.Add(item);
                    OnMailboxChanged?.Invoke();
                }
            }
            gold += goldBonus;

            exploration.DungeonEnded();
            OnGoldChanged?.Invoke(gold);
            OnDungeonEnded?.Invoke(run, rewards, goldBonus);
            SaveState();
            SpawnWave();
        }

        /// A just-cleared mini-boss pays out one floored, key-modified roll
        /// immediately (design §3).
        private void GrantMiniBossDrop()
        {
            var rng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var item = DungeonRewardRoller.RollOne(activeDungeon.Spec, equipmentFactory, questLevel, rng);
            if (item == null) return;
            if (lootInventory.AddItem(item))
            {
                OnLootDropped?.Invoke(item);
            }
            else
            {
                rewardMailbox.Add(item);
                OnMailboxChanged?.Invoke();
            }
        }

        /// Fusion cost preview: valid for any pair since difficulty always
        /// sums (KeyFusion.Fuse contract).
        public double KeyFusionCost(KeyInstance a, KeyInstance b)
        {
            if (a == null || b == null) return 0;
            return KeyFusion.Cost(economyConfig.fusionBaseCost, questLevel,
                a.Difficulty + b.Difficulty);
        }

        public bool FuseKeys(KeyInstance a, KeyInstance b)
        {
            // Validate rules and affordability BEFORE consuming anything:
            // no failure path may lose a key or gold.
            if (!KeyFusion.CanFuse(a, b, economyConfig.keyMaxDifficulty)) return false;
            double cost = KeyFusionCost(a, b);
            if (gold < cost) return false;

            var fused = KeyFusion.Fuse(a, b);
            if (fused == null) return false;

            if (!keyPouch.Remove(a)) return false;
            if (!keyPouch.Remove(b))
            {
                keyPouch.Add(a); // restore: the pair must be consumed atomically
                return false;
            }

            keyPouch.Add(fused);
            gold -= cost;
            OnGoldChanged?.Invoke(gold);
            SaveState();
            return true;
        }

        public void SellKey(KeyInstance key)
        {
            if (key == null || !keyPouch.Remove(key)) return;
            gold += KeyPouch.SellValue(key, questLevel, economyConfig.keySellBase);
            OnGoldChanged?.Invoke(gold);
            SaveState();
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
            saveManager.CurrentSave.boostAutoFireExpiry = boosts.GetExpiry(BoostType.AutoFireVerbs);
            saveManager.CurrentSave.boostSpeedUpExpiry = boosts.GetExpiry(BoostType.VerbSpeedUp);
            saveManager.CurrentSave.chestReadyAtTimestamp = chestReadyAt;
            saveManager.CurrentSave.removeAdsOwned = RemoveAdsOwned;
            saveManager.CurrentSave.rewardMailbox.Clear();
            foreach (var item in rewardMailbox)
                saveManager.CurrentSave.rewardMailbox.Add(SerializedEquipment.FromInstance(item));
            saveManager.CurrentSave.keys.Clear();
            foreach (var key in keyPouch.Keys)
                saveManager.CurrentSave.keys.Add(SerializedKey.FromInstance(key));
            saveManager.CurrentSave.dungeonRunCounter = dungeonRunCounter;
            saveManager.Save();
        }

        private void SpawnWave()
        {
            currentEnemies.Clear();

            // Dungeon waves are deterministic from (spec, cleared count); the
            // per-wave HP ramp is applied here, not in the generator.
            if (exploration.State == ExplorationState.InDungeon && activeDungeon != null)
            {
                var spec = activeDungeon.Spec;
                var dungeonWave = DungeonGenerator.MakeWave(spec, activeDungeon.WavesCleared);
                float ramp = 1f + economyConfig.dungeonWaveHpRamp * activeDungeon.WavesCleared;
                for (int i = 0; i < dungeonWave.EnemyCount; i++)
                {
                    var waveType = i < dungeonWave.EnemyTypes.Length
                        ? dungeonWave.EnemyTypes[i]
                        : dungeonWave.EnemyTypes[0];
                    // Mirrors the quest boss convention (adds at half multiplier);
                    // dungeon mini-bosses spawn alone, so this rarely triggers.
                    float mult = dungeonWave.IsBossWave && i > 0
                        ? dungeonWave.HpMultiplier * 0.5f
                        : dungeonWave.HpMultiplier;
                    float dungeonHp = economyConfig.EnemyHP(questLevel) * mult
                        * spec.EnemyHpMultiplier * ramp;
                    string dungeonPrefix = dungeonWave.IsBossWave && i == 0
                        ? "dungeon_boss" : "dungeon_enemy";
                    currentEnemies.Add(new EnemyState($"{dungeonPrefix}_{i}", waveType,
                        dungeonHp, dungeonHp * 0.05f));
                }

                OnWaveStarted?.Invoke(currentEnemies);
                return;
            }

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
            var mods = activeDungeon != null
                ? DungeonRewardRoller.ModifiersFor(activeDungeon.Spec.Key) : null;
            for (int k = 0; k < killCount; k++)
            {
                var rng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
                var drop = lootDropper.TryDrop(questLevel, rng, mods);
                if (drop != null && lootInventory.AddItem(drop))
                    OnLootDropped?.Invoke(drop);

                // Keys drop from active kills only, never inside dungeons (design §2).
                if (exploration.State != ExplorationState.InDungeon)
                {
                    float rate = economyConfig.keyDropRate
                        * (keyPouch.IsOverSoftCap(economyConfig.keySoftCap) ? 0.5f : 1f);
                    if (rng.NextDouble() < rate)
                        keyPouch.Add(KeyRoller.Roll(rng));
                }
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
            if (!paused) return;
            // Bank an active run before saving: cleared waves pay out, the run
            // is never resumed with a stale clock (design §3).
            if (activeDungeon != null) EndDungeon();
            SaveState();
        }

        private void OnApplicationQuit()
        {
            if (activeDungeon != null) EndDungeon();
            SaveState();
        }
    }
}
