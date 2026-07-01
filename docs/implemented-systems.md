# Starquill: Implemented Systems Reference

**Last updated:** 2026-07-01
**Status:** Authoritative. This document describes what is actually built on branch `unity-idle-clicker` as of the equipment stat redesign (verified: all 262 EditMode tests pass). Where it conflicts with `docs/plans/2026-02-12-idle-rpg-clicker-design.md`, this document wins.

---

## 1. Overview

Unity 6 (6000.3.8f1) mobile idle RPG clicker, portrait 1080x1920, IL2CPP. A party of 4 paper-doll characters auto-battles enemy waves. The player taps Verbs (pooled party abilities) to exploit a dual-triangle advantage system. Loot drops feed a collection/equip loop that visibly changes characters.

**Assemblies:** `Core → Data → Combat / Economy / Characters / Equipment / Exploration / Display → Managers`, with `UI` on top. Tests in `EditModeTests` (34 files, ~262 tests).

---

## 2. Core & Data (`Assets/Scripts/Core/`, `Assets/Scripts/Data/`)

### Enums (Core)
| Enum | Values |
|---|---|
| `StatType` | STR, DEX, CON, INT, WIS, CHA (+ Physical/Mental category extension) |
| `Rarity` | Common, Uncommon, Rare, Epic, Legendary |
| `EquipmentSlot` | Head, Torso, Arms, Legs, Feet, MainHand, OffHand, Misc1-4 (11 slots) |
| `TargetMode` | Single, Cleave, AoE |
| `VerbCategory` | Physical, Mental |
| `StatusEffectType` | None, Stagger, Bleed, Weaken, Expose, Reveal, Confuse |
| `Advantage` | Weak, Neutral, Strong |

JSON parsing: `MiniJSON` + `SimpleJson` helpers (Core assembly).

### Data ScriptableObjects
- `Stats`: serializable stat block (GetStat/SetStat/Total/HighestStat/Clone/operator+)
- `SpeciesDefinition`, `SpeciesAbilityDefinition`: base stats, species ability with 6 rank thresholds, body part IDs
- `VerbDefinition`: stat type, base damage, hit count, target mode, proc chance, cooldown, rarity
- `StatusEffectDefinition`: stacking rules, overrides, cleanse relationships
- `AdvantageMatrix`: static 6x6 damage/proc matrices (`Assets/Data/Config/DefaultAdvantageMatrix.asset`)
- `QuestZoneDefinition`: zone ID, dominant enemy types, quest count, dialogue arrays (data model only; no quest system consumes it yet)
- `EconomyConfig` (`Assets/Data/Config/DefaultEconomyConfig.asset`): all tuning knobs and formulas

### Economy formulas & knobs (EconomyConfig)
| Knob | Value |
|---|---|
| baseHP / hpGrowthRate | 50 / 0.12 (exponential enemy HP) |
| baseGold / goldGrowthRate | 5 / 0.10 |
| baseCost / costGrowthRate | 10 / 0.15 (upgrade costs) |
| offlineEfficiency / maxOfflineSeconds | 0.50 / 28800 (8h cap) |
| verbDrawCooldown / verbSlotCount / verbLockDuration | 10s / 3 / 3s |
| autoAttackDPSFraction | 0.3 |
| pity: Uncommon/Rare/Epic/Legendary | 50 / 200 / 1000 / 5000 kills |
| baseDropRate | 0.15 per kill |
| questDiscoveryRate / fragmentDropRate | 0.03 / 0.01 (no consumer yet) |
| ability XP: threshold/growth/rate | 100 / 1.8 / 1 per tick |
| ability gold cost: base/growth | 100 / 2.0 |
| prestigeMultiplierBase | 1.0 (stubbed; prestige is post-MVP) |

Formulas: `EnemyHP(questLevel)`, `GoldPerKill(questLevel, chaBonus, prestigeMult, boostMult)`, `UpgradeCost(currentLevel)`, `OfflineGold(goldPerSecond, elapsedSeconds, prestigeMult)`.

---

## 3. Combat (`Assets/Scripts/Combat/`)

- `CombatTickProcessor`: deterministic tick resolution; party auto-attacks (fraction of DPS), verb hits, advantage multipliers, enemy damage, wave death checks. Returns `CombatTickResult`.
- `VerbPool`: shared party verb pool; 3 draw slots, 10s rotation, tap-to-fire, verb lock (3s) after activation.
- `DamageCalculator`: dual-triangle advantage math via `AdvantageMatrix.GetMatchup()`.
- `EnemyState`: per-enemy runtime HP/stat typing.

## 4. Characters (`Assets/Scripts/Characters/`)

- `CharacterInstance`: species + stats + 11 equipment slots.
- `CharacterFactory` + `NameGenerator`: random character generation from `species.json` + `names.json`.
- `CharacterRoster` / `Party`: roster of owned characters; active party of 4.
- `AutoEquipper`: best-in-slot per slot from inventory (uses `ItemComparer`), returns displaced items.

## 5. Equipment (`Assets/Scripts/Equipment/`)

### Stat model (redesigned 2026-02-17; replaces StatMods + RolledAffixes)

Every `EquipmentInstance` carries:
1. **Primary/secondary stat pair.** `EquipmentFactory.GenerateStatPair(prefix, rarity, rng)`:
   - Prefix-weighted stat pools (80% from pool, 20% any stat), secondary always differs from primary
   - Rarity point budget: Common 4-6, Uncommon 7-10, Rare 11-14, Epic 15-18, Legendary 19-22
   - Split ~65-75% primary / remainder secondary; primary always > secondary
2. **One AwakenedAbility** (optional): passive stat boost defined in `abilities.json` (14 abilities), rolled by `AbilityTable.RollAbility()` weighted per equipment type prefix.
   - Potency: `basePotency + (level-1) * potencyPerLevel`, both scaled by rarity modifier (0.8x Common → 2.0x Legendary)
   - Max level by rarity: Common 2, Uncommon 3, Rare 4, Epic 5, Legendary 7
   - Levels via per-tick XP (exponential threshold: 100 * 1.8^(level-1)) or gold purchase (`GameManager.LevelUpAbility`, cost 100 * 2.0^(level-1) * rarity multiplier)
   - All abilities are passive stat boosts for MVP; triggered abilities are data-modeled (`triggerType`/`procChance`) but not combat-resolved
3. `GetTotalStatMods()` = stat pair + ability potency on the ability's boosted stat.

### Pipeline
- `EquipmentCatalog`: loads `equipment.json` + `weapons.json` (armor prefixes hd/tr/ar/lg/fe/mc; weapons w01-w07, shields w08-w09; modular weapons with per-layer variants)
- `EquipmentFactory`: `CreateRandom`, `CreateRandomWeapon`, `CreateRandomLoadout(questLevel)`, `CreateStarterLoadout`, `RollRarity(questLevel)`, `Reconstruct(SerializedEquipment)`
- `LootDropper`: per-kill drop roll (baseDropRate) + rarity roll + 70% armor / 30% weapon + `PityTracker` integration
- `LootInventory`: capacity 50, add/remove/filter, events
- `SellCalculator`: rarity-based gold value scaled by questLevel
- `ItemComparer`: `ScoreItem`, `Compare`, `FindBestForSlot`
- `SerializedEquipment` / `SerializedAbility`: save format (stat pair fields + ability id/level/XP; flat float[]/int[] color serialization for JsonUtility)

## 6. Display (`Assets/Scripts/Display/`)

Paper doll rendering: `DisplayBuilder` 4-stage pipeline (species parts → equipment layers → color application → composite order) → `CharacterDisplay` RenderTexture compositing at 400x400 bilinear. `ColorManager` loads `color_palettes.json`. `ImageResolver`/`ImageToken` map layer codes to sprites. `CharacterPortraitRenderer` for UI portraits.

## 7. Exploration (`Assets/Scripts/Exploration/`)

`ExplorationManager` / `ExplorationState`: ambient explore mode driving wave spawning and quest-level scaling. Quest discovery/fragments are config knobs without consumers yet.

## 8. Managers (`Assets/Scripts/Managers/`)

`GameManager` (scene singleton): owns Party, VerbPool, ExplorationManager, LootInventory, gold, questLevel.
- `ProcessTick()`: combat tick → gold → loot drops → ability XP
- Events: `OnCombatTick`, `OnGoldChanged`, `OnWaveStarted`, `OnWaveCleared`, `OnVerbActivated`, `OnLootDropped`, `OnRosterChanged`
- Player actions: `OnVerbTapped(slot)`, `SellItem`, `EquipItemFromInventory`, `AutoEquipCharacter`, `LevelUpAbility`, `BuildPartyFromRoster`
- Save on pause/quit via `SaveManager`/`SaveData` (local JSON: roster, equipment, inventory, gold, quest level, pity counters)

## 9. UI (`Assets/Scripts/UI/`, 27 files)

- **Explore screen:** `ExploreSceneController` (event subscriber, DeferredInitialSync coroutine for Start() ordering), `TopBarDisplay`, `VerbBarDisplay` + `VerbCardAnimator`, `EnemyDisplayController`, `DamageNumberSpawner`, `GoldCounterAnimator`, `PartyPortraitStrip`, parallax background
- **Party screen:** `PartyScreenController`, `RosterGridDisplay`, `CharacterFocusDisplay`, `EquipmentSlotsDisplay`, `EquipmentDrawer` (3-column equipment card layout), `ActionLoadoutDisplay`
- **Loot screen:** `LootScreenController`, `ItemDetailPanel`, `ItemDisplayData`
- **Infrastructure:** `ScreenManager` + `BottomNavDisplay` (screen switching), `NumberFormatter` (big-number notation), `StatTypeColors`, `SafeAreaAdapter`
- Canvas + TextMeshPro, CanvasScaler 1080x1920 match height

**Known deficiency:** equipment/loot/item displays are poorly designed for player readability. Stat pairs and awakened abilities cannot be meaningfully verified in gameplay through the current UI. A readability redesign is a roadmap item.

## 10. Editor Tooling (`Assets/Editor/`)

- `SetupGameManager` (Tools menu): creates config assets + GameManager + EventSystem fix
- `ExploreSceneBuilder` (Tools menu): full scene rebuild; always save scene after
- `ClearSave`: wipes local save
- `SpriteImportFixer`: import settings for paper-doll sprites

---

## 11. Not Yet Implemented (MVP remainder)

| System | Notes |
|---|---|
| Quest system backend | `Assets/Scripts/Quests/` is empty; `QuestZoneDefinition` + questDiscoveryRate exist unused |
| Quests screen UI | accept / retreat / complete flow |
| Shop screen + boosts | purchasable boosts, timed chest |
| Offline earnings claim | `OfflineGold()` formula exists; no claim flow/modal |
| Equipment/loot UI readability | current displays block gameplay verification of the stat redesign |
| Character level-up / stat allocation | not started |
| Monetization (ads/IAP) | `com.unity.purchasing` 5.4.0 + `com.unity.ads` installed, unused (4.x purchasing produced package errors; upgraded) |
| Cloud save / analytics / Remote Config | not started |
| Prestige, dungeon keys, fragments | post-MVP (design docs exist: `dungeon-key-system-design-doc.md`) |

The remaining-work roadmap lives in `docs/roadmap.md`.
