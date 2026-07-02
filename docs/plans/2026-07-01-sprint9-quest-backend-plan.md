# Sprint 9: Quest System Backend — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Quest zones, deterministic quest generation, an accept/retreat/complete state machine, and reward execution wired into GameManager — pure logic with full EditMode coverage, no UI.

**Architecture:** New `Starquill.Quests` assembly (refs Core + Data only) holds data model, generator, and `QuestLog` state machine; GameManager orchestrates discovery offers, quest-wave spawning, rewards (loot rolls via EquipmentFactory), and save fields. `ExplorationManager`'s existing state scaffolding is reused.

**Tech Stack:** C# / Unity 6, NUnit EditMode, JSON content via SimpleJson (Core).

**Design doc:** `docs/plans/2026-07-01-sprint9-quest-backend-design.md`

**Verification protocol:** Coplay `check_compile_errors` after every task; user runs Test Runner at the checkpoints (do NOT script test runs — editor crash risk). Glyph rule and UiTheme rules do not apply (no UI this sprint).

---

### Task 1: Quests assembly + data model + quest_zones.json

**Files:**
- Create: `Assets/Scripts/Quests/Starquill.Quests.asmdef` (refs `Starquill.Core`, `Starquill.Data`)
- Create: `Assets/Scripts/Quests/QuestTier.cs` (enum Normal/Elite/Hard/Boss)
- Create: `Assets/Scripts/Quests/WaveSpec.cs` (`int EnemyCount; StatType[] EnemyTypes; float HpMultiplier; bool IsBossWave`)
- Create: `Assets/Scripts/Quests/QuestSpec.cs` (`int ZoneIndex, QuestIndex; QuestTier Tier; string DisplayName; WaveSpec[] Waves; QuestRewardSpec Reward`)
- Create: `Assets/Scripts/Quests/QuestRewardSpec.cs` (`float GoldMultiplier; int LootRolls; Rarity? RarityFloor; int ExtraNoFloorRolls`) with static `ForTier(QuestTier)`: Normal (1.5f, 1, null, 0) · Elite (2f, 1, Uncommon, 0) · Hard (2.5f, 1, Rare, 1) · Boss (4f, 2, Rare, 0)
- Create: `Assets/Scripts/Quests/QuestZone.cs` (`string Id, Name; StatType[] DominantTypes; string[] IntroDialogue, CompletionDialogue; const int QuestsPerZone = 11`)
- Create: `Assets/Scripts/Quests/QuestZoneTable.cs` (LoadFromResources → `Data/quest_zones`, LoadFromJson via SimpleJson mirroring AbilityTable's pattern; `IReadOnlyList<QuestZone> Zones`; `GetZone(int index)` clamps to last zone)
- Create: `Assets/Resources/Data/quest_zones.json` — 2 zones: "verdant-hollow" (dominants STR, CON — beasts theme) and "gloomspire-ruins" (dominants INT, WIS — arcane theme), each with 3-4 intro/completion dialogue strings
- Create: `Assets/Tests/EditMode/Quests/QuestZoneTableTests.cs`
- Modify: `Assets/Tests/EditMode/EditModeTests.asmdef` (add `Starquill.Quests` reference)
- Delete: `Assets/Scripts/Data/QuestZoneDefinition.cs` (+ .meta) — unused ScriptableObject superseded by JSON

**Tests:** parses both zones; dominant types parse to valid StatType; dialogue arrays non-empty; `GetZone(99)` clamps to last; `GetZone(-1)` clamps to first. Compile check → commit `feat: quest zones data model + quest_zones.json`.

### Task 2: QuestGenerator (TDD)

**Files:** Create `Assets/Scripts/Quests/QuestGenerator.cs`; Test `Assets/Tests/EditMode/Quests/QuestGeneratorTests.cs`

`public static QuestSpec Generate(QuestZone zone, int zoneIndex, int questIndex, int questLevel)` — seeded `System.Random(zoneIndex * 397 ^ questIndex * 31 ^ questLevel)`:
- Tier ladder: index 0-2 Normal, 3 Elite, 4-6 Normal, 7 Elite, 8-9 Hard, 10 Boss (static `TierFor(int questIndex)`)
- Wave counts: Normal 3-5 (index 4-6 → 4-6), Elite 3-4 + final mini-boss wave, Hard 5-7, Boss 2 lead-in + 1 boss wave
- Per wave: enemyCount 2-4 (+1 Hard), hpMultiplier ramps linearly 1.0 → 1.3 across waves; mini-boss wave = 1 enemy at 4x, boss wave = 1 at 8x + 2 typed adds
- Enemy types: 80% zone dominant / 20% any; Hard waves force ≥2 distinct types; boss wave adds use dominants
- DisplayName: `"{zone.Name} {questIndex+1}/11"` (+" — Boss" for boss); Reward = `QuestRewardSpec.ForTier(tier)`

**Tests (write first):** determinism (two calls, identical spec deep-compare); TierFor ladder exact; wave count ranges per tier over 50 seeds; HP ramp monotonic non-decreasing; boss quest final wave IsBossWave with 8x multiplier and 3 enemies; Hard waves have ≥2 distinct types; dominant weighting ≥60% over 200 sampled enemies. Compile → commit `feat: deterministic QuestGenerator`.

### Task 3: QuestLog state machine (TDD)

**Files:** Create `Assets/Scripts/Quests/QuestLog.cs`; Test `Assets/Tests/EditMode/Quests/QuestLogTests.cs`

```csharp
public enum QuestPhase { Idle, Offered, Active, Retreated }
public class QuestLog
{
    public int ZoneIndex { get; private set; }
    public int NextQuestIndex { get; private set; }   // 0..10
    public QuestPhase Phase { get; private set; }
    public int ActiveWaveIndex { get; private set; }
    public double GoldEarnedInQuest { get; private set; }
    public QuestSpec ActiveSpec { get; private set; } // set on Offer, kept through Active/Retreated

    public bool TryOffer(QuestSpec spec)   // Idle → Offered (false otherwise)
    public bool Accept()                   // Offered → Active(wave 0)
    public bool Decline()                  // Offered → Idle (spec cleared; re-offerable)
    public bool AdvanceWave()              // Active: wave++, false when past last (caller completes)
    public bool IsOnLastWave { get; }
    public void RecordGold(double amount)  // Active only
    public double Retreat()                // Active → Retreated; returns gold to DEDUCT (half of earned)
    public bool Retry()                    // Retreated → Active(wave 0), gold counter reset
    public bool Dismiss()                  // Retreated → Idle (quest stays next; re-offerable)
    public bool Complete()                 // Active on last wave → Idle; NextQuestIndex++; boss: ZoneIndex++, NextQuestIndex=0
    public void Restore(int zone, int next, QuestPhase phase, int wave, double gold, QuestSpec spec) // save load
}
```

**Tests (write first):** every legal transition; every illegal transition returns false and mutates nothing (e.g. Accept from Idle, Retreat from Offered, Complete mid-quest); retreat returns half of recorded gold and Retry resets counter and wave; boss Complete rolls zone forward and resets index; non-boss Complete increments quest index; Decline leaves quest re-offerable. Compile → commit `feat: QuestLog state machine`.

**CHECKPOINT A: user runs Test Runner (expect prior suite + ~30 new, all green).**

### Task 4: Rarity floor helper (TDD)

**Files:** Modify `Assets/Scripts/Equipment/EquipmentFactory.cs`; Test append `Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs`

`public static Rarity RollRarityWithFloor(int questLevel, Rarity? floor, System.Random rng)` — `RollRarity` then clamp up to floor when set. Tests: floor null passes through; result never below floor over 100 seeds; floor Legendary always Legendary. Compile → commit `feat: rarity floor rolls for quest rewards`.

### Task 5: Save fields

**Files:** Modify `Assets/Scripts/Managers/SaveData.cs` (add `int questZoneIndex; int questNextIndex; int questPhase; int questActiveWave; double questGoldEarned;`) — Offered phase saves as Idle (offers don't persist, per design).

Compile → commit `feat: quest state save fields`.

### Task 6: GameManager integration

**Files:** Modify `Assets/Scripts/Managers/GameManager.cs`, `Assets/Scripts/Managers/Starquill.Managers.asmdef` (+ `Starquill.Quests`)

- Fields: `QuestZoneTable questZones` (load in Awake), `QuestLog questLog = new()`; public `QuestLog QuestLog`.
- Events: `OnQuestOffered(QuestSpec)`, `OnQuestCompleted(QuestSpec, double goldBonus, List<EquipmentInstance> lootRewards)`, `OnQuestRetreated`.
- Wire `exploration.OnQuestDiscovered` (Awake, after construction): generate next spec (`QuestGenerator.Generate(questZones.GetZone(questLog.ZoneIndex), questLog.ZoneIndex, questLog.NextQuestIndex, questLevel)`), `questLog.TryOffer` → raise `OnQuestOffered`.
- Public API: `AcceptQuest()` (log.Accept + exploration.EnterQuest + SpawnWave), `DeclineQuest()`, `RetreatQuest()` (deduct returned gold, exploration.QuestRetreated, event), `RetryQuest()`, `DismissRetreat()`.
- `SpawnWave()`: when `exploration.State == InQuest`, build enemies from `questLog.ActiveSpec.Waves[questLog.ActiveWaveIndex]` — per enemy: type from WaveSpec, `hp = EnemyHP(questLevel) * wave.HpMultiplier` (boss wave: first enemy 8x already in spec multiplier — spec carries per-wave multiplier; boss enemy sized via `IsBossWave` first-enemy convention from generator).
- Wave-clear path (where `exploration.ProcessWaveCleared()` is called): in quest mode, `questLog.IsOnLastWave ? CompleteQuest() : (questLog.AdvanceWave(), SpawnWave())`.
- Gold during quest: in the tick/verb kill-gold paths, when InQuest also `questLog.RecordGold(earned)`.
- `CompleteQuest()`: gold bonus `GoldPerKill(questLevel) * totalEnemiesInSpec * reward.GoldMultiplier` added; loot: `reward.LootRolls` rolls at `RollRarityWithFloor(questLevel, reward.RarityFloor, rng)` + `ExtraNoFloorRolls` at no floor, created via existing random armor/weapon split, added to inventory (respect capacity — overflow drops are discarded with a TODO for Sprint 10 UX); `questLevel += tier == Boss ? 2 : 1`; `questLog.Complete()`; `exploration.QuestCompleted()`; event; SpawnWave.
- Save/load: persist and `Restore` (Offered → saved as Idle; Active resumes: regenerate spec deterministically from saved indices + `currentQuestLevel`, restore wave).

Compile → commit `feat: quest flow wired into GameManager`.

### Task 7: Verification (CHECKPOINT B)

1. Compile clean; user runs full Test Runner (expect ~330+ green).
2. Play-mode smoke via script: force an offer (call the discovery handler via reflection or set `questDiscoveryRate = 1` temporarily on the config instance in play mode), Accept, clear waves, verify completion event + rewards + questLevel bump; retreat path; save/load resume.
3. Update `docs/sprint-review.md` + `docs/roadmap.md` (Sprint 9 complete) + `docs/implemented-systems.md` §7/§11; commit `docs: Sprint 9 quest backend complete`.
