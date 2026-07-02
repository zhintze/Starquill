# Destinations: Encounters, Locations & Dungeons — Design

**Date:** 2026-07-02 · **Status:** Draft for review — supersedes `docs/dungeon-key-system-design-doc.md` (that doc was written for a generic idle clicker: generators, prestige, tap combos, ScriptableObject-per-item; none of it matches Starquill's implemented systems). This doc re-grounds the same three ideas in the real codebase.

**Open decisions are marked `[D#]` and collected in §10 for veto.**

---

## 1. Overview

Destinations is the post-MVP layer teased on the Quests screen. Three modes, one shared resource system:

| Mode | Player fantasy | Targeting axis | Session shape |
|---|---|---|---|
| **Encounters** | "I need STR gear — hunt it" | **Stat** (key color forces primary stat) | Timed wave rush, 2.5–3.5 min |
| **Locations** | "A vault opened — grind it before it closes" | **Slot/volume** (biome biases slot + floods drops) | Real-time window, 8–15 min |
| **Dungeons** | "The big curated run" | **Both** (floors themed per triangle stat) | Untimed 3-floor gauntlet, ~10–15 min |

The loop: exploring drops **keys** and **fragments** → keys open Encounters (stat-targeted gear) → fragments discover Locations (drop volume) → full key *sets* open Dungeons (pinnacle curated loot) → better gear raises quest level → everything scales up.

**Non-goals for v1:** Gold keys, key IAP packs, ad-based extenders, push notifications, remote config, analytics events, leaderboards. All were in the generic doc; all cut (YAGNI). Monetization touchpoints can bolt on later without structural change.

---

## 2. Keys

### Model

A key is a `(color, tier)` pair. **Color = one of the six stats** `[D1]` — a STR key's encounter spawns STR-themed enemies and drops STR-primary equipment. This reinforces the dual-triangle identity and gives key colors mechanical meaning the player already understands.

Tiers reuse the existing rarity-floor machinery (`RollRarityWithFloor`):

| Tier | Name | Encounter duration | Curated-roll floor | Base rolls |
|---|---|---|---|---|
| 1 | Worn | 2:30 | Uncommon | 2 |
| 2 | Gilded | 3:00 | Rare | 3 |
| 3 | Radiant | 3:30 | Epic | 4 |

6 colors × 3 tiers = 18 inventory counts (an `int[18]` in save data — no per-key objects).

### Drops `[D2]`

- **Per active kill:** `keyDropRate` (proposed 0.004 ≈ one key per ~4 min of play). Tier roll: 80/17/3.
- **Color weighting:** 60% weighted to the current zone's `DominantTypes`, 40% uniform — the zone you farm shapes the keys you get, so zone choice is itself targeting.
- **Quest rewards:** Elite → 1 Worn key; Hard → 1 Worn + 25% Gilded; Zone Boss → 1 Gilded. Added to `QuestRewardSpec`.
- **No offline key drops** `[D3]` — keys are the active-play currency (fragments already cover offline).

### Cap & combining

- **Soft cap 30 total:** over cap, `keyDropRate` halves. Never blocked.
- **Combining (phase 2, not v1):** 3 same-color tier-N → 1 same-color tier-N+1. Cross-color recoloring deferred — Dungeons already sink off-color keys.

---

## 3. Encounters — targeted loot

### Flow

Quests screen → Encounters card → pick color + tier (shows enemy stat, counter-stat hint, rarity floor, duration) → confirm consumes key → timed wave rush → summary sheet with curated rolls.

### Mechanics

A **timed wave rush**: clear as many waves as possible before the clock runs out. This is the active-play contrast to idle exploring, and it converts Verb skill into loot with zero new tracking systems — firing advantage Verbs kills faster, which *is* the performance score.

- Waves generated like explore waves (2–5 enemies, HP ramp per wave), **enemy types 80% key-color stat**, seeded deterministically from `(color, tier, runCounter, questLevel)`.
- Every 5th wave: mini-boss (HP ×4) that pays one immediate curated roll.
- Enemy HP/gold scale off quest level via existing `EconomyConfig` formulas. No fail state — the only limit is the clock.
- Normal per-kill gold and loot drops continue during the run (mailbox absorbs overflow, as with quests).

### Rewards

At the bell: `baseRolls` (by tier, table above) `+ 1 bonus roll per 3 waves cleared beyond par, capped at +3` `[D4]` (par = tuned expected wave count). Every curated roll:

- **Primary stat forced to key color** — new `GenerateStatPair` overload with `StatType? forcePrimary` (secondary still rolls from the item's pools).
- Rarity via `RollRarityWithFloor(questLevel, tierFloor, rng)`.
- Slot/prefix rolled normally (encounters target stats, not slots — that's Locations' job).

---

## 4. Locations — discovered grind windows

### Discovery

Keeps the **single fragment counter already implemented** `[D5]` (`FragmentProgress`, drop rate 0.01/kill, offline at 0.25 efficiency, teaser bar targets 100). No per-location fragment types — that's a multi-currency inventory for little gain.

- Bar fills to `fragmentTarget` (move the UI's hardcoded `100f` into `EconomyConfig`) → a location is **discovered**: biome rolled from `locations.json`, tier rolled 60/25/12/3 (Common/Rare/Epic/Legendary).
- One pending location at a time; while one is pending the bar holds at full.
- **Windows start on activation, not discovery** `[D6]`: a pending location waits up to 24 h (then expires, refunding 50% of fragments). Tapping **Enter** starts the real-time window. Urgency lives inside the session; a player who discovers a vault at bedtime isn't punished.

| Tier | Window | Rarity floor (boss drops only) |
|---|---|---|
| Common | 15 min | — |
| Rare | 12 min | Uncommon |
| Epic | 10 min | Rare |
| Legendary | 8 min | Epic |

### The grind

Locations are the **volume** farm (Encounters do quality; Locations do quantity):

- Explore-style continuous waves themed to the biome's dominant stats.
- **Drop rate ×4, gold ×2, key & fragment drops ×3** for the window.
- Every 90 s: a rare spawn (mini-boss) paying a guaranteed drop at the tier's rarity floor.
- **Final 2 min = fever:** drop multipliers double again.
- Window runs on real time (`DateTime.UtcNow`, persisted as unix timestamps like the chest timer). Closing the app doesn't pause it.

### Biomes (v1: four) `[D7]`

Each biome biases the **slot** of its drops (70% biased, 30% normal) — this is what Encounters can't give:

| Biome | Dominant stats | Slot bias |
|---|---|---|
| Ember Forge | STR, DEX | Weapons |
| Crystal Garden | CON, WIS | Armor (torso/legs/arms) |
| Void Rift | INT, CHA | Misc/accessories |
| Sky Citadel | all | none — rolls one tier higher on the 60/25/12/3 table |

Data-driven via `Assets/Resources/Data/locations.json` (id, name, dominant stats, slot weights, flavor line), same pattern as `quest_zones.json`.

---

## 5. Dungeons — multi-zone curated runs

Designed from scratch (named in `gameplay-loop.md`, previously specced nowhere).

### Entry: key sets `[D8]`

A dungeon consumes **one key of each color in a triangle, all of the same tier**:

- **STR + DEX + CON** → *the Foundry Depths* (Physical dungeon)
- **INT + WIS + CHA** → *the Astral Archive* (Mental dungeon)

This gives off-color keys a purpose (no dead drops) and makes dungeons a deliberate save-up goal. Key tier sets the reward tier.

### Structure

Three **floors**, one per triangle stat, escalating:

- Floor = 4–6 waves (themed to that floor's stat) + a **floor boss** (HP ×4, reuse Elite shape).
- Final floor boss = triangle boss: HP ×8 with two typed adds (reuse Boss wave shape), covering all three stats — the full stat-matchup exam.
- **Untimed.** Retreat anytime; loot already banked is kept, remaining floors are forfeit (keys were spent on entry). Consistent with the no-fail philosophy: there is no death, only walking away.
- Deterministic generation from `(dungeonId, runCounter, questLevel)` — same pattern as `QuestGenerator`.

### Rewards

- **Each floor boss:** 1 curated roll, primary stat forced to the floor's stat, floored by key tier (Worn → Rare, Gilded → Epic, Radiant → Epic).
- **Final boss:** the **curated choice** — roll 3 items (primary stat = player's pick among the triangle, floor one tier above the floor-boss floor; Radiant → Legendary), present all 3, **player keeps 1** `[D9]`. This is the "curated loot" promise from the teaser: agency, not another slot machine.
- Gold: ×5 quest-style completion bonus; character XP: 2× boss-quest bonus.

---

## 6. Economy knobs (EconomyConfig additions)

```
[Header("Destinations")]
keyDropRate            = 0.004   // per active kill
keyTierWeights         = 80/17/3
keySoftCap             = 30      // over: drop rate halves
encounterDurations     = 150/180/210 s (by tier)
encounterBaseRolls     = 2/3/4
encounterBonusPer      = 3 waves over par, max +3
fragmentTarget         = 100     // moved from QuestsScreenController
locationWindowMinutes  = 15/12/10/8 (by tier)
locationDropMult       = 4;  locationGoldMult = 2;  locationKeyFragMult = 3
locationFeverMult      = 2;  locationFeverSeconds = 120
locationPendingHours   = 24; locationExpireRefund = 0.5
dungeonGoldMult        = 5
```

Balance flags (tune in the pass, not now): at 0.01/kill, 100 fragments ≈ many hours to first discovery — consider a first-discovery milestone at 25–30 fragments, or raising `fragmentDropRate`, once real session data exists. `tools/balance_sim.py` should grow an encounter/location branch.

---

## 7. Architecture

Follows the quest pattern throughout: **plain C# + deterministic generation + JSON data + GameManager executes rewards.** No ScriptableObject-per-item (the generic doc's approach conflicts with codebase conventions).

### New assembly: `Starquill.Destinations`

References Core (+ Quests for `WaveSpec` reuse). Like Quests, it produces *specs*; gold math and loot rolls stay in GameManager.

```
Assets/Scripts/Destinations/
├── KeyTier.cs            enum Worn/Gilded/Radiant
├── KeyInventory.cs       int[18] counts; TryConsume, Add, soft-cap query, combine (phase 2)
├── EncounterGenerator.cs (color, tier, runCounter, questLevel) → EncounterSpec
├── EncounterSpec.cs      duration, wave template, reward spec (floor + base rolls + forcePrimary)
├── LocationTable.cs      loads locations.json
├── LocationState.cs      pending {id, tier, discoveredAtUnix} / active {expiresAtUnix}; pure clock-in functions
├── DungeonGenerator.cs   (dungeonId, runCounter, questLevel) → DungeonSpec (3 floors of WaveSpec[] + floor rewards)
└── DungeonSpec.cs
```

Key colors are just `StatType` — no new enum.

### Touch points in existing code

- **`EquipmentFactory`**: add `forcePrimary` parameter to `GenerateStatPair` + a slot-weighted prefix picker for location bias. Both are small, testable extensions.
- **`ExplorationState`**: add `InEncounter`, `InLocation`, `InDungeon` (or a parallel mode enum on GameManager — decide at implementation). Combat ticks reuse `CombatTickProcessor` unchanged; the mode only changes wave sourcing and reward multipliers.
- **`QuestRewardSpec`**: add key payouts per tier.
- **`GameManager`**: `StartEncounter(color, tier)`, `EnterLocation()`, `StartDungeon(id, tier)`, end-of-run reward execution (mirrors `CompleteQuest`), mailbox reuse for overflow.
- **`SaveData`**: `int[] keyCounts`, pending/active location fields (unix timestamps), `encounterRunCounter`, `dungeonRunCounter`. Fragment progress already saved.
- **Events**: `OnKeyDropped`, `OnLocationDiscovered`, `OnLocationExpired`, `OnEncounterEnded`, `OnDungeonFloorCleared` — same event-driven UI pattern.
- **Clock**: location expiry uses `DateTime.UtcNow` at check time, computed in pure functions taking `long nowUnix` so tests inject time.

### UI (Quests screen, Destinations section goes live)

- **Key pouch row**: six color chips with counts (badged by tier).
- **Encounters card** → sheet: color grid + tier picker, preview (enemy stat, "bring X Verbs" counter-hint, rarity floor, duration), confirm.
- **Location card**: pending (biome art, tier, Enter button, 24 h countdown) / active (window countdown) / empty (fragment bar, unchanged).
- **Dungeons card**: two dungeons with key-set slots showing owned/needed.
- **In-run HUD**: reuse quest wave-progress bar; add countdown timer (encounters/locations) or floor indicator (dungeons). Summary sheets reuse the quest reward sheet; dungeon final adds the pick-1-of-3 row.
- **Unlock gating** `[D10]`: Destinations section stays locked until the first Zone Boss is beaten — keeps the early game focused and matches when key income (quest rewards) actually starts.

---

## 8. Testing

Same style as the ~344 existing EditMode tests:

- `EncounterGenerator` / `DungeonGenerator`: determinism, wave-type distributions, tier→floor mapping (mirrors `QuestGeneratorTests`).
- `KeyInventory`: add/consume/soft-cap, save round-trip.
- `GenerateStatPair(forcePrimary)`: forced primary honored, secondary never equals primary, budgets unchanged.
- `LocationState`: discovery trigger at target, pending expiry + refund, window expiry — all with injected `nowUnix`.
- Reward execution: curated roll counts, floors, bonus-roll math, mailbox overflow.

---

## 9. Phasing

Three implementation sprints, each independently shippable:

1. **Sprint D1 — Keys + Encounters:** KeyInventory, drops, EncounterGenerator, forced-primary rolls, Encounters UI + in-run HUD + summary. The fragment bar keeps teasing.
2. **Sprint D2 — Locations:** fragment consumer, locations.json, pending/active lifecycle, window multipliers, fever, location UI.
3. **Sprint D3 — Dungeons:** DungeonGenerator, key-set entry, floor flow, pick-1-of-3 reward, dungeon UI.

---

## 10. Open decisions (assumed, flagged for veto)

| # | Decision taken | Alternatives considered |
|---|---|---|
| D1 | Key color = one of six stats | Slot keys; triangle keys (3); rarity-only keys |
| D2 | Key sources: active kills (zone-weighted color) + quest-tier rewards | Milestones, daily login, ads (deferred with monetization) |
| D3 | No offline key drops | Offline at reduced rate |
| D4 | Performance = waves cleared over par → bonus rolls (max +3) | Combo/tap tracking (doesn't fit auto-battle) |
| D5 | Single generic fragment counter (as built) | Per-location fragment types (generic doc) |
| D6 | Window starts on activation; pending lasts 24 h, expiry refunds 50% | Window from discovery moment (generic doc; punishes absence) |
| D7 | 4 biomes, slot-biased loot; Locations = volume, Encounters = quality | Stat-biased locations (would duplicate Encounters) |
| D8 | Dungeon entry = full triangle key set, same tier | Single special key; gold keys |
| D9 | Final boss reward = pick 1 of 3 rolled items | Straight rolls; guaranteed slot choice |
| D10 | Unlock Destinations after first Zone Boss | From start; per-mode staggered unlocks |
