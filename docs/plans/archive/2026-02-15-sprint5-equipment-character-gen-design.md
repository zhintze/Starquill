# Sprint 5: Equipment & Character Generation — Design Document

**Sprint:** 5
**Date:** 2026-02-15
**Status:** Approved
**Prerequisite:** Sprint 4 (combat wiring) complete, 99 tests passing

---

## Goal

Port the Godot equipment system to Unity, add rarity/affix generation, build a character factory for full random generation, and implement a character roster with save/load. After this sprint, the game launches with 8 randomly generated characters (each with random equipment visible on their paper dolls), 4 selected as the active party, with all data persisting across sessions.

## Architecture Decisions

### 1. JSON Catalog (not ScriptableObjects)
Equipment data loads from `equipment.json` and `weapons.json` (ported from Godot archive), matching the existing species JSON loading pattern via MiniJSON. The existing `EquipmentDefinition` ScriptableObject is replaced by runtime data.

### 2. Runtime EquipmentInstance
Equipment exists only as runtime objects created by the factory. No hand-authored equipment assets. Each instance has rolled affixes, random colors, and a variant number — immutable after creation.

### 3. Character Roster Model
Players have a roster of collected characters (cap: 20, initially 8 starters). Active party is 4 selected from the roster. Roster is the unit of save/load. Future gacha/reward systems add to the roster.

---

## Data Model

### EquipmentCatalog

Loads from `equipment.json` (70+ items) and `weapons.json` (9 weapons). Each catalog entry:

```
CatalogEntry
├── itemType: string          (e.g. "tr03", "hd01", "w05")
├── description: string       (human-readable name)
├── amount: int               (variant count)
├── layerCodes: int[]         (display layer indices)
├── hiddenLayers: int[]       (species layers this hides)
├── layerColorVariance: int[] (layers that get random colors)
├── modular: bool             (has modular variants)
└── handType: string          (weapons only: "one_handed" / "two_handed")
```

Equipment type prefixes and their slots:
- `hd##` → Head (13 types)
- `tr##` → Torso (14 types)
- `ar##` → Arms (5 types)
- `lg##` → Legs (2 types)
- `fe##` → Feet (2 types)
- `mc##` → Misc (13 types)
- `w##` → MainHand/OffHand (9 types)

### EquipmentInstance

Runtime equipment object, created only by the factory:

```
EquipmentInstance
├── itemType: string
├── itemNum: int              (visual variant)
├── slot: EquipmentSlot       (resolved from prefix)
├── rarity: Rarity
├── baseColor: Color
├── varianceColors: Dictionary<int, Color>
├── statMods: Stats           (base stats from rarity)
├── rolledAffixes: List<RolledAffix>
├── layerCodes: int[]         (from catalog)
├── hiddenLayers: int[]       (from catalog)
├── layerColorVariance: int[] (from catalog)
├── modular: bool
├── handType: string          (weapons only)
└── displayName: string       (generated: "Rare Iron Helm")
```

### RolledAffix

```
RolledAffix
├── affixId: string
├── statType: StatType
├── value: float
├── isPercentage: bool
```

Affix count by rarity:
- Common: 0 affixes
- Uncommon: 1 affix
- Rare: 2 affixes
- Epic: 2-3 affixes
- Legendary: 3 affixes

### AffixTable

Loaded from `affixes.json`. Defines the pool of possible affixes per equipment slot:

```
AffixEntry
├── affixId: string
├── displayName: string
├── statType: StatType
├── isPercentage: bool
├── validSlots: EquipmentSlot[]
├── valueRanges: Dictionary<Rarity, {min, max}>
```

### CharacterRoster

```
CharacterRoster
├── characters: List<CharacterInstance>
├── activePartyIndices: int[4]
├── rosterCap: int (20)
├── AddCharacter(CharacterInstance)
├── RemoveCharacter(int index)
├── SetPartyMember(partySlot, rosterIndex)
├── GetActiveParty() → CharacterInstance[4]
```

---

## Equipment Factory

### Core Methods

**CreateRandom(string prefix, Rarity rarity):**
1. Filter catalog entries matching the prefix
2. Pick a random entry
3. Roll a random variant number (1..amount)
4. Pick base color from equipment palette
5. Pick variance colors for layerColorVariance layers
6. Generate base stat mods from rarity tier
7. Roll affixes via AffixTable
8. Return immutable EquipmentInstance

**CreateRandomLoadout(int questLevel):**
Generates a full 11-slot set using Godot's priority/chance system:

| Slot | Chance | Priority |
|------|--------|----------|
| Torso | 100% | 2 (highest) |
| Legs | 100% | 1 |
| Head | 90% | 0 |
| Arms | 80% | 0 |
| Feet | 70% | 0 |
| Weapon | 60% | 0 |
| Misc (x4) | 30% each | 0 |
| Off-hand | 30% if main hand is one-handed | 0 |

Priority slots are equipped first. Rarity distribution weighted by quest level.

**RollAffixes(Rarity rarity, EquipmentSlot slot):**
1. Determine affix count from rarity
2. Filter AffixTable entries valid for this slot
3. Pick N random affixes (no duplicates)
4. Roll values within rarity's min/max range
5. Return List<RolledAffix>

**Reconstruct(SerializedEquipment data):**
Rebuilds an EquipmentInstance from save data without re-rolling. Looks up catalog entry by itemType, restores all fields from serialized data.

### Rarity Distribution

At quest level 1: ~80% Common, ~20% Uncommon.
Formula: each rarity has a weight that shifts with quest level. Exact tuning via EconomyConfig.

### Stat Generation

Base stats from rarity (before affixes):

| Rarity | Stat Budget | Distribution |
|--------|------------|--------------|
| Common | 0-2 total | 1 random stat |
| Uncommon | 2-4 total | 1-2 stats |
| Rare | 4-8 total | 2 stats |
| Epic | 8-14 total | 2-3 stats |
| Legendary | 14-20 total | 3 stats + guaranteed unique |

Affixes add on top of base stats.

### Color System

ColorManager (existing) extended with equipment palette. Base color randomly picked. Variance layers get independent random colors. Colors serialize to save data as RGBA.

### Conflict Detection

Ported from Godot:
- Same itemType in multiple slots: last wins
- Overlapping hidden layers: equipment with more coverage wins
- Two-handed weapons clear off-hand slot
- Misc slots overflow: items go to first empty misc slot

---

## Character Factory

### Core Methods

**CreateRandom(SpeciesDisplayData species, int level, int questLevel):**
1. Generate SpeciesInstanceData (random visual traits)
2. Set base stats from species definition
3. Generate full equipment loadout via EquipmentFactory.CreateRandomLoadout(questLevel)
4. Equip items with conflict detection
5. Pick 2 verbs matching the species' highest stat type
6. Generate a random display name
7. Return complete CharacterInstance

**CreateStarterRoster(int count = 8):**
1. Load all available species
2. Pick `count` species ensuring variety (max 2 of same species)
3. Create each character at level 1, quest level 1
4. Return list of CharacterInstance

### Name Generation

Simple approach: load a names list from JSON (first names + epithets). "Thorgrim the Bold", "Elara Swiftblade", etc. ~50 first names, ~20 epithets, combined randomly.

### Verb Assignment

Each character gets 2 verbs from a starter verb pool. Verb selection biased toward the character's highest stat type:
- Primary stat verb: guaranteed
- Secondary verb: random from remaining pool

Starter verb pool (same 4 as current, expandable):
- Slash (STR), Shield Bash (CON), Fireball (INT), Heal (WIS)

Future sprints add more verbs to the pool.

---

## Save/Load

### SaveData Structure

```
SaveData
├── gold: double
├── currentQuestLevel: int
├── roster: SerializedCharacter[]
│   ├── id: string
│   ├── displayName: string
│   ├── speciesId: string
│   ├── level: int
│   ├── xp: int
│   ├── speciesKills: int
│   ├── baseStats: Stats
│   ├── allocatedStats: Stats
│   ├── speciesVisuals: SerializedSpeciesInstance
│   │   ├── skinColor, hairColor, eyesColor
│   │   ├── facialDetailColor, skinVarianceColors
│   │   ├── modularImageNums, chosenHairGroup
│   │   └── scaleX, scaleY
│   ├── equipment: SerializedEquipment[11]
│   │   ├── itemType: string
│   │   ├── itemNum: int
│   │   ├── rarity: int
│   │   ├── baseColor: float[4]
│   │   ├── varianceColors: Dictionary<int, float[4]>
│   │   └── rolledAffixes: SerializedAffix[]
│   └── equippedVerbIds: string[]
├── activePartyIndices: int[4]
└── pityTracker: PityData
    ├── killsSinceUncommon: int
    ├── killsSinceRare: int
    ├── killsSinceEpic: int
    └── killsSinceLegendary: int
```

### Serialization

Uses existing MiniJSON pattern. `SaveManager.Save()` serializes the full roster. `SaveManager.Load()` reconstructs all CharacterInstance and EquipmentInstance objects via factory Reconstruct methods.

### Migration

Existing saves (gold + questLevel only) load without error. Missing roster triggers `InitializeStarterRoster()`. No save format versioning needed yet — first roster-aware save is the baseline.

---

## Display Integration

ExploreSceneController.SetupPlaceholderParty() currently creates SpeciesInstanceData and renders species-only paper dolls. Sprint 5 changes this:

1. Get active party from roster
2. For each character, build EquipmentDisplayInfo list from their EquipmentInstance[] slots
3. Pass equipment to DisplayBuilder.Build() alongside species data
4. Paper dolls now render with visible equipment (hats, armor, weapons)

The DisplayBuilder already supports EquipmentDisplayInfo — this just requires bridging EquipmentInstance → EquipmentDisplayInfo, which is a simple mapping of fields.

---

## Testing Strategy

### Edit Mode Tests (Pure C#)

**EquipmentCatalog tests:**
- JSON loads correctly, all entries present
- Prefix filtering returns correct items
- Weapon hand types parsed correctly

**EquipmentFactory tests:**
- CreateRandom produces valid instances per rarity
- Affix count matches rarity tier
- Affix values within min/max bounds
- CreateRandomLoadout fills priority slots always
- Chance-based slots respect probability (statistical test over N runs)
- Conflict detection: two-handed weapons clear off-hand
- Reconstruct round-trips correctly (create → serialize → reconstruct → equals)

**CharacterFactory tests:**
- CreateRandom produces character with equipment in all priority slots
- CreateStarterRoster produces requested count with species variety
- Verb assignment matches highest stat type

**CharacterRoster tests:**
- Add/remove characters
- Active party indices stay valid after removal
- Roster cap enforced

**Save/Load tests:**
- Full round-trip: create roster → save → load → verify equality
- Migration: old save format (gold only) loads and triggers roster init
- PityTracker persists correctly

**Estimated: ~25-30 new tests, bringing total to ~125**

---

## Files Summary

### New Files (~12-15)
- `Assets/Scripts/Equipment/EquipmentCatalog.cs`
- `Assets/Scripts/Equipment/EquipmentInstance.cs`
- `Assets/Scripts/Equipment/EquipmentFactory.cs`
- `Assets/Scripts/Equipment/AffixTable.cs`
- `Assets/Scripts/Equipment/RolledAffix.cs`
- `Assets/Scripts/Characters/CharacterFactory.cs`
- `Assets/Scripts/Characters/CharacterRoster.cs`
- `Assets/Scripts/Characters/NameGenerator.cs`
- `Assets/Data/affixes.json`
- `Assets/Data/names.json`
- `Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs`
- `Assets/Tests/EditMode/Equipment/EquipmentCatalogTests.cs`
- `Assets/Tests/EditMode/Characters/CharacterFactoryTests.cs`
- `Assets/Tests/EditMode/Characters/CharacterRosterTests.cs`

### Modified Files (~5-8)
- `Assets/Scripts/Characters/CharacterInstance.cs` — equipment uses EquipmentInstance[]
- `Assets/Scripts/Managers/GameManager.cs` — roster integration, starter roster init
- `Assets/Scripts/Managers/SaveManager.cs` — expanded save/load
- `Assets/Scripts/UI/ExploreSceneController.cs` — equipment display on paper dolls
- `Assets/Scripts/Display/ColorManager.cs` — equipment palette support
- `Assets/Scripts/Data/EconomyConfig.cs` — rarity distribution config

### Data Files (ported from Godot)
- `Assets/Data/equipment.json` (70+ items)
- `Assets/Data/weapons.json` (9 weapons)

---

## Not in Scope (Sprint 5)

- Equipment enhancement/reroll/ascend/merge (Sprint 7: Loot screen)
- Set bonuses (Sprint 7)
- Loot drops during combat (Sprint 7)
- Party screen UI (Sprint 6)
- Loot screen UI (Sprint 7)
- Quest system (Sprint 8)
- Gacha / character acquisition (future)
- Codex collection log (future)

---

## Sprint Roadmap

| Sprint | Focus | Key Deliverables |
|--------|-------|-----------------|
| 5 | Equipment & character gen | Factory, catalog, affixes, roster, save/load |
| 6 | Party screen | Bottom nav, character detail, equip slots, party swap |
| 7 | Loot screen | Loot drops, inventory, equip/compare, upgrade paths |
| 8 | Quests | Quest zones, discovery, state machine, quest UI |
