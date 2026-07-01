# Starquill: Idle RPG Clicker — Architecture & Systems Design
## "Bounty Bash"-Style Mobile Game (Unity)

> **⚠ Partially superseded (2026-07-01).** For implemented reality, see `docs/implemented-systems.md`; for remaining work, see `docs/roadmap.md`. Notably out of date here:
> - **Equipment:** the affix system, equipment sets, and enhance/reroll/ascend/merge described below were replaced by the primary/secondary stat pair + AwakenedAbility model (`docs/plans/2026-02-17-equipment-stat-redesign-plan.md`).
> - **Section 12 MVP Build Plan:** the 2-sprint plan was not followed; actual execution was Sprints 1-8 per `docs/sprint-review.md`.
> Everything else (core loop, verbs, advantage triangles, economy math, quest structure) remains the design of record.

---

## 1. Game Summary

Starquill is a mobile idle RPG where players assemble a party of 4 fantasy characters from distinct Species, equip them with visually-rendered gear, and send them exploring through an ever-scaling world of combat encounters. Characters auto-attack based on their stats while the player activates Verbs — powerful abilities randomly drawn from a shared party pool — to exploit a two-triangle advantage system (Physical: STR>DEX>CON, Mental: INT>WIS>CHA). Loot drops constantly, immediately changing the paper-doll appearance of all party members, feeding a collection-driven progression loop. Between quests, players upgrade equipment, swap party composition for advantage coverage, and push deeper into new zones. The economy scales exponentially with big-number notation, offline earnings accumulate while the party "explores" in the background, and a prestige system (post-MVP) allows costly resets for permanent multipliers via Stellar Ink. Monetization is rewarded-ad-first: 2x idle earnings, bonus chests, time skips — never pay-to-win.

---

## 2. Core Loop and Meta Loop

### Core Loop (every 5–30 seconds)

1. Party auto-attacks the current exploration wave's enemies.
2. Verb draw slots (3–4 visible) fill randomly from the shared pool.
3. Player taps drawn Verbs to fire them (single-target hits a random enemy, AoE hits all). Untapped Verbs rotate out after 10 seconds and a new Verb is drawn.
4. Enemies die, dropping gold + occasional loot.
5. Wave clears, next wave spawns.
6. Occasionally, a **Quest is discovered** — a structured multi-wave challenge with curated rewards. Or **fragments** accumulate toward encounters/locations/dungeons.

### Meta Loop (every 5–30 minutes and across days)

- Upgrade equipment (enhance, reroll affixes, ascend rarity, merge duplicates).
- Swap party members for advantage coverage against upcoming quest zones.
- Accept and complete discovered Quests for milestone rewards (locations, encounters, dungeons).
- Claim offline earnings from exploring (with optional rewarded ad for 2x).
- Progress through Quest Zones (escalating difficulty themes).
- Collect loot sets and fill the Codex for permanent bonuses.
- Rank up Species abilities through accumulated play.

### Player Inputs

| Input | Context |
|-------|---------|
| Tap Verb cards | Combat — fire drawn abilities |
| Tap to equip/swap loot | Loot and character screens |
| Tap upgrade buttons | Equipment enhance, character level-up |
| Claim buttons | Offline earnings, timed chests, quest rewards |
| Watch rewarded ads | 2x earnings, bonus chest, fragment boost |
| Set party composition | Party screen — swap members, assign verb loadouts |
| Accept/retry quests | Quest discovery popup, retry icon |
| Purchase boosts | Auto-fire verbs, verb speed-up (in-game currency) |

---

## 3. Entity Mapping: Starquill → Idle Clicker

### Species

- **Role**: A character trait selected at creation, like facial structure or hair style. Defines stat distribution (30 points across STR/DEX/CON/INT/WIS/CHA) and archetype identity. Each species has a unique **Species Ability** — a passive effect that's always active and ranks up through play.
- **Data**: Name, stat distribution array (30 total points), species ability reference, body part IDs (head, body, ears, eyes, hair, facial detail, facial hair, arms, legs, mouth, nose, other parts), color palettes (skin, hair, eyes), scale modifiers (x_scale, y_scale), item restrictions.
- **Math influence**: Determines Verb damage scaling (`charStat × verbStatScaling × 0.1`), status effect proc thresholds (stats below 4 = penalty), Verb slot count (level-gated), idle bonuses per stat (STR = +% base damage, DEX = +% crit, CON = +% party HP, INT = +% XP, WIS = +% loot discovery, CHA = +% gold).
- **Species Ability examples**:
  - Human "Adaptable": +5% damage with off-type Verbs (reduces mismatch penalty). Rank 5: +15%.
  - Dwarf "Stone Blood": CON Weaken effects last +1 tick. Rank 5: +2 ticks.
  - Elf "Keen Senses": DEX Bleed effects deal +10% per tick. Rank 5: +25%.
  - Skeleton "Undying": Party auto-revives once per encounter at 25% HP. Rank 5: 40% HP.
- **Species Ability Ranking**: Ranks 1–5, unlocked by accumulating kills with that species in the party. Thresholds: 100 / 500 / 2,000 / 10,000 / 50,000 kills.
- **UI**: Full paper-doll display on party screen. Species shown as visual identity (body shape, ears, facial structure, colors). Species ability displayed as a passive icon beneath character portrait with rank indicator.

### Character

- **Role**: A party member. The player builds a party of 3–4 characters who contribute to the shared Verb Pool. Each character has a Species (creation trait), equipped gear, and a Verb loadout.
- **Data**: ID, display name, SpeciesInstance (randomized visual traits from species template + fixed stat distribution), Stats object (base from species + equipment mods + level-up allocations), equipment slots (head, torso, arms, legs, feet, main hand, off hand, misc ×4 = 11 slots), Verb slots (2–5 based on level), level, XP, talent selections.
- **Math influence**: Character level scales base stats. Equipment stat mods add to base. Final stat values determine Verb damage, proc chances, and idle bonuses. Characters contribute their equipped Verbs to the shared pool.
- **UI**: Full paper-doll on combat/exploration screen and party management. Equipment slots visible on character detail. Verb loadout shown as draggable card slots. Coverage indicator shows party-wide advantage gaps.

### Equipment

- **Role**: The primary power progression lever and loot dopamine source. Equipment visually changes the paper doll and provides stat bonuses. Rarity tiers (Common through Legendary) with affixes.
- **Data**: Item type (e.g., "hd01", "tr03", "w01"), variant number, layer codes (visual z-order), layer color variance, hidden layers (species parts this equipment covers), modular flag, base color + variance colors, stat modifiers dictionary, rarity tier, affix list, set membership.
- **Math influence**: Flat stat additions (+3 STR, +2 DEX), percentage multipliers (+5% damage), set bonuses. Equipment is the main way characters gain stats beyond their Species base. Upgrade paths (enhance/reroll/ascend/merge) provide exponential stat growth.
- **UI**: Immediately visible on character paper doll when equipped. Loot drops show item preview with stat comparison. Color variance makes each piece visually unique. Rarity border glow on item icons (white/green/blue/purple/orange).

### DisplayObjects (DisplayPiece)

- **Role**: The visual rendering layer. Composites Species body parts + Equipment layers into layered 2D character sprites. This is what makes loot feel "awesome" — you SEE the new sword on your character immediately.
- **Data**: Layer z-order (0–999+), texture reference, color modulate (tint), offset/scale/rotation, flip flags (horizontal/vertical), offhand weapon flag.
- **Math influence**: None. DisplayObjects are pure presentation. Changes to equipment or species traits trigger a display rebuild via the DisplayBuilder.
- **UI**: All 4 party members rendered as layered sprite stacks on the exploration screen. Equipment preview on loot screens shows before/after comparison. Layer conflict resolution: equipment hidden_layers suppress species body part layers.

### Verbs

- **Role**: The primary player interaction system and the combat engine. Verbs are combat actions typed to one of 6 stats, forming the dual-triangle advantage system. Each Verb carries a stat type, damage profile, status effect, and cooldown. See `verb-stat-system-design-doc.md` for complete reference.
- **Data**: verbId, displayName, statType (STR/DEX/CON/INT/WIS/CHA), category (Physical/Mental), baseDamage, hitCount, targetMode (Single/Cleave/AoE), status effect (Stagger/Bleed/Weaken/Expose/Reveal/Confuse), effectProcChance, effectDuration, effectPotency, statScaling, levelRequirement, rarity, cooldownTicks, verbPoolPriority.
- **Math influence**:
  ```
  actualDamage = baseDamage
      × (1 + charStat × statScaling × 0.1)
      × advantageMultiplier        [0.67, 0.8, 1.0, 1.2, or 1.5]
      × exposeMultiplier           [1.0 or 1.25]
      × equipmentDamageBonus
      × prestigeMultiplier
      × boostMultiplier
  ```
- **Draw mechanic**: During combat, 3–4 Verb slots display randomly drawn Verbs from the shared pool. When tapped, the Verb fires at a random enemy (single-target) or all enemies (AoE/Cleave) and goes on cooldown. Untapped Verbs rotate out after 10 seconds and a new random Verb is drawn. Verbs do NOT auto-fire by default. **Auto-fire** and **verb speed-up** are temporary boosts purchasable with in-game currency.
- **UI**: Verb cards at bottom of exploration screen, color-coded by stat type (STR=orange, DEX=white, CON=brown, INT=blue, WIS=green, CHA=purple). Advantage/disadvantage arrows shown on enemies. Cooldown sweep animation. New Verb draws animate in with a card-flip effect.

---

## 4. Verbs as the Primary System

### Tick/Event Model

Combat runs on a deterministic tick system. One tick = 1 second (tunable via Remote Config).

**Each tick:**

1. **Auto-attack phase**: Each party member deals passive DPS based on their highest stat. This is the idle baseline — damage flows even with no player input.
2. **Verb draw check**: If any of the 3–4 Verb slots is empty, draw a random available Verb from the pool (not on cooldown). Populate the slot.
3. **Verb rotation check**: If a Verb has sat untapped in its slot for 10 seconds (tunable: `verbDrawCooldown`), remove it and draw a replacement. The rotated-out Verb re-enters the drawable pool (it is not put on cooldown).
4. **Player activation**: If the player taps a drawn Verb, it fires immediately. Single-target Verbs hit a random enemy. Cleave hits 2–3 random enemies. AoE hits all enemies. No manual targeting.
5. **Cooldown tick**: All Verbs on cooldown decrement by 1. Verbs coming off cooldown re-enter the drawable pool.
6. **Status effect tick**: Process all active effects per `verb-stat-system-design-doc.md` rules (Bleed damage, Stagger skip, Weaken reduction, Expose amplification, Reveal strip, Confuse misdirection).
7. **Death/wave check**: Remove dead enemies, spawn next wave if cleared, trigger quest boss if at milestone wave.

**Determinism**: Given the same pool composition, same draw seed, and same enemy wave, the outcome is reproducible. RNG exists only in draw order and single-target selection — neither creates progression dead-ends because the auto-attack baseline ensures enemies always die eventually.

**Purchasable Boosts** (in-game currency):

| Boost | Cost | Duration | Effect |
|-------|------|----------|--------|
| Auto-Fire Verbs | 500 gold × quest level | 5 minutes | Drawn Verbs auto-fire after 2 seconds instead of rotating out |
| Verb Speed-Up | 300 gold × quest level | 5 minutes | Verb cooldowns reduced by 50%, draw rotation reduced to 5 seconds |

### Dual-Triangle Advantage System

**Physical Triangle:**
```
        STR (Force)
       / ↑         \
  beats              loses to
     ↓                 ↑
DEX (Finesse) ←beats← CON (Endurance)
```

**Mental Triangle:**
```
        INT (Arcana)
       / ↑          \
  beats               loses to
     ↓                  ↑
WIS (Instinct) ←beats← CHA (Influence)
```

**Cross-Triangle Rules:**

| Attacker → Target | Damage Modifier | Status Effect Proc |
|-------------------|----------------|--------------------|
| Physical → Physical (triangle) | 1.0×, 1.5× (advantage), or 0.67× (disadvantage) | Normal (50% base) |
| Mental → Mental (triangle) | 1.0×, 1.5× (advantage), or 0.67× (disadvantage) | Normal (50% base) |
| Physical → Mental | 1.2× always | Effects CANNOT proc (0%) |
| Mental → Physical | 0.8× always | Effects ALWAYS proc (100%) |

Complete 6×6 advantage matrix: see `verb-stat-system-design-doc.md` Part 1.

### Verb Archetypes (6+)

| Archetype | Stat | Example Verb | Behavior |
|-----------|------|-------------|----------|
| **Burst Strike** | STR | Overhead Slam | High single-hit damage to random enemy. Stagger delays target's next action by 1 tick. |
| **Multi-Hit Shred** | DEX | Fan of Blades | 3–5 low-damage hits across all enemies. Bleed stacks (5% of hit damage/tick, up to 5 stacks) add sustained DPS. |
| **Damage Amp** | INT | System Crack | Moderate damage + Expose on random target. Exposed targets take +25% from all sources. The force multiplier. |
| **Tank/Counter** | CON | Iron Wall | Low damage to random enemy + Weaken (target deals -20% damage). Party survival tool. |
| **Heal/Cleanse** | WIS | Restoration | Heals all allies. Cleanses 1 Bleed stack per target. Reveal strips enemy buffs and prevents reapplication. |
| **Chaos/Misdirect** | CHA | Mass Hysteria | Moderate damage to all + Confuse. 30% chance confused enemies hit their own allies each tick. |

**Verb-driven temporary multipliers**: Effects like Expose (+25% damage taken) and Weaken (-20% damage dealt) are Verb output — multipliers applied within the damage formula with fixed durations and deterministic interaction rules. They are intrinsic to the Verb system, not a standalone subsystem.

**Status effect interaction rules** (from verb-stat doc):
- Confuse **replaces** Weaken if both would be active
- Reveal **cleanses** Confuse
- Weaken **reduces** Stagger duration by 1 tick
- Bleed **stacks** up to 5 from different sources
- Expose, Stagger, Weaken, Reveal, Confuse do NOT stack — reapplication refreshes duration
- CON Verbs have 50% chance to resist Expose
- INT-type enemies have 30% chance to resist Reveal

### Stat Identity Summary

| Stat | Triangle | Beats | Status Effect | Idle Bonus | Visual |
|------|----------|-------|---------------|------------|--------|
| STR | Physical | DEX | Stagger (delay action) | +% base damage | Orange/red, screen shake |
| DEX | Physical | CON | Bleed (DoT, stacks) | +% crit chance | White/silver, speed lines |
| CON | Physical | STR | Weaken (-20% damage dealt) | +% party HP | Brown/iron, earth tones |
| INT | Mental | WIS | Expose (+25% damage taken) | +% XP gain | Blue/cyan, geometric |
| WIS | Mental | CHA | Reveal (strip/block buffs) | +% loot discovery | Green/gold, nature glow |
| CHA | Mental | INT | Confuse (hit own allies) | +% gold earned | Purple/pink, sparkles |

---

## 5. Quest & Exploration Structure

### Exploring (The Ambient State)

Exploring is the default game state. The party auto-battles waves of enemies scaled to the party's **quest level**. During exploration:

- Gold and XP earned per kill.
- Loot drops at base rates (pity timers active).
- **Quest discoveries**: Small chance per exploration wave (`questDiscoveryRate = 0.03`) to discover a Quest. A popup appears: "A quest has been found!" with quest details and an Accept button.
- **Fragment accumulation**: Kills have a base chance (`fragmentDropRate = 0.01`) to drop encounter/location/dungeon fragments. When enough fragments accumulate for a destination, the player receives a notification and can choose to visit.
- Exploring continues indefinitely — it's the heartbeat of the game.

### Quests (The Structured Challenge)

Quests are discovered during exploration. When accepted, the party enters a structured multi-wave challenge within a **Quest Zone**.

**Quest Zone Structure:**

Each Quest Zone contains 10 Quests + 1 Boss Quest, themed around dominant enemy stat types.

| Quest # | Type | Enemies | Reward |
|---------|------|---------|--------|
| 1–3 | Normal | 3–5 waves of 2–4 enemies | Gold + chance at Common/Uncommon loot |
| 4 | Elite | 3 waves + mini-boss | Guaranteed Uncommon+ loot drop |
| 5–7 | Normal | 4–6 waves, scaling HP | Gold + fragment drops increase |
| 8 | Elite | 4 waves + mini-boss | Guaranteed Rare+ loot or Verb drop |
| 9–10 | Hard | 5–7 waves, mixed stat types | Gold + key drops + equipment |
| 11 | **Zone Boss** | 1 boss with phases + typed adds | Guaranteed Rare+ gear + milestone unlock |

**Quest Dialogue**: Quests have a chance of dialogue events — brief narrative moments with NPC text. On quest completion, the reward summary may include a new quest offer ("The merchant mentions a rumor about the Ironwood Forest..."), creating a natural chain into the next quest.

**Milestone Rewards** (from Boss Quests):
- New **locations** unlock (fragment destinations from the key system)
- New **permanent encounter types** (e.g., "Arcane Trial" — all INT enemies, unique loot table)
- New **dungeons** (extended multi-zone challenges with curated loot)
- Equipment blueprints, Verb drops, cosmetic color palettes

### Quest Failure and Retry

There is no hard fail state. If the party wipes or the quest timer exceeds 3 minutes:

- The quest **retreats** — the player keeps 50% of gold earned during the attempt.
- A **retry icon** appears on the edge of the exploration screen (non-intrusive, always accessible).
- The party **resumes exploring** to continue leveling, earning loot, and gaining power.
- Tapping the retry icon re-enters the quest at any time.
- The retry icon persists until the quest is completed or manually dismissed.

### Idle Exploring (Offline)

When the player is away, the party continues exploring at reduced efficiency:

- **Gold**: 50% of active rate (`offlineEfficiency = 0.50`)
- **Fragments**: 25% of active discovery rate (`offlineFragmentRate = 0.25`)
- **Loot**: Offline loot drops accumulate in a claim queue (Common/Uncommon only, capped at 20 items)
- **Cap**: Maximum 8 hours of offline progress (`maxOfflineSeconds = 28,800`)
- **No quest progress offline** — quests require active play

---

## 6. Deterministic Math Model

### Damage Formula

```
finalDamage = baseDamage
    × (1 + charStat × verbStatScaling × 0.1)
    × advantageMultiplier        [0.67, 0.8, 1.0, 1.2, or 1.5]
    × exposeMultiplier           [1.0 or 1.25]
    × equipmentDamageBonus       [1.0 + sum of %dmg gear mods]
    × prestigeMultiplier         [1.0 at launch, stub]
    × globalBoostMultiplier      [1.0 base, 2.0 during ad boost]
```

**Verb stat scaling example** (from verb-stat doc):
```
Earthquake (STR Verb, baseDamage 55, statScaling 1.0)
Used by Brute species (STR = 12):
  actualDamage = 55 × (1 + 12 × 1.0 × 0.1) = 55 × 2.2 = 121

Same Verb used by Mage species (STR = 2):
  actualDamage = 55 × (1 + 2 × 1.0 × 0.1) = 55 × 1.2 = 66
```

### Enemy HP Scaling (Exponential)

```
enemyHP = baseHP × (1 + hpGrowthRate) ^ questLevel

baseHP       = 50
hpGrowthRate = 0.12  (12% per quest level)
```

| Quest Level | Enemy HP | Era |
|-------------|----------|-----|
| 1 | 50 | Tutorial |
| 10 | 155 | Early game |
| 25 | 850 | Mid game, first wall |
| 50 | 14,500 | Late early-game |
| 100 | 4.2M | Mid game, big numbers begin |
| 200 | 35.2B | Late game, notation kicks in |
| 500 | 1.2aa | Deep endgame |

### Gold Earned Per Kill

```
goldPerKill = baseGold
    × (1 + goldGrowthRate) ^ questLevel
    × chaGoldBonus               [1.0 + partyTotalCHA × 0.02]
    × prestigeMultiplier
    × boostMultiplier            [1.0 or 2.0 from ad]

baseGold       = 5
goldGrowthRate = 0.10  (10% per quest level)
```

| Quest Level | Gold/Kill | Gold/Min (exploring) |
|-------------|-----------|---------------------|
| 1 | 5 | ~30 |
| 10 | 16 | ~95 |
| 25 | 85 | ~510 |
| 50 | 1,450 | ~8.7K |
| 100 | 420K | ~2.5M |
| 200 | 3.5B | ~21B |

### Upgrade Cost Scaling

```
upgradeCost = baseCost × (1 + costGrowthRate) ^ currentLevel

baseCost       = 10
costGrowthRate = 0.15  (15%, intentionally faster than gold growth)
```

Gold growth (10%) lags behind cost growth (15%), creating natural pressure: players must push to higher quest levels or optimize party composition to keep upgrading. This is the core "soft wall" that drives engagement without hard-blocking.

### Loot Drop Model (Bounded RNG)

```
dropChance = baseDropRate × (1 + wisDiscoveryBonus) × questLevelModifier

Pity timer: Guaranteed drops after N kills without that rarity.
            Pity counter persists across sessions (saved to disk).
```

| Rarity | Base Drop Rate | Pity Timer |
|--------|---------------|------------|
| Common | 15% per kill | None |
| Uncommon | 4% per kill | 50 kills |
| Rare | 0.8% per kill | 200 kills |
| Epic | 0.15% per kill | 1,000 kills |
| Legendary | 0.02% per kill | 5,000 kills |

Progression is never unfair — pity timers guarantee forward movement regardless of luck.

### Offline Earnings (Exploring)

```
offlineGold = goldPerSecond
    × min(elapsedSeconds, maxOfflineSeconds)
    × offlineEfficiency
    × prestigeMultiplier

goldPerSecond    = goldPerKill × killsPerSecond (at current quest level)
maxOfflineSeconds = 28,800  (8 hours, tunable)
offlineEfficiency = 0.50    (50% of active rate, tunable)
```

Offline also accumulates fragment progress at 25% of active discovery rate.

### Tuning Knobs (20 Variables)

| # | Knob | Default | Controls |
|---|------|---------|----------|
| 1 | `baseHP` | 50 | Starting enemy HP |
| 2 | `hpGrowthRate` | 0.12 | Enemy HP curve steepness |
| 3 | `baseGold` | 5 | Starting gold per kill |
| 4 | `goldGrowthRate` | 0.10 | Gold scaling per quest level |
| 5 | `costGrowthRate` | 0.15 | Upgrade cost curve |
| 6 | `offlineEfficiency` | 0.50 | Offline earning rate |
| 7 | `maxOfflineSeconds` | 28800 | Offline earnings cap |
| 8 | `offlineFragmentRate` | 0.25 | Offline fragment discovery rate |
| 9 | `verbDrawCooldown` | 10 | Seconds before untapped Verb rotates |
| 10 | `verbSlotCount` | 3 | Visible Verb draw slots |
| 11 | `autoAttackDPS` | 0.3 | Base auto-attack as fraction of Verb DPS |
| 12 | `pityUncommon` | 50 | Kills until guaranteed Uncommon |
| 13 | `pityRare` | 200 | Kills until guaranteed Rare |
| 14 | `pityEpic` | 1000 | Kills until guaranteed Epic |
| 15 | `pityLegendary` | 5000 | Kills until guaranteed Legendary |
| 16 | `questRetreatGoldPenalty` | 0.50 | Gold kept on quest retreat |
| 17 | `exploreKillsPerMinute` | 6 | Ambient exploration kill rate |
| 18 | `questDiscoveryRate` | 0.03 | Chance per exploration wave to discover a quest |
| 19 | `fragmentDropRate` | 0.01 | Base fragment drop chance per kill |
| 20 | `prestigeMultiplierBase` | 1.0 | Stub — scales all earnings post-prestige |

All 20 knobs are exposed via Unity Remote Config for live tuning without app updates.

---

## 7. Loot System (First-Class)

### Rarity Tiers

| Rarity | Color | Border | Affix Slots | Stat Roll Range |
|--------|-------|--------|------------|-----------------|
| Common | White/Gray | None | 0 | Base stats only |
| Uncommon | Green | Thin green | 1 | +1–3 to one stat |
| Rare | Blue | Blue glow | 2 | +2–5 to two stats |
| Epic | Purple | Purple pulse | 2–3 | +4–8, can include % bonuses |
| Legendary | Orange | Orange radiance | 3 | +6–12, guaranteed unique effect |

### Affix System

Every equipment piece rolls affixes based on rarity. Affixes are drawn from a deterministic pool per equipment slot (helmets can't roll leg-related mods). Rolls are bounded — worst-case Rare is always better than best-case Common.

**Affix categories:**
- Flat stat bonuses: +N STR/DEX/CON/INT/WIS/CHA
- Percentage bonuses: +N% damage, +N% gold, +N% crit
- Verb bonuses: +N% to specific status effect duration
- Defensive: +N% HP, +N% damage reduction
- Legendary unique effects: "Verbs of this item's stat type cost -1 cooldown tick"

### Deterministic Guardrails

- **Pity timers** — persisted to save file, never reset except on prestige. Guarantee forward movement.
- **Quest cadence guarantees** — Elite quests = Uncommon+ drop. Boss quests = Rare+ drop. Always.
- **Deterministic chest progression** — every 10th chest opened is one rarity tier higher than normal.
- **Fragment destinations** — spending fragments to enter encounters/locations/dungeons guarantees curated loot tables. The player knows what pool they're rolling from before spending.
- **Reroll bounded** — equipment reroll always stays within the rarity's affix range. You can't reroll a Rare into worse-than-Uncommon stats.

### Loot Acquisition Sources

1. **Exploration drops** — ambient kills during exploring have base drop rates with pity timers. Bread-and-butter income. Mostly Common/Uncommon. The constant flow that feeds the paper-doll dopamine.

2. **Quest rewards** — structured rewards per the quest zone cadence. Elites and bosses guarantee quality. Quest completion dialogue may offer a choice between 2–3 reward items, giving the player agency.

3. **Fragment destinations** (encounters/locations/dungeons from the key system, post-MVP) — spend accumulated fragments to enter curated loot pools. Color-themed keys target specific stat categories (Red = STR/DEX Verbs + attack gear, Blue = CHA Verbs + gold gear, etc.). This is where targeted farming happens.

4. **Timed exploration chest** — every 4 hours of real time (active or offline), a chest appears on the exploration screen. Tap to open. Watch rewarded ad for double contents. Contains gold + 1–2 items + fragment progress.

### Collection Goals

**Codex** — A collection log tracking every unique equipment piece, Verb, and species combination the player has owned. Not inventory-dependent — once found, always logged.

- Organized by category: Equipment by slot, Verbs by stat, Species by archetype.
- Completing Codex pages grants permanent stat bonuses:
  - "All Rare+ helmets" → +3% party HP
  - "All STR Verbs" → +5% STR Verb damage
  - "3 Species at Ability Rank 3+" → +2% all stats
- Total Codex completion percentage displayed on profile. Endgame chase for completionists.

**Equipment Sets** — Wearing 2/3/4 pieces from a themed set grants escalating bonuses.

| Set | 2pc Bonus | 3pc Bonus | 4pc Bonus |
|-----|-----------|-----------|-----------|
| Ironwood | +5% CON | +10% party HP | Weaken lasts +1 tick |
| Shadowthread | +5% DEX | +10% crit | Bleed deals +15% per tick |
| Arcane Lattice | +5% INT | +10% XP | Expose duration +1 tick |
| Wildbloom | +5% WIS | +10% heal potency | Reveal also cleanses ally debuffs |
| Gilded Court | +5% CHA | +10% gold | Confuse chance +10% |
| Warbringer | +5% STR | +10% base damage | Stagger duration +1 tick |

Sets encourage targeted farming and create meaningful gear replacement decisions ("this piece is higher rarity, but it breaks my set bonus").

---

## 8. Progression Systems

### Character Progression

Characters gain XP from exploration kills and quest completion.

```
xpPerKill = baseXP × (1 + questLevel × 0.05) × intXPBonus
levelUpCost = baseXPCost × (1 + 0.18) ^ currentLevel
```

**Level-up grants:**
- +1 stat point to distribute (player choice, soft cap at species primary stat +3)
- Verb slot unlock at level thresholds: 2 slots → 3 at lv11 → 4 at lv25 → 5 at lv51
- Every 10 levels: a **Talent pick** — choose 1 of 3 passives tied to the character's primary stat

**Talent examples:**
- STR: "Heavy Hitter" (+10% Stagger duration) vs "Cleaving Force" (single-target Verbs deal 30% splash) vs "Momentum" (+5% damage per consecutive STR Verb drawn)
- WIS: "Deep Roots" (+20% heal potency) vs "Third Eye" (Reveal also Exposes for 1 tick) vs "Sanctuary" (healed allies take -10% damage for 2 ticks)
- DEX: "Razor Edge" (+15% Bleed damage) vs "Evasion" (+10% dodge chance for party) vs "Flurry" (+1 hit count on all DEX Verbs)

Talents are permanent per character but can be respec'd for in-game gold (cost scales with level).

### Species Progression

Species is a creation-time trait. The Species Ability upgrades through passive play:

| Rank | Kill Threshold | Bonus Scaling |
|------|---------------|---------------|
| 1 | 0 (innate) | Base effect |
| 2 | 100 kills | +20% potency |
| 3 | 500 kills | +40% potency |
| 4 | 2,000 kills | +70% potency |
| 5 | 10,000 kills | +100% potency |
| Max | 50,000 kills | +150% potency |

This incentivizes playing with a species long enough to rank up their ability, creating a soft loyalty loop without locking players in.

### Equipment Progression

Four upgrade paths, each consuming different resources:

| Path | Input | Effect | Cap |
|------|-------|--------|-----|
| **Enhance** | Gold | +1 level, linear stat growth per enhance | Level 20 per rarity tier |
| **Reroll** | Reroll Stones (exploration drop) | Re-randomize affixes within rarity bounds | Unlimited |
| **Ascend** | 3 same-rarity items + Ascension Shards | Upgrade rarity tier (e.g., Uncommon → Rare) | Up to Legendary |
| **Merge** | 2 identical items | Combine into one with boosted base stats | Once per item |

- **Enhance** is the daily gold sink — cheap, incremental, always available.
- **Reroll** is the chase for perfect affixes — bounded, never worse than rarity floor.
- **Ascend** is the long-term rarity push — expensive, transforms good items into great ones.
- **Merge** prevents duplicate loot from feeling like waste — turns "another common sword" into value.

### Prestige System (Designed, Built Post-MVP)

**Currency**: Stellar Ink

**Unlock**: Available once the player reaches Quest Level 100 (first natural wall).

**Cost to Prestige**: Prestige is not free. It requires spending significant accumulated resources:
- Gold cost: 50% of total gold ever earned (scales with how much you've played)
- Material cost: 10 Ascension Shards + 50 Reroll Stones (significant resource investment)
- This ensures prestige is a deliberate, weighty decision — not something you do casually

**What resets:**
- Character levels (back to 1)
- Gold (spent on prestige cost, remainder zeroed)
- Quest progress (back to Zone 1)
- Equipment enhance levels (back to +0)
- Talent selections (must re-pick, but same options available)
- Exploration zone (back to starting area)
- Consumable resources (Reroll Stones, Ascension Shards)

**What persists:**
- All equipment (un-enhanced, but rarity and affixes intact)
- All Verbs owned
- All characters and their species (identity preserved)
- Species ability ranks
- Codex progress and completion bonuses
- Equipment set bonuses (if pieces still equipped)
- Fragment destinations discovered
- Account-level stats and achievements

**Stellar Ink earned:**
```
stellarInkEarned = floor((highestQuestLevel / 10) ^ 1.5)
```

| Quest Level at Reset | Stellar Ink Earned |
|---------------------|-------------------|
| 100 | 31 |
| 150 | 58 |
| 200 | 89 |
| 300 | 164 |
| 500 | 353 |

**Stellar Ink purchases** (prestige skill tree):

| Upgrade | Cost | Type | Effect |
|---------|------|------|--------|
| Ink of Power | 10 | Repeatable (diminishing: 10%, 9%, 8%...) | +% all damage |
| Ink of Fortune | 15 | Repeatable | +% gold earned |
| Ink of Discovery | 20 | Repeatable | +% fragment discovery rate |
| Ink of Wisdom | 20 | Repeatable | +% XP gain |
| Ink of Resilience | 15 | Repeatable | +% offline efficiency |
| Verb Mastery | 50 | One-time | +1 starting Verb slot |
| Auto-Scribe | 200 | One-time | Unlock permanent Auto-Fire Verbs |
| Stellar Attunement | 100 | One-time | Prestige cost reduced by 25% |

**Economy stub**: `prestigeMultiplier` defaults to 1.0 in all formulas from day one. Every Stellar Ink purchase modifies this value. When prestige ships, nothing in the core math needs refactoring — just flip the multiplier from 1.0 to active.

---

## 9. UX/UI Flow (Mobile-First)

### Screen List and Navigation

```
[Bottom Nav Bar — always visible, 5 tabs]
  ⚔️ Explore  |  📜 Quests  |  🎒 Loot  |  👥 Party  |  🏪 Shop
```

### Explore (Home/Combat Screen)

The default screen. Always active.

```
┌─────────────────────────────────────────┐
│ 💰 1.2M Gold    ⭐ Lv 34    🧩 ▓▓▓░ 7/12│  ← Top bar: gold, level, fragment progress
├─────────────────────────────────────────┤
│                                         │
│  👤👤👤👤        👹👹👹                │  ← Party (left) vs Enemies (right)
│  (paper dolls)   (enemy sprites)        │  ← Full paper doll rendering, all 4 members
│                                         │
│  ─── Wave 3/5 ─── Quest Lv 34 ───      │  ← Wave indicator
│                                         │
│  [🔄 Quest Retry]                       │  ← Floating retry icon (if retreated)
│                                         │
├─────────────────────────────────────────┤
│  [🟠 Bash]  [🔵 Analyze]  [⚪ Slash]   │  ← Verb draw slots (3-4 cards)
│   4s left     7s left      2s left      │  ← Rotation timer per card
├─────────────────────────────────────────┤
│  ⚔️  |  📜  |  🎒  |  👥  |  🏪       │  ← Bottom nav
└─────────────────────────────────────────┘
```

- Active boosts displayed as small icons under the top bar with remaining time
- Timed chest appears as a floating icon when ready (jiggles)
- Damage numbers float briefly, capped at 5 visible, stack into combo display for rapid hits
- Advantage indicators: green up-arrow / red down-arrow on enemies when a Verb is drawn that has advantage/disadvantage

### Quests Screen

```
┌─────────────────────────────────────────┐
│  📜 QUESTS                              │
├─────────────────────────────────────────┤
│  ACTIVE QUEST: Ironwood Forest 4/11     │
│  [████████░░] Wave 3/5 — Elite Quest    │
│  Dominant: CON/STR    Reward: Rare+ ⚔️  │
│  [▶ CONTINUE]                           │
├─────────────────────────────────────────┤
│  DISCOVERED QUESTS:                     │
│  ┌─────────────────────────────────┐    │
│  │ Crystal Caverns — Zone Lv 38   │    │
│  │ "A merchant speaks of crystals" │    │
│  │ [ACCEPT]                        │    │
│  └─────────────────────────────────┘    │
│  ┌─────────────────────────────────┐    │
│  │ Ember Pass — Zone Lv 41        │    │
│  │ "Smoke rises from the east"    │    │
│  │ [ACCEPT]                        │    │
│  └─────────────────────────────────┘    │
├─────────────────────────────────────────┤
│  MILESTONES:                            │
│  ✅ Sunken Vault — Location unlocked    │
│  ✅ Arcane Trial — Encounter unlocked   │
│  🔒 Void Rift — Complete Zone 6 Boss    │
└─────────────────────────────────────────┘
```

### Loot Screen

```
┌─────────────────────────────────────────┐
│  🎒 LOOT         [Codex 34%] [Sets]    │
├─────────────────────────────────────────┤
│  RECENT:  ✨ ✨ ✨                      │  ← New items glow
│  [🟣 Arcane Helm +5] [🔵 Iron Blade]   │
│  [🟢 Leaf Guard]     [⚪ Cloth Boots]   │
├─────────────────────────────────────────┤
│  FILTER: [All] [Head] [Torso] [Weapon]  │
│          [Arms] [Legs] [Feet] [Misc]    │
│          [Verbs]                         │
├─────────────────────────────────────────┤
│  Item Detail (on tap):                  │
│  ┌─────────────────────────────────┐    │
│  │ 🟣 Arcane Helm of Insight      │    │
│  │ Epic — Head Slot                │    │
│  │ +6 INT, +4 WIS, +3% XP         │    │
│  │ Set: Arcane Lattice (2/4)       │    │
│  │ ────────────────────────────    │    │
│  │ [EQUIP] [ENHANCE] [REROLL]     │    │
│  │ [ASCEND] [MERGE]               │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

### Party Screen

```
┌─────────────────────────────────────────┐
│  👥 PARTY           Coverage: ████░░    │
├─────────────────────────────────────────┤
│  [Char1]  [Char2]  [Char3]  [Char4]    │  ← Tap to select, full paper dolls
│  STR 12   DEX 11   INT 10   WIS 12     │  ← Primary stat shown
│  Brute    Rogue    Mage     Sage        │  ← Archetype label
├─────────────────────────────────────────┤
│  SELECTED: Char1 — "Thorgrim" Lv 22    │
│  Species: Dwarf  Ability: Stone Blood ★3│
│                                         │
│  📊 Stats: STR 12+3 DEX 3 CON 8+1 ...  │
│                                         │
│  ⚔️ Equipment:                          │
│  [🟣 Helm] [🔵 Plate] [⚪ Gauntlet]    │
│  [🟢 Greave] [⚪ Boot] [🔵 Axe] ...    │
│                                         │
│  📜 Verbs (3/3 slots):                  │
│  [🟠 Earthquake] [🟠 Titan Crush]      │
│  [🟤 Iron Wall]                         │
│  [+ Add Verb]                           │
│                                         │
│  🌟 Talents: Heavy Hitter, Momentum    │
│  [LEVEL UP available!]                  │
└─────────────────────────────────────────┘
```

### Shop Screen

```
┌─────────────────────────────────────────┐
│  🏪 SHOP                               │
├─────────────────────────────────────────┤
│  BOOSTS (in-game currency):             │
│  [Auto-Fire Verbs — 500g×Lv — 5 min]   │
│  [Verb Speed-Up — 300g×Lv — 5 min]     │
│  [2x Gold — Watch Ad — 30 min]         │
├─────────────────────────────────────────┤
│  TIMED CHEST:                           │
│  [🎁 Open Chest]  [📺 2x — Watch Ad]  │
│  Next chest in: 2h 14m                  │
├─────────────────────────────────────────┤
│  PREMIUM:                               │
│  [Remove Ads — $4.99]                   │
│  [Starter Pack — $2.99 — 71h left!]    │
│  [Gem Pouch 300💎 — $1.99]             │
│  [Gem Chest 800💎 — $4.99]             │
│  [VIP Pass — $5.99/mo]                 │
├─────────────────────────────────────────┤
│  ⚙️ Settings | ☁️ Cloud Save | 🔊 Audio │
└─────────────────────────────────────────┘
```

### Additional Screens

- **Fragment Destinations** (sub-screen of Quests): Shows available encounters/locations/dungeons with fragment progress bars. Post-MVP integration with key system.
- **Prestige** (sub-screen of Party, post-MVP): Stellar Ink balance, prestige skill tree, cost to prestige, what resets/persists summary.

### Live vs. Claim Updates

| Live (always updating) | On Claim/Action |
|------------------------|-----------------|
| Gold counter | Loot drops (popup) |
| XP bar | Quest rewards (summary screen) |
| Enemy HP bars | Offline earnings (claim screen on app open) |
| Verb cooldowns and rotation timers | Timed chest (tap to open) |
| Auto-attack damage numbers | Level-up (popup with choices) |
| Exploration wave counter | Quest discovery (accept/dismiss popup) |
| Fragment progress bar | Fragment destination ready (notification) |
| Active boost timers | |

### Avoiding UI Spam

- Damage numbers: float briefly, cap at 5 visible, stack into combo display for rapid multi-hits
- Common loot: auto-collect with subtle +1 indicator, no popup
- Uncommon loot: brief popup with item art and rarity glow, auto-dismiss after 2s or tap to inspect
- Rare loot: popup with particle effect, stays until dismissed
- Epic+ loot: full-screen reveal moment with particle effects and sound
- Gold counter: smooth number rolling animation, never jumps
- Verb rotation: subtle card-flip, not jarring

### Idle-Specific Affordances

- **Return-to-game claim screen**: Shows offline gold earned, fragments found, items dropped. Big "Claim" button. "Watch Ad for 2x" beside it.
- **Timed chest**: Visible countdown timer on Shop tab badge. Chest jiggles when ready.
- **Boost buttons**: Clearly priced in gold. Duration shown. Active boosts as icons under top bar.
- **Quest retry**: Non-intrusive floating icon on Explore screen edge. Always accessible, never interrupts.

---

## 10. Ads + IAP Placement Plan

### Rewarded Ad Placements (Primary Revenue)

| Placement | Trigger | Reward | Frequency Cap |
|-----------|---------|--------|---------------|
| 2x Offline Earnings | Return-to-game claim screen | Double gold + fragments from offline | Once per return |
| Timed Chest Double | Opening timed exploration chest | Double chest contents | Every 4 hours (per chest) |
| Bonus Verb Draw | After quest completion | Draw 1 bonus Verb from rarity-boosted pool | Once per quest |
| Fragment Boost | Exploration screen boost button | 2x fragment discovery rate for 30 min | 3x per day |
| Reroll Assist | Equipment reroll screen | Free reroll (saves a Reroll Stone) | 5x per day |
| Quest Retry Buff | Quest retry icon (after retreat) | +20% party damage for the retry attempt | Once per retreat |

Design principle: Every rewarded ad gives a concrete, visible benefit. The player should think "that was worth 30 seconds."

### Interstitial Placements (Natural Breaks Only)

| Placement | Trigger |
|-----------|---------|
| Quest complete → return to exploring | After reward summary dismissed |
| Prestige reset (post-MVP) | After Stellar Ink transaction |
| Zone Boss defeated | After milestone reward screen |

**Cap**: Maximum 1 interstitial per 5 minutes. Never during active gameplay or mid-quest.

### Banner Placements (Passive Screens Only)

| Screen | Position |
|--------|----------|
| Shop screen | Bottom |
| Codex/Collection screen | Bottom |
| Settings | Bottom |

**Never on**: Explore, Quest, Party, or Loot equip screens. Core gameplay screens are ad-free.

### "Remove Ads" Behavior

- **Removes**: All interstitials, all banners.
- **Does NOT remove**: Rewarded ads (remain available as optional, player-initiated).
- **Bonus**: +10% permanent gold bonus as a thank-you for purchasing.
- **Price**: $4.99 (non-consumable, one-time).

### IAP Offer Catalog

| Item | Type | Price | Contents |
|------|------|-------|----------|
| Remove Ads | Non-consumable | $4.99 | Remove interstitials + banners, +10% gold |
| Starter Pack | Non-consumable (one-time) | $2.99 | 500 gems + 3 Rare equipment + 10 Reroll Stones. First 72 hours only. |
| Gem Pouch | Consumable | $1.99 | 300 gems |
| Gem Chest | Consumable | $4.99 | 800 gems |
| Gem Vault | Consumable | $9.99 | 1,800 gems (best value) |
| VIP Pass | Subscription (monthly) | $5.99/mo | 2x offline earnings (while active), +1 Verb draw slot, daily Rare+ chest, exclusive cosmetic color palettes |

### Retention-Friendly Cadence

- Free players never hit a wall that only money solves. Pity timers, exploration, and quest rewards ensure steady progression.
- Gems earnable in-game (daily login, milestones, Codex completion) at ~50/day. Paying accelerates, never gates.
- No energy system. Play as much as you want.
- Ads are optional boosts, never required to progress.

---

## 11. Data-Driven Content Structure (Unity-Friendly)

### ScriptableObject Asset Types

| SO Type | Purpose | Launch Count |
|---------|---------|-------------|
| `SpeciesDefinition` | Name, stat distribution (30pts), species ability ref, body part IDs, color palettes, scale, item restrictions | 8–12 |
| `SpeciesAbilityDefinition` | Ability name, description, 5 rank thresholds, effect per rank, icon | 1 per species |
| `CharacterTemplate` | Default species, starting Verbs, starting equipment, tutorial flags | 4–6 starters |
| `EquipmentDefinition` | Item type, slot, variant count, layer codes, hidden layers, color variance, base stat mods, rarity, affix pool ref | 80–120 |
| `AffixDefinition` | Stat modified, value range per rarity, slot restrictions, display string | 20–30 |
| `EquipmentSetDefinition` | Set name, piece list, 2pc/3pc/4pc bonus definitions | 6–10 sets |
| `VerbDefinition` | Per verb-stat doc: ID, stat type, damage, hits, target mode, status effect, proc chance, cooldown, rarity, scaling | 36+ (6/stat min) |
| `StatusEffectDefinition` | Effect type, duration, potency, stacking rules, interaction overrides, VFX ref | 6 (1 per stat) |
| `EnemyDefinition` | Stat type, HP multiplier, damage multiplier, Verb loadout, visual ref | 30–40 |
| `QuestZoneDefinition` | Zone name, dominant enemy types, 10 quest configs + boss, loot table refs, dialogue pool, background art | 10–15 zones |
| `QuestDefinition` | Zone ref, wave configs, reward table, dialogue triggers, milestone unlocks | 1 per quest |
| `LootTableDefinition` | Weighted entry list (item ref, rarity, weight, min/max qty), key tier filter | 20–30 tables |
| `FragmentDestinationDefinition` | Type (encounter/location/dungeon), fragments required, window duration, curated loot table, enemy types, biome art | 6–10 |
| `TalentDefinition` | Stat type, level tier, 3 options with descriptions and effect formulas | 6 stats × multiple tiers |
| `DialoguePool` | Quest dialogue lines, NPC names, acceptance/completion text | Per zone |
| `EconomyConfig` | All 20 tuning knobs, upgrade cost curves, offline caps | 1 singleton |
| `PrestigeConfig` | Stellar Ink costs, reset rules, prestige skill tree definitions | 1 singleton |
| `AdvantageMatrix` | Complete 6×6 matchup table: damage multipliers + status proc modifiers | 1 singleton |

### Static vs. Remotely Configurable

| Static (Baked Into Build) | Remote Config (Tune Live) |
|--------------------------|--------------------------|
| Species definitions, body part art | All 20 economy tuning knobs |
| Verb definitions, status effect rules | Drop rates, pity timer thresholds |
| Equipment art, layer codes, set definitions | Upgrade cost curves |
| Quest zone structure, dialogue text | Offline efficiency + caps |
| Advantage matrix base values | Fragment discovery rates |
| UI layouts, screen flow | Ad frequency caps |
| | Seasonal event multipliers |
| | Quest discovery rate |
| | Verb draw cooldown (10s default) |
| | Boost costs and durations |
| | Prestige cost scaling |

Remote Config allows fixing economy mistakes and A/B testing balance across player segments without shipping updates.

---

## 12. MVP Build Plan

### Sprint 1 (2 Weeks): Core Loop Proof

**Goal**: Player can explore, fight waves, draw and tap Verbs, earn gold, equip loot on paper-doll characters.

**Deliverables**:
- Exploration screen with auto-attacking party (4 characters, full paper-doll rendering)
- Verb draw system (3 slots, random draw from pool, 10s rotation, tap to fire, no auto-fire)
- Dual-triangle advantage system (damage multipliers, green/red arrow indicators)
- 2 species with stat distributions and 1 species ability each
- Enemy waves with stat typing (3–4 enemy types)
- Gold per kill + smooth gold counter
- Equipment drops (Common/Uncommon, 3 slots: weapon, torso, head)
- Equip item → paper doll updates immediately
- Quest discovery from exploration (1 zone, 5 quests + boss)
- Quest retreat + retry icon on exploration screen
- Character level-up with stat point allocation
- Offline exploring earnings (gold only, claim on return)
- Save/load (local JSON)
- Basic UI: explore screen, party screen, loot screen (minimal)
- `prestigeMultiplier` stubbed at 1.0 in all formulas

### Sprint 2 (2 Weeks): Loot Depth + Monetization Skeleton

**Goal**: Loot feels meaningful, economy has tension, ads are integrated, retention hooks exist.

**Deliverables**:
- Full rarity tiers (Common through Legendary) with affix system
- All 11 equipment slots active
- Equipment enhance + reroll + ascend + merge
- Pity timers on all rarity tiers (persisted)
- Equipment sets (2–3 sets) with set bonuses
- Codex collection log with completion rewards
- All 6 status effects from verb-stat doc
- Verb performance scaling (stat thresholds: strong/moderate/weak/dump)
- Timed exploration chest (4-hour cycle)
- Rewarded ads: 2x offline, chest double, fragment boost
- Interstitials at quest completion
- Remove Ads IAP
- 4+ species with abilities
- Talent picks (every 10 levels, 3 choices per stat)
- Fragment drop system (accumulate during exploration)
- Quest dialogue + acceptance flow
- Party coverage indicator
- Purchasable boosts (auto-fire Verbs, Verb speed-up)
- Cloud save via Unity Authentication + Cloud Save
- Unity Analytics integration (key events)
- Remote Config for all 20 tuning knobs

### Later (Sprint 3+): Depth Systems

- **Dungeon Key System**: Full key/encounter/location/dungeon system from `dungeon-key-system-design-doc.md`. Fragment destinations become playable. Color-themed keys, encounter mode, location discovery with timed windows, key combining.
- **Prestige**: Stellar Ink currency, prestige cost structure (resource-heavy), reset logic, prestige skill tree. Flip `prestigeMultiplier` from 1.0 to active.
- **Additional species** (8–12 total) with unique abilities
- **Additional quest zones** (10–15 total) with zone-specific dialogue
- **VIP subscription** IAP
- **Seasonal events** (via Remote Config: limited-time quest zones with unique loot)
- **Social features** (leaderboards, guild exploration)
- **Analytics-driven tuning** (adjust Remote Config knobs based on real player data)

---

## Deprecated Systems (Document-Only)

The following Starquill systems are **not part of this design iteration**. They are documented for future reference but must be rebuilt in the idle-clicker style for Unity:

| System | Status | Notes |
|--------|--------|-------|
| Inventory (Godot) | Deprecated | Replaced by equipment slots + Codex collection log. No free-form inventory needed. |
| Status Effects (Godot) | Deprecated | Rebuilt as Verb-driven temporary multipliers within the combat tick system. See verb-stat doc. |
| Combat Resolver (Godot) | Deprecated | Rebuilt as deterministic tick-based auto-combat with Verb draw mechanic. |
| Factories (Godot) | Deprecated | Rebuilt as ScriptableObject-driven data loading in Unity. |
| All Godot engine files | Archived | Moved to separate archive folder. Unity is the target engine. |

---

## Reference Documents

- `verb-stat-system-design-doc.md` — Complete Verb, stat, dual-triangle, and status effect reference
- `dungeon-key-system-design-doc.md` — Key system, encounter mode, location discovery (Sprint 3+)
- `unity-idle-clicker-project-guide.md` — Unity packages, services, asset store, architecture patterns
