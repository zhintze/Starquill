# Destinations: Locations & Dungeons — Design

**Date:** 2026-07-02 (rev 3, all decisions resolved) · **Status:** Ready for implementation planning — supersedes `docs/dungeon-key-system-design-doc.md` (generic idle-clicker doc, kept for provenance) and rev 1 of this file (which had a third "Encounters" mode, now removed).

**All `[D#]` markers are resolved; §9 records the resolutions.**

---

## 1. Overview

Destinations is the post-MVP layer teased on the Quests screen. Two modes:

| Mode | Player fantasy | Entry | Session shape |
|---|---|---|---|
| **Locations** | "I found Blackroot Castle — something interesting is in there" | Fragment bar fills → discovery | One-time visit: fixed-length wave run ending in a dialogue/fanfare finale |
| **Dungeons** | "I built the exact key for the loot I want" | Spend a key (droppable, fusable) | Timed wave rush; the key defines loot targeting and difficulty |

The split: **Locations are the story-flavored, one-time surprises** — recruiting, dialogue choices, quest starts, boss fights, unusual rewards. **Dungeons are the repeatable, player-directed loot engine** — keys and key fusion let players target exactly the drop they want, at a difficulty price.

The shared skill hook: **waves cleared over par grant bonus curated rolls** in both modes (par is time-based in fixed-length Locations, wave-based in timed Dungeons). Firing advantage Verbs kills faster — Verb skill *is* the performance score; no new tracking systems.

**Non-goals for v1:** push notifications, remote config, analytics, ads/IAP touchpoints, leaderboards, key trading.

---

## 2. Keys

Keys open Dungeons. A key is an **instance** (not a count): it carries one or more modifiers plus a difficulty value.

### Three base kinds

| Kind | Modifier carried | Variants |
|---|---|---|
| **Color key** | All loot in the dungeon drops with a primary color from this family | 8 color families `[D1]`: Red, Orange, Brown, Yellow, Green, Blue, Purple, Neutral |
| **Type key** | Heavily weights the equipment slot of drops | 7: Head, Torso, Arms, Legs, Feet, Weapon, Misc |
| **Class key** | Loot rolls both stats of a build archetype (one as primary, one as secondary) | 15: every 2-stat combination (see table) |

Color families are hue-buckets over the existing 1,027-color "main" palette, classified at load (HSV ranges) and exposed as `ColorManager.GetRandomColor("main", rng, family)`. The paper-doll payoff: a Brown-key dungeon visibly showers brown gear.

**Class archetypes (15):**

| | | |
|---|---|---|
| STR+DEX Warrior | STR+CON Juggernaut | STR+INT Battlemage |
| STR+WIS Warden | STR+CHA Warlord | DEX+CON Skirmisher |
| DEX+INT Saboteur | DEX+WIS Ranger | DEX+CHA Trickster |
| CON+INT Sentinel | CON+WIS Guardian | CON+CHA Champion |
| INT+WIS Sage | INT+CHA Occultist | WIS+CHA Oracle |

Class-key loot: primary/secondary are the archetype's two stats (which one is primary rolls 50/50); stat budget unchanged.

### Difficulty

Every key has **difficulty D ≥ 1**. Base keys drop at D1 (85%), D2 (13%), D3 (2%) at every quest level — fusion is the main ladder to high D `[D2]`. Difficulty drives the dungeon, on a **steep risk/reward curve** `[D3]`:

- Enemy HP/damage: `× (1 + 0.6 × (D − 1))`
- Curated-roll rarity floor: D1 → Uncommon, D2 → Rare, D3 → Epic, D4 → Epic + doubled Legendary weight, **D5+ → Legendary**
- Base curated rolls: `2 + D` (capped 6)
- Rush duration: `150 s + 20 s × (D − 1)`

High-fusion keys are meant to be genuinely dangerous jackpots: a D5 key against an under-geared party clears few waves and wastes its potential.

### Fusion — the gold sink

Players fuse two keys into one at a **large gold cost**. The result **sums their difficulty**, and modifiers combine by rule `[D4]`:

- **Different kinds** (color + type, type + class, …): result carries **all modifiers of both**.
- **Same kind, same variant** (brown + brown): modifiers merge — this is the **upgrade path**, a stronger key of the same targeting `[D2]`.
- **Same kind, different variant** (brown + red): **invalid fusion**, rejected in UI. Every key keeps a crisp promise: at most one color, one slot, one class.
- Max D is capped at 6.

```
Brown color key (D1) + Helmet type key (D1)
    → Brown Helmet key (D2): brown-family, helmet-weighted drops, harder enemies
    + Warrior class key (D1)
    → Brown Warrior Helmet key (D3): the exact item hunt, at real difficulty
Brown color key (D1) + Brown color key (D1)
    → Brown color key (D2): same hunt, higher stakes and floor
```

- Cost scales with ambition: `fusionBaseCost × questLevel × D_result²` `[D5]` — quadratic, so triple-fused precision hunts are late-game purchases (base 250 at quest level 40: D3 ≈ 90k gold, D5 ≈ 250k).
- Fusion UI lives in the key pouch; preview shows the resulting key and cost before confirming.

### Acquisition & cap

- **Per active kill:** `keyDropRate` (proposed 0.004). Kind rolled 40/30/30 color/type/class; variant uniform (color family weighted toward palette density).
- **Quest rewards:** Elite → 1 key; Hard → 1 key + 25% second; Zone Boss → 1 D2 key. Added to `QuestRewardSpec`.
- No offline key drops (fragments cover offline).
- **Soft cap 30 keys:** over cap, drop rate halves. Never blocked. Keys are sellable for gold `[D6]`.

---

## 3. Dungeons — key-defined timed rushes

### Flow

Key pouch → select key → preview (modifiers, difficulty, duration, rarity floor, enemy strength) → confirm consumes key → **timed wave rush** → summary sheet with curated rolls.

### Mechanics

- Clear as many waves as possible before the clock ends. Explore-style waves (2–5 enemies, HP ramp), scaled by quest level × difficulty multiplier.
- Enemy stat types: uniform mix by default; a class key themes 60% of enemies to its archetype's stats (fight the build you're farming).
- Every 5th wave: mini-boss (HP ×4) paying one immediate curated roll. **Not every run has a boss** — no other scripted ending; the bell is the ending.
- No fail state; the only limit is the clock. Normal per-kill gold/drops continue (also key-modified: color/slot/class filters apply to *all* drops inside the dungeon).

### Rewards

At the bell: `(2 + D)` curated rolls `+ 1 per 3 waves over par, max +3` (par = tuned expected clears for the party's quest level). Every roll applies the key's full modifier set:

- Color family forced (color modifier) — new optional palette-family filter in roll path.
- Slot heavily weighted, ~80/20 (type modifier).
- Both archetype stats forced (class modifier) via `GenerateStatPair` overload.
- Rarity: `RollRarityWithFloor(questLevel, floorFromD, rng)`.

---

## 4. Locations — one-time discovered visits

### Discovery

Single fragment counter as built (`FragmentProgress`, 0.01/kill, offline 0.25 efficiency). Bar fills to `fragmentTarget` → a location is discovered, tier rolled 60/25/12/3 (Common/Rare/Epic/Legendary). One pending at a time; bar holds at full while pending. Pending lasts 24 h, then expires refunding 50% of fragments. **The run starts on activation, not discovery** — absence is never punished.

**Every location is procedurally generated and visited once, ever** `[D9]`. There is no template pool to drain: a `LocationGenerator` mints each discovery unique from parts data (`location_parts.json`), the same pattern as character name generation:

- **Name grammar:** adjective/noun/place-type pools per biome flavor → *Blackroot Castle*, *Bogun's Bog*, *the Sunken Antler Court*.
- **Rolled identity:** dominant stat types, flavor line, tier.
- **Assembled ending:** dialogue text and outcome set composed from a component library of ending archetypes (a gatekeeper, a stranded traveler, a sealed vault…) with parameterized text slots.

Generation is seeded from a persistent `locationCounter`, so a discovery survives save/load by regeneration (the quest-spec pattern). Visited locations are simply gone; only the counter and lightweight history (names seen) persist.

### The run — fixed length

A location visit is a **fixed-length wave run** (not a timed window):

| Tier | Waves | Rarity floor (finale + mini-boss drops) |
|---|---|---|
| Common | 8 | — |
| Rare | 10 | Uncommon |
| Epic | 13 | Rare |
| Legendary | 16 | Epic |

- Drop rate ×4 and gold ×2 for the whole run; keys and fragments ×3.
- A mid-run rare spawn (mini-boss) pays a floored drop.
- **Fever finale:** the last 3 waves double all multipliers again.
- **Par is time-based** here: clear the full run under the tier's par time → bonus curated rolls at the finale (+1 per 20% under par, max +3).
- Waves themed to the location's dominant stats (data-driven, like quest zones).

### The finale — dialogue & fanfare

Every location ends in a **scripted ending event**, the thing Locations have that nothing else does. The ending is a **decision tree**: a dialogue prompt with 2–3 responses, and the response chosen changes what you get.

**v1 outcome types:**

| Outcome | Effect |
|---|---|
| `Recruit` | A generated NPC (species/level near party average) joins the roster `[D7]` |
| `Loot` | Curated equipment roll(s), floored by tier, optionally themed |
| `ShopBoost` | A free timed boost activates (reuses `BoostManager`) |
| `QuestStart` | Immediately offers a quest (fires the existing offer flow; falls back to `Loot` if a quest is already active) |
| `BossFight` | One extra boss wave; winning pays a second, bigger curated roll |
| `Gold` | Flat gold payout scaled by tier × quest level |

**Decision-tree data model (built basic-first, per direction):**

```json
"ending": {
  "nodes": [
    { "id": "root",
      "text": "A hooded figure blocks the gate. 'State your business.'",
      "choices": [
        { "text": "We come in peace.",      "outcome": { "type": "Recruit" } },
        { "text": "Stand aside or fall.",   "outcome": { "type": "BossFight" } },
        { "text": "We're just here to trade.", "next": "trade" }
      ] },
    { "id": "trade", "text": "...", "choices": [ ... ] }
  ]
}
```

Nodes chain via `next`, leaves carry an `outcome`. v1 endings are one node deep; the tree structure exists so later features (stat-gated choices, party-member callouts, multi-step scenes) slot in without a data migration. Choices have **no wrong answers that void the reward** — they select *which* reward `[D8]`.

**One-time:** a completed location is gone forever — uniqueness comes from procedural generation (§Discovery), not from a reusable template pool, so there is nothing to burn out `[D9]`.

---

## 5. Economy knobs (EconomyConfig additions)

```
[Header("Destinations")]
keyDropRate           = 0.004      // per active kill
keyKindWeights        = 40/30/30   // color/type/class
keyDifficultyWeights  = 85/13/2    // D1/D2/D3 at drop
keySoftCap            = 30
keyMaxDifficulty      = 6
fusionBaseCost        = 250        // × questLevel × D_result²
dungeonDurationBase   = 150 s (+20 s per D above 1)
dungeonEnemyMultPerD  = 0.6
dungeonBonusPer       = 3 waves over par, max +3
fragmentTarget        = 100        // moved from QuestsScreenController const
locationWaves         = 8/10/13/16 by tier
locationDropMult      = 4;  locationGoldMult = 2;  locationKeyFragMult = 3
locationFeverWaves    = 3  (multipliers ×2)
locationParBonus      = +1 roll per 20% under par time, max +3
locationPendingHours  = 24; locationExpireRefund = 0.5
```

Balance flags for the tuning pass: first-discovery pacing (100 fragments at 0.01/kill is slow — consider a 25–30 fragment first milestone), fusion cost curve against late-game gold income (`tools/balance_sim.py` should grow a destinations branch), dungeon par tables per quest level.

---

## 6. Architecture

Follows the quest pattern throughout: **plain C# + deterministic generation + JSON data + GameManager executes rewards.** No ScriptableObject-per-item.

### New assembly: `Starquill.Destinations`

References Core (+ Quests for `WaveSpec` reuse). Produces *specs*; gold math and loot rolls stay in GameManager.

```
Assets/Scripts/Destinations/
├── KeyInstance.cs        { colorFamily?, slot?, archetype?, difficulty } + display name builder
├── KeyPouch.cs           List<KeyInstance>; add/consume/sell, soft-cap query
├── KeyFusion.cs          validation (one modifier per kind, D cap), cost formula, fuse
├── ClassArchetype.cs     the 15 stat-pair archetypes + names
├── ColorFamily.cs        enum + HSV classification ranges
├── DungeonGenerator.cs   (KeyInstance, runCounter, questLevel) → DungeonSpec
├── DungeonSpec.cs        duration, wave template, reward spec (rolls/floor/modifiers)
├── LocationGenerator.cs  (locationCounter, questLevel) → LocationSpec; name grammar +
│                         ending assembly from location_parts.json
├── LocationState.cs      pending/active lifecycle; pure functions over injected nowUnix
├── LocationSpec.cs       generated name/identity, fixed wave list, par time, ending tree
├── DialogueTree.cs       nodes/choices/outcomes model + JSON parsing
└── EndingOutcome.cs      outcome types + parameters
```

### Touch points in existing code

- **`ColorManager`**: hue-bucket the "main" palette into families at load; `GetRandomColor(palette, rng, ColorFamily?)`.
- **`EquipmentFactory`**: `GenerateStatPair` overload taking forced stats (one or both); slot-weighted prefix picker; color-family passthrough into `CreateRandom`/`CreateRandomWeapon`.
- **`ExplorationState`**: add `InDungeon`, `InLocation`. Combat ticks reuse `CombatTickProcessor` unchanged; mode changes wave sourcing and reward multipliers only.
- **`QuestRewardSpec`**: key payouts per tier.
- **`GameManager`**: `StartDungeon(key)`, `EnterLocation()`, ending-event execution (dialogue outcome dispatch: roster add, boost grant, quest offer, boss wave injection), end-of-run reward execution mirroring `CompleteQuest`, mailbox reuse for overflow.
- **`SaveData`**: serialized key list, pending/active location (unix timestamps), run counters incl. `locationCounter` (pending/active locations regenerate from seed on load, quest-spec style). Fragment progress already saved.
- **Events**: `OnKeyDropped`, `OnLocationDiscovered`, `OnLocationExpired`, `OnDungeonEnded`, `OnLocationEndingReached` (drives the dialogue UI).

### UI (Quests screen, Destinations section goes live)

- **Key pouch**: key list with modifier chips + difficulty pips; fuse flow (pick two, preview result + cost); sell.
- **Dungeons card** → key picker → run preview → in-run HUD: countdown + wave counter + par indicator.
- **Location card**: pending (name, tier, art hook, Enter, 24 h countdown) / active (wave progress) / empty (fragment bar as today).
- **Ending dialogue sheet**: portrait/flavor text + choice buttons; result fanfare reuses the quest reward sheet.
- **Unlock gating:** Destinations unlock after the first Zone Boss for now; the final gating scheme is deferred to the tutorial implementation (Sprint 12+) `[D10]`.

---

## 7. Testing

Same style as the existing EditMode suite:

- `KeyFusion`: modifier-exclusivity rules, difficulty summing/cap, cost formula.
- `DungeonGenerator` / location spec generation: determinism, modifier application, D → floor/duration mapping.
- `ColorFamily` classification: every palette entry lands in exactly one family; family filter returns only members.
- `GenerateStatPair` forced-stat overloads: forced stats honored, budgets unchanged.
- `DialogueTree`: JSON parse, node chaining, outcome dispatch table.
- `LocationGenerator`: deterministic regeneration from seed, name-grammar output validity, ending assembly (every generated ending parses and every choice reaches an outcome).
- `LocationState`: discovery trigger, pending expiry + refund — injected `nowUnix`.
- Reward execution: roll counts, floors, par-bonus math, mailbox overflow, roster-full recruit fallback.

---

## 8. Phasing

1. **Sprint D1 — Keys + Dungeons:** ColorFamily bucketing, KeyPouch, drops, fusion, DungeonGenerator, modifier-applied rolls, dungeon UI + HUD + summary.
2. **Sprint D2 — Locations:** fragment consumer, LocationGenerator + location_parts.json (name grammar, ending archetypes), fixed-length runs, DialogueTree + ending outcomes (all six types), location UI + dialogue sheet.
3. **Sprint D3 — Content + polish:** more location parts (name grammars, ending archetypes), archetype/color key art, balance pass with sim branch, fanfare/juice.

---

## 9. Decision log (all resolved 2026-07-02)

| # | Resolution |
|---|---|
| D1 | HSV-bucketed color families classified automatically at load. Originally 8; revised post-D1 (2026-07-02, PM request) to 10: White and Black split out of Neutral (Black = v<0.20 incl. dark tinted shades; White = low-sat v>=0.75; Neutral = true grays) |
| D2 | Base keys drop D1 85% / D2 13% / D3 2% at every quest level; **same-variant fusion is the upgrade path** (brown D1 + brown D1 → brown D2) |
| D3 | **Steep risk/reward:** enemy mult +0.6/D; floors D1 Unc / D2 Rare / D3 Epic / D4 Epic + 2× Legendary weight / **D5+ Legendary** |
| D4 | Max one modifier per kind per key; same-kind different-variant fusion is invalid; D cap 6 |
| D5 | Fusion cost = fusionBaseCost × questLevel × D_result² (quadratic) |
| D6 | Excess keys sellable for gold (difficulty-scaled pricing) |
| D7 | Recruits always join — roster has no cap; if one is ever added, fall back to Gold |
| D8 | No whiffs: dialogue choices select *which* reward, never nothing |
| D9 | Locations are **one-time ever, procedurally generated unique** (LocationGenerator + location_parts.json); no template pool |
| D10 | Unlock after first Zone Boss for now; final gating deferred to tutorial implementation |

**Resolved in design review (same day):** Encounters removed as a mode (par-bonus mechanic retained in both remaining modes) · key color = literal loot color, not stat · key kinds = color/type/class · class keys = 15 two-stat archetypes · fusion with summed difficulty · dungeons = timed wave rush, no fixed floors, no guaranteed boss · locations = one-time, fixed-length, dialogue-tree finale, fixed-length confirmed over timed window.
