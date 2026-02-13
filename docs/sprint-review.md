# Starquill Idle RPG Clicker — Sprint Review

---

## Sprint 1: Backend Architecture & Core Systems

**Date:** 2026-02-12 — 2026-02-13
**Branch:** `unity-idle-clicker`
**Base:** `dev`
**Status:** Complete — pushed to origin, pending PR

### Goal

Stand up the full Unity 6 backend for an idle RPG clicker: data layer, combat engine, economy math, save system, and game loop. No UI or visuals — pure logic with test coverage on all math-critical paths.

### Deliverables

#### Project Setup
- Archived all Godot files to `godot-archive/`
- Initialized Unity 6 (6000.3.8f1) project with IL2CPP, portrait orientation, 1080x1920
- Established 8 assembly definitions enforcing dependency boundaries:
  `Core → Data → Combat/Economy/Characters/Equipment/Exploration → Managers`
- Configured `.gitignore` for Unity

#### Core Enums (7 files)
| File | Purpose |
|------|---------|
| `StatType.cs` | STR, DEX, CON, INT, WIS, CHA with Physical/Mental category extension |
| `Rarity.cs` | Common, Uncommon, Rare, Epic, Legendary |
| `TargetMode.cs` | Single, Cleave, AoE |
| `VerbCategory.cs` | Physical, Mental |
| `StatusEffectType.cs` | None, Stagger, Bleed, Weaken, Expose, Reveal, Confuse |
| `EquipmentSlot.cs` | Head, Torso, Arms, Legs, Feet, MainHand, OffHand, Misc1–4 |
| `Advantage.cs` | Weak, Neutral, Strong |

#### Data ScriptableObjects (11 files)
| File | Purpose |
|------|---------|
| `Stats.cs` | Serializable stat block with GetStat(), SetStat(), Total, HighestStat(), Clone(), operator+ |
| `SpeciesDefinition.cs` | Base stats, species ability, body part IDs, item restrictions |
| `SpeciesAbilityDefinition.cs` | 6 rank thresholds with scaling effects |
| `EquipmentDefinition.cs` | Item ID, slot, stat mods, affixes, layer codes, set membership |
| `AffixDefinition.cs` | Stat modifier ranges, percentage flag, valid slots |
| `EquipmentSetDefinition.cs` | 2pc/3pc/4pc set bonuses |
| `VerbDefinition.cs` | Stat type, base damage, hit count, target mode, proc chance, cooldown, rarity |
| `StatusEffectDefinition.cs` | Stacking rules, overrides, cleanse relationships |
| `EconomyConfig.cs` | 20 tuning knobs + EnemyHP(), GoldPerKill(), UpgradeCost(), OfflineGold() formulas |
| `AdvantageMatrix.cs` | Static 6x6 damage/proc matrices, GetMatchup() returning MatchupResult |
| `QuestZoneDefinition.cs` | Zone ID, dominant enemy types, quest count, dialogue arrays |

#### Combat System (6 files)
| File | Purpose |
|------|---------|
| `DamageCalculator.cs` | Static damage formula: base * statMult * advantage * expose * equip * prestige * boost |
| `DrawnVerb.cs` | Wraps VerbDefinition with owner index, draw time, cooldown tracking |
| `VerbPool.cs` | Card-draw verb system: fill slots, rotate stale verbs (10s), activate with cooldown |
| `EnemyState.cs` | Enemy HP, status effects (apply/tick/query), damage output with Weaken/Stagger |
| `CombatTickResult.cs` | Per-tick output: damage dealt/received, kills, gold, status procs, advantage hits |
| `CombatTickProcessor.cs` | Full tick pipeline: auto-attack → verb resolution → status ticks → enemy attacks → kill check |

#### Equipment (1 file)
| File | Purpose |
|------|---------|
| `PityTracker.cs` | Guaranteed drops at 50/200/1000/5000 kills for Uncommon/Rare/Epic/Legendary |

#### Characters (2 files)
| File | Purpose |
|------|---------|
| `CharacterInstance.cs` | Species, base stats, equipment array, verb slots (level-scaled 2/3/4/5), species ability rank |
| `Party.cs` | Max 4 members, stat aggregation, verb pooling, idle bonuses (CHA→gold, INT→XP, WIS→discovery at 2%/pt) |

#### Exploration (2 files)
| File | Purpose |
|------|---------|
| `ExplorationState.cs` | Enum: Exploring, InQuest, QuestRetreat |
| `ExplorationManager.cs` | State machine with quest discovery (3%), fragment drops (1%), state transitions, events |

#### Managers (3 files)
| File | Purpose |
|------|---------|
| `SaveData.cs` | Serializable save structure: gold, characters, pity tracker, timestamps, fragments, codex |
| `SaveManager.cs` | PlayerPrefs + JsonUtility save/load, offline seconds calculation, delete save |
| `GameManager.cs` | Central MonoBehaviour: 1s tick loop, verb activation, wave spawning, verb pool rebuild, 30s auto-save |

#### Test Coverage (7 files, ~49 tests)
| Test File | Tests | What's Covered |
|-----------|-------|----------------|
| `StatTypeTests.cs` | 6 | Category mapping for all 6 stats |
| `StatsTests.cs` | 4 | GetStat, Total, HighestStat, operator+ |
| `AdvantageMatrixTests.cs` | 12 | All triangle matchups, cross-triangle rules, neutral |
| `DamageCalculatorTests.cs` | 10 | Base damage, low stat, advantage/disadvantage, expose, combos, proc modifiers |
| `VerbPoolTests.cs` | 6 | Fill, exceed, activate, rotate at 10s, cooldown blocking, tick frees |
| `EconomyConfigTests.cs` | 8 | HP scaling, gold per kill, CHA bonus, cost growth, offline cap/efficiency |
| `PityTrackerTests.cs` | 3 | No guarantee before threshold, guaranteed at 50, register drop resets |

### Bugs Found & Fixed

1. **Double Expose multiplier** — Both `EnemyState.TakeDamage()` and `DamageCalculator.Calculate()` applied 1.25x, resulting in 1.5625x. Fixed: removed from `TakeDamage`, added to auto-attack path in `CombatTickProcessor`.

2. **Dead enemies re-counted every tick** — Kill check iterated all dead enemies (including previously dead), awarding duplicate gold. Fixed: kill check now uses the `aliveEnemies` snapshot captured before damage.

### Commits

```
ab10e57f Add idle RPG clicker architecture and design documents
9a5328d8 Add Sprint 1 implementation plan with 19 tasks and test coverage
630eef2f Archive Godot files and prepare for Unity project
e10a38d4 Initialize Unity 6 project with folder structure and test framework
ccbced30 Add core enums: StatType, Rarity, TargetMode, StatusEffectType, EquipmentSlot
66d36916 Add Stats class with tests and SpeciesDefinition ScriptableObjects
63c9c7e1 Add Equipment, Affix, and EquipmentSet ScriptableObject definitions
e539068d Add VerbDefinition and StatusEffectDefinition ScriptableObjects
9563387e Add EconomyConfig and AdvantageMatrix with full test coverage
6f049180 Add DamageCalculator with full damage formula and stat modifier tests
d8906173 Add EconomyConfig formula tests validating HP/gold/cost scaling curves
a797386b Add VerbPool with draw/rotate/cooldown mechanics and tests
9c3b841e Add CombatTickProcessor, EnemyState, and CombatTickResult
6dc52b78 Add PityTracker with guaranteed drop thresholds and tests
0173b442 Add CharacterInstance and Party classes with stat aggregation
b250e454 Add ExplorationManager state machine and QuestZoneDefinition
2b36f486 Add SaveManager with JSON serialization and offline time tracking
e4829e66 Add GameManager with tick loop, verb activation, and auto-save
43b692cb Fix double Expose multiplier and per-tick kill re-counting bugs
90e5cf76 Add Unity-generated meta files and project settings
```

### Stats

- **Source files:** 33 (.cs) across 8 assemblies
- **Test files:** 7 (.cs), ~49 test cases
- **Lines added:** ~1,926 (source + tests + meta)
- **Commits:** 20

### Known Issues

- Unity headless batch mode test runner hangs on Arch Linux due to `ScriptableRuntimeReflectionSystem` GC handle bug. Tests must be run in Unity Editor GUI.
- `GameManager.SpawnWave` uses `UnityEngine.Random` while other classes use `System.Random` with optional seeds. Minor determinism inconsistency for testing.
- `Economy` assembly exists but is empty — `EconomyConfig` lives in `Data` assembly.

### Open Questions for Sprint 2

- Port paper doll display system from Godot or rebuild from scratch in Unity?
- UI framework choice: Unity UI Toolkit vs. traditional Canvas + TextMeshPro?
- Should verb slot UI use drag-and-drop or tap-to-activate?

---

## Sprint 2: [Planned]

*To be filled after sprint completion.*
