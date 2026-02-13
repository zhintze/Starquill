# Verb & Stat System Design Document
## Dual-Triangle Combat with Pooled Party Verbs

---

## System Summary

Every character (species) in the game has stats drawn from the classic six: STR, DEX, CON, INT, WIS, CHA. These stats aren't just numbers — they function as **elemental damage types**. Each stat produces **Verbs** (combat actions) that carry that stat's type, and the type determines advantage/disadvantage against enemies.

A party of 3–4 characters pools all their Verbs together into a shared **Verb Pool**. The game auto-resolves combat using the pool, selecting the best available Verb for each enemy. The player's job is party composition and Verb loadout — not per-action micromanagement.

---

## Part 1: The Dual Triangle

### Physical Triangle

```
        STR (Force)
       / ↑         \
  beats              loses to
     ↓                 ↑
DEX (Finesse) ←beats← CON (Endurance)

STR beats DEX  — Raw power overwhelms agility
DEX beats CON  — Precision finds gaps in toughness
CON beats STR  — Absorbs brute force, outlasts it
```

### Mental Triangle

```
        INT (Arcana)
       / ↑          \
  beats               loses to
     ↓                  ↑
WIS (Instinct) ←beats← CHA (Influence)

INT beats WIS  — Calculated strategy outsmarts gut feeling
WIS beats CHA  — Sees through manipulation and deception
CHA beats INT  — Charm disrupts cold logic
```

### Cross-Triangle Rules

Physical and Mental don't directly counter each other. Instead, they interact asymmetrically:

| Attacker | Target | Damage Modifier | Status Effect |
|----------|--------|----------------|---------------|
| Physical → Physical | Use triangle | Normal (1.0×, 1.5×, or 0.67×) | Normal proc chance |
| Mental → Mental | Use triangle | Normal (1.0×, 1.5×, or 0.67×) | Normal proc chance |
| Physical → Mental | Always | 1.2× damage | Status effects **cannot** proc |
| Mental → Physical | Always | 0.8× damage | Status effects **always** proc |

**What this means in practice:**
- Physical Verbs hit harder across the board but are "dumb" — they can't apply debuffs to mental enemies
- Mental Verbs hit softer against physical enemies but their debuffs always land
- A pure physical party shreds mental enemies with raw DPS but has no control tools
- A pure mental party can lock down physical enemies with debuffs but kills slowly
- A mixed party has both burst damage and control — the intended optimal composition

### Complete Advantage Matrix

Quick reference for all type matchups. Damage multiplier shown, status proc in parentheses.

| Attacker ↓ / Defender → | STR | DEX | CON | INT | WIS | CHA |
|--------------------------|-----|-----|-----|-----|-----|-----|
| **STR** | 1.0× (50%) | **1.5×** (50%) | 0.67× (50%) | 1.2× (0%) | 1.2× (0%) | 1.2× (0%) |
| **DEX** | 0.67× (50%) | 1.0× (50%) | **1.5×** (50%) | 1.2× (0%) | 1.2× (0%) | 1.2× (0%) |
| **CON** | **1.5×** (50%) | 0.67× (50%) | 1.0× (50%) | 1.2× (0%) | 1.2× (0%) | 1.2× (0%) |
| **INT** | 0.8× (100%) | 0.8× (100%) | 0.8× (100%) | 1.0× (50%) | **1.5×** (50%) | 0.67× (50%) |
| **WIS** | 0.8× (100%) | 0.8× (100%) | 0.8× (100%) | 0.67× (50%) | 1.0× (50%) | **1.5×** (50%) |
| **CHA** | 0.8× (100%) | 0.8× (100%) | 0.8× (100%) | **1.5×** (50%) | 0.67× (50%) | 1.0× (50%) |

Reading this table: STR attacking DEX = 1.5× damage with 50% chance to proc status effect. CHA attacking STR = 0.8× damage with 100% status proc chance.

---

## Part 2: Stats in Detail

Each stat has a primary role, a Verb flavor, a signature status effect, and a visual language.

### STR — Force

| Property | Value |
|----------|-------|
| Triangle | Physical |
| Beats | DEX |
| Loses to | CON |
| Verb Flavor | Smash, Crush, Slam, Cleave, Charge |
| Status Effect | **Stagger** — Target's next action is delayed by 1 tick |
| Damage Profile | High burst, single target |
| Visual | Orange/red impacts, screen shake, dust clouds |
| Idle Bonus | +% base tap damage |

**Design identity:** The straightforward power stat. STR Verbs hit the hardest per action but don't scale as well against groups. Stagger is simple but effective — it slows enemies down, giving the party more time.

### DEX — Finesse

| Property | Value |
|----------|-------|
| Triangle | Physical |
| Beats | CON |
| Loses to | STR |
| Verb Flavor | Slash, Pierce, Dodge, Ricochet, Flurry |
| Status Effect | **Bleed** — Deals 5% of initial hit damage per tick for 3 ticks |
| Damage Profile | Lower per-hit, multi-hit, AoE potential |
| Visual | White/silver streaks, afterimages, speed lines |
| Idle Bonus | +% critical hit chance |

**Design identity:** The fast, multi-hit stat. DEX Verbs hit multiple times or multiple targets. Individually weaker than STR hits, but Bleed stacking on multiple enemies adds up. Excels against CON's high HP pools by percentage-based damage.

### CON — Endurance

| Property | Value |
|----------|-------|
| Triangle | Physical |
| Beats | STR |
| Loses to | DEX |
| Verb Flavor | Block, Retaliate, Brace, Fortify, Endure |
| Status Effect | **Weaken** — Target deals 20% less damage for 2 ticks |
| Damage Profile | Low damage, defensive/counter-attack, self-healing |
| Visual | Brown/iron shields, stone particles, earth tones |
| Idle Bonus | +% HP for all party members |

**Design identity:** The tank stat. CON Verbs don't do much damage directly — they reduce incoming damage and counter-attack. Weaken is a team-survival tool. CON characters keep the party alive while others deal damage. Against STR enemies, their damage reduction turns the enemy's best asset (burst damage) into a non-threat.

### INT — Arcana

| Property | Value |
|----------|-------|
| Triangle | Mental |
| Beats | WIS |
| Loses to | CHA |
| Verb Flavor | Analyze, Shatter, Decode, Calculate, Exploit |
| Status Effect | **Expose** — Target takes 25% more damage from all sources for 2 ticks |
| Damage Profile | Moderate damage, amplifies party damage |
| Visual | Blue/cyan geometric patterns, data glyphs, crystalline fractures |
| Idle Bonus | +% XP gain |

**Design identity:** The force multiplier. INT Verbs don't deal the most damage themselves, but Expose makes everything else hit harder. An INT character in a party with STR hitters is devastating — the INT exposes the target, then the STR character's 1.5× advantage hit is amplified by another 25%. INT is the "smart" stat that rewards team synergy.

### WIS — Instinct

| Property | Value |
|----------|-------|
| Triangle | Mental |
| Beats | CHA |
| Loses to | INT |
| Verb Flavor | Sense, Predict, Heal, Purify, Foresee |
| Status Effect | **Reveal** — Removes target's buffs and prevents buff application for 2 ticks |
| Damage Profile | Low direct damage, healing, cleansing, anti-buff |
| Visual | Green/gold nature glow, leaf particles, soft light |
| Idle Bonus | +% loot discovery rate (key drops and fragments) |

**Design identity:** The support/anti-buff stat. WIS Verbs heal allies and strip enemy buffs. Reveal is the hard counter to enemies that self-buff (especially CHA enemies who buff their allies). WIS is the "I don't need to kill you if I can undo everything you're doing" stat. Low damage output means WIS-heavy parties are slow but incredibly stable.

### CHA — Influence

| Property | Value |
|----------|-------|
| Triangle | Mental |
| Beats | INT |
| Loses to | WIS |
| Verb Flavor | Taunt, Inspire, Deceive, Rally, Seduce |
| Status Effect | **Confuse** — Target has 30% chance to hit an ally instead of your party for 2 ticks |
| Damage Profile | Low-moderate direct damage, buffs allies, debuffs through misdirection |
| Visual | Purple/pink swirls, hearts, sparkle effects, speech bubbles |
| Idle Bonus | +% gold earned from all sources |

**Design identity:** The trickster/buffer. CHA Verbs are about manipulation — turning enemies against each other and buffing your own party. Confuse is chaotic and fun to watch (enemy hits their own ally). CHA counters INT because INT's careful calculations fall apart when the battlefield is in chaos. Against WIS, though, the tricks are seen through immediately.

---

## Part 3: Verbs

### What is a Verb?

A Verb is a combat action. It's the atom of the combat system — the smallest unit that does something. Every Verb has:

```csharp
[CreateAssetMenu(menuName = "IdleGame/Verb Definition")]
public class VerbDefinition : ScriptableObject
{
    [Header("Identity")]
    public string verbId;
    public string displayName;          // "Crushing Blow", "Mind Shatter", etc.
    public string description;
    public Sprite icon;

    [Header("Stat Typing")]
    public StatType statType;           // STR, DEX, CON, INT, WIS, CHA
    public VerbCategory category;       // Physical or Mental (derived from statType)

    [Header("Damage")]
    public float baseDamage;            // before multipliers
    public int hitCount;                // 1 for single hit, 2-5 for multi-hit
    public TargetMode targetMode;       // Single, Cleave (2-3), AoE (all)

    [Header("Status Effect")]
    public StatusEffect effect;         // Stagger, Bleed, Weaken, Expose, Reveal, Confuse
    public float effectProcChance;      // base 50%, modified by advantage rules
    public int effectDuration;          // in ticks
    public float effectPotency;         // strength of the effect

    [Header("Scaling")]
    public float statScaling;           // how much the parent stat multiplies damage
    public int levelRequirement;        // minimum character level to unlock
    public Rarity rarity;               // Common through Legendary

    [Header("Cooldown")]
    public float cooldownTicks;         // ticks before this verb can be used again
    public int verbPoolPriority;        // auto-select priority (lower = used first)
}
```

### Verb Rarity and Power

Verbs come in the same rarity tiers as loot. Higher rarity = better base damage, additional hits, or stronger status effects.

| Rarity | Base Damage Range | Max Hit Count | Effect Potency Bonus | Typical Source |
|--------|------------------|--------------|---------------------|---------------|
| Common | 10–25 | 1 | +0% | Starting species, common drops |
| Uncommon | 20–45 | 1–2 | +10% | Early encounters, common locations |
| Rare | 40–80 | 1–3 | +25% | Rare encounters, location bosses |
| Epic | 70–140 | 2–4 | +50% | Epic keys, epic locations |
| Legendary | 120–250 | 3–5 | +100% | Gold keys, legendary locations |

### Example Verbs by Stat

**STR Verbs:**

| Name | Rarity | Damage | Hits | Target | Effect | Cooldown |
|------|--------|--------|------|--------|--------|----------|
| Bash | Common | 18 | 1 | Single | Stagger (1 tick) | 1 |
| Overhead Slam | Uncommon | 35 | 1 | Single | Stagger (1 tick) | 2 |
| Earthquake | Rare | 55 | 1 | AoE | Stagger (1 tick) | 4 |
| Titan Crush | Epic | 110 | 2 | Cleave | Stagger (2 ticks) | 5 |
| World Breaker | Legendary | 200 | 3 | AoE | Stagger (3 ticks) | 8 |

**DEX Verbs:**

| Name | Rarity | Damage | Hits | Target | Effect | Cooldown |
|------|--------|--------|------|--------|--------|----------|
| Quick Slash | Common | 8 | 2 | Single | Bleed (3 ticks) | 1 |
| Fan of Blades | Uncommon | 12 | 3 | AoE | Bleed (3 ticks) | 3 |
| Vital Strike | Rare | 60 | 1 | Single | Bleed (5 ticks) | 3 |
| Shadow Flurry | Epic | 25 | 5 | Cleave | Bleed (4 ticks) | 5 |
| Thousand Cuts | Legendary | 40 | 5 | AoE | Bleed (5 ticks) | 7 |

**INT Verbs:**

| Name | Rarity | Damage | Hits | Target | Effect | Cooldown |
|------|--------|--------|------|--------|--------|----------|
| Analyze | Common | 15 | 1 | Single | Expose (2 ticks) | 2 |
| Logic Bomb | Uncommon | 30 | 1 | Cleave | Expose (2 ticks) | 3 |
| System Crack | Rare | 50 | 2 | Single | Expose (3 ticks) | 4 |
| Mind Shatter | Epic | 80 | 2 | AoE | Expose (3 ticks) | 6 |
| Absolute Zero | Legendary | 150 | 3 | AoE | Expose (4 ticks) | 8 |

**CHA Verbs:**

| Name | Rarity | Damage | Hits | Target | Effect | Cooldown |
|------|--------|--------|------|--------|--------|----------|
| Taunt | Common | 10 | 1 | Single | Confuse (2 ticks) | 2 |
| Silver Tongue | Uncommon | 22 | 1 | Cleave | Confuse (2 ticks) | 3 |
| Mass Hysteria | Rare | 40 | 1 | AoE | Confuse (3 ticks) | 5 |
| Puppet Master | Epic | 70 | 2 | AoE | Confuse (3 ticks) | 6 |
| Absolute Charm | Legendary | 130 | 3 | AoE | Confuse (4 ticks) | 8 |

**CON Verbs:**

| Name | Rarity | Damage | Hits | Target | Effect | Cooldown |
|------|--------|--------|------|--------|--------|----------|
| Retaliate | Common | 12 | 1 | Single | Weaken (2 ticks) | 1 |
| Iron Wall | Uncommon | 20 | 1 | Single | Weaken (2 ticks) | 2 |
| Tectonic Slam | Rare | 45 | 1 | AoE | Weaken (3 ticks) | 4 |
| Unbreakable | Epic | 60 | 2 | Cleave | Weaken (3 ticks) | 5 |
| Mountain's Wrath | Legendary | 100 | 3 | AoE | Weaken (4 ticks) | 7 |

**WIS Verbs:**

| Name | Rarity | Damage | Hits | Target | Effect | Cooldown |
|------|--------|--------|------|--------|--------|----------|
| Purify | Common | 8 | 1 | Single | Reveal (2 ticks) | 2 |
| Nature's Insight | Uncommon | 18 | 1 | Cleave | Reveal (2 ticks) | 3 |
| True Sight | Rare | 35 | 1 | AoE | Reveal (3 ticks) | 4 |
| Ancestral Vision | Epic | 55 | 2 | AoE | Reveal (3 ticks) | 6 |
| Omniscience | Legendary | 90 | 3 | AoE | Reveal (4 ticks) | 8 |

WIS also has **healing Verbs** that target allies instead of enemies:

| Name | Rarity | Heal Amount | Target | Bonus | Cooldown |
|------|--------|-------------|--------|-------|----------|
| Mend | Common | 20 HP | Single ally | — | 2 |
| Rejuvenate | Uncommon | 15 HP/tick | Single ally | 3 tick HoT | 4 |
| Restoration | Rare | 40 HP | All allies | — | 5 |
| Life Surge | Epic | 80 HP | All allies | Cleanses debuffs | 7 |

---

## Part 4: The Verb Pool

### How Pooling Works

Each character in the party contributes their equipped Verbs to a shared **Verb Pool**. The pool is the party's combined arsenal.

```
Party:
  Character A (STR-focused): [Bash, Overhead Slam, Earthquake]
  Character B (DEX-focused): [Quick Slash, Fan of Blades, Vital Strike]
  Character C (INT-focused): [Analyze, Logic Bomb, System Crack]
  Character D (WIS-focused): [Purify, Mend, Restoration]

Verb Pool = [Bash, Overhead Slam, Earthquake, Quick Slash, Fan of Blades,
             Vital Strike, Analyze, Logic Bomb, System Crack, Purify,
             Mend, Restoration]

Total: 12 Verbs available for auto-combat
```

### Verb Slots Per Character

| Character Level Range | Verb Slots |
|----------------------|------------|
| 1–10 | 2 slots |
| 11–25 | 3 slots |
| 26–50 | 4 slots |
| 51+ | 5 slots |

A full party of 4 at high level contributes **20 Verbs** to the pool. This is the endgame breadth of tactical options.

### Auto-Combat Verb Selection

Combat is idle/auto — the game picks Verbs from the pool automatically. The selection algorithm:

```
Each combat tick:
  1. Get list of enemies with their stat types
  2. For each enemy (priority: lowest HP first):
     a. Filter pool for Verbs not on cooldown
     b. Score each available Verb:
        - Advantage bonus: +100 priority if Verb type beats enemy type
        - Disadvantage penalty: -50 priority if enemy type beats Verb type
        - Damage score: baseDamage × statScaling × characterStatValue
        - Status value: +30 if enemy doesn't already have this status
        - AoE bonus: +20 per additional target that would be hit
     c. Select highest-scoring Verb
     d. Execute Verb, put on cooldown
  3. Process status effect ticks on all affected entities
  4. Check for wave clear / boss defeat
```

**Key design point:** The player never manually picks Verbs during combat. The strategy is *before* combat — choosing which characters and which Verbs to equip. The auto-select is smart enough that good composition feels rewarding, and bad composition feels like your team is struggling.

### Player-Facing Verb Priority

Players can set a **priority order** for their Verbs if they want more control:

```
┌──────────────────────────────────────┐
│  VERB POOL — Priority Order          │
│                                      │
│  1. ⭐ Analyze (INT)     [↕ drag]   │
│  2.    Vital Strike (DEX) [↕ drag]   │
│  3.    Earthquake (STR)   [↕ drag]   │
│  4.    Restoration (WIS)  [↕ drag]   │
│  5.    Fan of Blades (DEX)[↕ drag]   │
│  ...                                 │
│                                      │
│  [AUTO (Recommended)]  [SAVE ORDER]  │
└──────────────────────────────────────┘
```

Most players will leave it on Auto. Power users can optimize for specific encounters.

---

## Part 5: Status Effects — Complete Reference

### Status Effect Resolution

All status effects tick at the same rate as combat ticks. Effects can stack from different sources but the same effect from the same Verb refreshes duration instead of stacking.

### Effect Details

**Stagger (STR)**
```
- Trigger: On hit from STR Verb
- Base proc chance: 50% (100% if mental→physical cross)
- Duration: 1–3 ticks depending on Verb rarity
- Effect: Target's next action is delayed. Their cooldowns don't decrease
          for the duration. Effectively "skips their turn."
- Stacking: Does NOT stack. Reapplication refreshes duration.
- Counter: CON's Weaken status reduces Stagger duration by 1 tick
- Visual: Stars circling target's head, stumbling animation
```

**Bleed (DEX)**
```
- Trigger: On hit from DEX Verb
- Base proc chance: 50%
- Duration: 3–5 ticks
- Effect: Deals 5% of the triggering hit's damage per tick.
          Multiple Bleeds from different Verbs DO stack.
- Stacking: YES — up to 5 stacks from different sources
- Counter: WIS healing Verbs cleanse 1 Bleed stack per heal
- Visual: Red droplet particles, target flashes red on tick
- Math example: Vital Strike hits for 200 damage with Bleed.
  Bleed deals 10/tick for 5 ticks = 50 bonus damage.
  3 stacked Bleeds = 30/tick = 150 total bonus damage.
```

**Weaken (CON)**
```
- Trigger: On hit from CON Verb
- Base proc chance: 50%
- Duration: 2–4 ticks
- Effect: Target deals 20% less damage from all sources.
          Reduces Stagger duration on allies by 1 tick.
- Stacking: Does NOT stack. Reapplication refreshes duration.
- Counter: CHA's Confuse overrides Weaken (replaces it)
- Visual: Gray tint on target, "cracking armor" particles
```

**Expose (INT)**
```
- Trigger: On hit from INT Verb
- Base proc chance: 50% (100% if mental→physical cross)
- Duration: 2–4 ticks
- Effect: Target takes 25% more damage from ALL sources.
          This is the party-wide damage amplifier.
- Stacking: Does NOT stack. Reapplication refreshes duration.
- Counter: CON Verbs have 50% chance to resist Expose
- Visual: Target glows with vulnerability cracks, cyan highlight
- Synergy: THE key enabler. An Exposed target hit by an
  advantaged STR Verb takes: base × 1.5 (advantage) × 1.25 (exposed) = 1.875×
```

**Reveal (WIS)**
```
- Trigger: On hit from WIS Verb
- Base proc chance: 50%
- Duration: 2–4 ticks
- Effect: Strips all buffs from target. Prevents new buff application.
          Also reveals hidden enemies (if encounter has stealth mobs).
- Stacking: Does NOT stack. Reapplication refreshes duration.
- Counter: INT-type enemies have 30% chance to resist Reveal
- Visual: Green light wash, "unveiling" animation, buff icons shatter
- Key use: Hard counter to CHA enemy buffs and self-buffing bosses
```

**Confuse (CHA)**
```
- Trigger: On hit from CHA Verb
- Base proc chance: 50% (100% if mental→physical cross)
- Duration: 2–4 ticks
- Effect: Target has 30% chance per tick to attack an ally instead
          of your party. If no allies present, target skips action.
          Overrides and replaces Weaken if both would be active.
- Stacking: Does NOT stack. Reapplication refreshes duration.
- Counter: WIS Reveal cleanses and prevents Confuse
- Visual: Purple spiral eyes, question marks, target sways
- Fun factor: This is the "chaos" effect. Watching a boss
  punch its own minion is deeply satisfying.
```

### Status Effect Interaction Matrix

When multiple effects are on the same target:

| Active Effect | New Effect Applied | Result |
|--------------|-------------------|--------|
| Stagger | Bleed | Both active — Bleed ticks even during Stagger |
| Stagger | Expose | Both active — Exposed + Staggered is devastating |
| Weaken | Confuse | Confuse **replaces** Weaken |
| Weaken | Stagger | Both active — Weaken reduces Stagger duration by 1 |
| Expose | Reveal | Both active — Exposed + no buffs = fully vulnerable |
| Confuse | Reveal | Reveal **cleanses** Confuse |
| Bleed ×3 | Bleed (new source) | Stacks — now Bleed ×4 |
| Bleed ×5 | Bleed (new source) | Capped — refreshes longest duration |

---

## Part 6: Species & Stat Distribution

Species (the player's collectible characters) each have a stat distribution that determines which Verbs they can equip and how effective those Verbs are.

### Stat Distribution Model

Each species has a total of **30 stat points** distributed across the six stats. The distribution creates the species' identity.

**Archetypes:**

| Archetype | Primary | Secondary | Dump Stats | Example Distribution |
|-----------|---------|-----------|-----------|---------------------|
| Brute | STR (12) | CON (8) | INT (2), CHA (2) | 12/3/8/2/3/2 |
| Rogue | DEX (12) | WIS (6) | CON (2), CHA (3) | 3/12/2/4/6/3 |
| Tank | CON (12) | STR (7) | INT (2), DEX (3) | 7/3/12/2/4/2 |
| Mage | INT (12) | WIS (7) | STR (2), DEX (2) | 2/2/3/12/7/4 |
| Sage | WIS (12) | INT (6) | STR (2), DEX (3) | 2/3/4/6/12/3 |
| Trickster | CHA (12) | DEX (6) | CON (2), STR (3) | 3/6/2/4/3/12 |
| Hybrid | — | — | None extreme | 5/5/5/5/5/5 |

### Stat Scaling for Verbs

A Verb's actual damage depends on the character's stat value for that Verb's type:

```
actualDamage = verb.baseDamage × (1 + characterStat × verb.statScaling × 0.1)

Example:
  Earthquake (STR Verb, baseDamage 55, statScaling 1.0)
  Used by Brute species (STR = 12):
  actualDamage = 55 × (1 + 12 × 1.0 × 0.1) = 55 × 2.2 = 121

  Same Verb used by Mage species (STR = 2):
  actualDamage = 55 × (1 + 2 × 1.0 × 0.1) = 55 × 1.2 = 66
```

**This is why party composition matters.** A STR Verb on a STR character hits nearly twice as hard as the same Verb on a low-STR character. Players want to match Verb types to each character's strong stats.

### Verb Equip Restriction

Characters can equip any Verb, but Verbs from stats below a threshold perform poorly:

| Character's Stat Value | Verb Performance |
|-----------------------|-----------------|
| 8+ (strong) | Full scaling, full status proc chance |
| 5–7 (moderate) | Full scaling, 75% status proc chance |
| 3–4 (weak) | 75% scaling, 50% status proc chance |
| 1–2 (dump) | 50% scaling, 25% status proc chance |

This softly discourages mismatched loadouts without hard-blocking them. A Brute CAN equip Analyze (INT), it just won't proc Expose reliably.

---

## Part 7: Party Composition Strategy

### The "Coverage" Problem

Enemies in encounters and locations are typed. The game generates enemy compositions that test the player's party coverage.

**Enemy Wave Composition Patterns:**

| Wave Type | Enemy Stat Types | Tests |
|-----------|-----------------|-------|
| Physical Gauntlet | STR, DEX, CON mix | Physical triangle knowledge |
| Mental Maze | INT, WIS, CHA mix | Mental triangle knowledge |
| Split Push | 2 Physical + 2 Mental | Cross-triangle coverage |
| Boss + Adds | Boss (1 type) + minions (counter type) | Focus vs. AoE decisions |
| Mirror Match | Same types as player party | Forces neutral matchups |
| Hard Counter | Types that counter player's primary | Punishes one-dimensional parties |

### Recommended Party Templates

These emerge naturally from the advantage system. Players will discover them through play:

**"The Balanced Squad" (Safest, No Weakness)**
```
Character 1: STR primary — Physical DPS
Character 2: DEX primary — Multi-hit + Bleed
Character 3: INT primary — Expose for damage amp
Character 4: WIS primary — Healing + Reveal support
Coverage: Physical ✓ (STR+DEX), Mental ✓ (INT+WIS), Healing ✓
Weakness: No CON (squishy), No CHA (no chaos tools)
```

**"The Blitz" (Max Offense, High Risk)**
```
Character 1: STR primary — Burst damage
Character 2: DEX primary — Bleed stacking
Character 3: INT primary — Expose everything
Character 4: CHA primary — Confuse to prevent damage
Coverage: All offensive types represented
Weakness: No healing (WIS), no damage reduction (CON)
```

**"The Fortress" (Max Survival, Slow Clear)**
```
Character 1: CON primary — Tank + Weaken
Character 2: WIS primary — Healer + Reveal
Character 3: CON secondary / STR primary — Off-tank DPS
Character 4: WIS secondary / INT primary — Support DPS
Coverage: Extreme survivability, good debuff coverage
Weakness: Very slow kill speed, time-limited encounters punish this
```

**"The Cheese" (Exploit Specific Matchup)**
```
Character 1: INT — Expose
Character 2: INT — More Expose
Character 3: STR — Demolish exposed targets
Character 4: STR — More demolition
Coverage: INT+STR focused. Melts WIS and DEX enemies.
Weakness: Destroyed by CHA and CON enemies. High risk, high reward.
```

---

## Part 8: Integration with Existing Systems

### Encounter Integration

Encounters from the Key system now have **stat-typed enemies**. The key color determines the *loot*, but the encounter's enemy composition determines the *combat challenge*.

**New field on Encounter ScriptableObject:**

```csharp
[Header("Enemy Composition")]
public EnemyWaveConfig[] waves;

[System.Serializable]
public class EnemyWaveConfig
{
    public StatType[] enemyTypes;         // what stats enemies use
    public float difficultyMultiplier;    // scales HP/damage
    public EnemyBehavior behaviorPattern; // Aggressive, Defensive, Mixed
}
```

**Encounter Difficulty from Verb Matchup:**

The performance score from encounters now factors in how well the party's Verbs matched up against enemies:

```
performanceScore = baseScore
    + (advantageHits × 2)        // reward good matchups
    - (disadvantageHits × 1)     // mild penalty for bad matchups
    + (statusProcs × 1.5)        // reward status effect usage
    + (waveClearSpeed × 3)       // reward fast clears
```

This means a well-composed party earns more bonus loot rolls even in the same encounter.

### Location Integration

Locations now have a **dominant stat type** that determines what enemies spawn there:

| Location | Biome | Dominant Enemy Types | Best Party Counter |
|----------|-------|---------------------|-------------------|
| Sunken Vault | Underwater | CON + WIS (tanky, healing) | DEX + INT (bleed through tanks, expose) |
| Ember Forge | Volcanic | STR + INT (burst + expose) | CON + CHA (absorb burst, confuse) |
| Crystal Garden | Enchanted | WIS + CHA (heals + confusion) | INT + WIS (expose + reveal to strip buffs) |
| Void Rift | Dark | INT + DEX (expose + bleed) | WIS + CON (cleanse bleeds, resist expose) |
| Sky Citadel | Floating | All types mixed | Balanced party required |
| Frozen Archive | Ice | CON + INT (tanky + smart) | DEX + CHA (bleed through, chaos) |

This creates a reason for players to own multiple species and swap party composition based on what encounter or location they're entering. **Party loadout becomes a strategic decision tied to the content they're playing.**

### Key Color → Verb Loot Connection

New loot category for the Key system — **Verb Drops**:

| Key Color | Verb Types Found | Why |
|-----------|-----------------|-----|
| Red (Combat) | STR and DEX Verbs | Offensive physical |
| Blue (Economy) | CHA Verbs + gold items | CHA's idle bonus is +% gold |
| Green (Growth) | CON and WIS Verbs | Sustain and support |
| Purple (Prestige) | INT Verbs + meta items | INT's idle bonus is +% XP |
| Gold (Legendary) | Any stat Legendary Verbs | The chase |

This means key color preference is now influenced by what Verb types the player needs for their party. A player building a physical-heavy party hunts Red keys. A player who just got a new CHA species hunts Blue keys.

---

## Part 9: How the Player Learns This

This system has depth, but the player should never need to read this document. Here's the onboarding:

### Tutorial Sequence (First 30 Minutes)

1. **Start with 1 character** (balanced type). Give them 2 STR Verbs. Player taps through first encounter. "Verbs are your attacks."

2. **Fight a CON enemy.** STR Verbs show red "disadvantage" arrows. Damage numbers are small. Player feels the resistance. "Some Verbs work better against certain enemies."

3. **Unlock second character** (DEX type) with DEX Verbs. Fight same CON enemy. Green "advantage" arrows. Big damage. "Different characters beat different enemies."

4. **Show the Verb Pool screen.** "Your party shares their Verbs. More characters = more options." The pool now has STR + DEX Verbs.

5. **Fight a mixed wave** (CON + STR enemies). Auto-combat uses DEX Verbs on CON enemies, and the STR character uses different Verbs on STR enemies. Player sees the system working. "Your team figures out the best attacks automatically."

6. **Unlock third character.** Player picks from INT, WIS, or CHA species. First real strategic choice. Game briefly highlights what each stat beats.

### Ongoing Learning

- **Encounter preview** always shows enemy stat types with colored icons
- **Advantage arrows** on the combat HUD (green up = advantage, red down = disadvantage)
- **Post-encounter summary** shows "Advantage hits: 12, Disadvantage hits: 3" so players see the correlation with performance
- **Party screen** has a "Coverage" indicator showing which enemy types the party handles well vs. poorly

### The "Aha Moments"

These are designed to happen naturally:

- **"Oh, I need a mental character"** — After losing to a wave of CHA enemies with an all-physical party
- **"Expose is broken"** — First time INT Exposes a target and then STR hits it for massive damage
- **"I should swap my party for this location"** — Ember Forge keeps wrecking the player's INT-heavy team
- **"Confuse is hilarious"** — Watching the boss punch its own minion

---

## Part 10: ScriptableObject Architecture

### Complete Data Model

```
Assets/Data/
├── Stats/
│   ├── SO_Stat_STR.asset         — stat metadata, icon, color, idle bonus type
│   ├── SO_Stat_DEX.asset
│   ├── SO_Stat_CON.asset
│   ├── SO_Stat_INT.asset
│   ├── SO_Stat_WIS.asset
│   └── SO_Stat_CHA.asset
├── StatusEffects/
│   ├── SO_Status_Stagger.asset   — duration, potency, visual, stacking rules
│   ├── SO_Status_Bleed.asset
│   ├── SO_Status_Weaken.asset
│   ├── SO_Status_Expose.asset
│   ├── SO_Status_Reveal.asset
│   └── SO_Status_Confuse.asset
├── Verbs/
│   ├── STR/
│   │   ├── SO_Verb_Bash.asset
│   │   ├── SO_Verb_OverheadSlam.asset
│   │   └── ...
│   ├── DEX/
│   ├── CON/
│   ├── INT/
│   ├── WIS/
│   └── CHA/
├── Species/
│   ├── SO_Species_Brute.asset     — stat distribution, verb slots, art
│   ├── SO_Species_Rogue.asset
│   └── ...
├── Enemies/
│   ├── SO_Enemy_STR_Grunt.asset   — stat type, HP, damage, verbs used
│   └── ...
└── AdvantageTable/
    └── SO_AdvantageMatrix.asset   — the complete lookup table
```

### Advantage Matrix ScriptableObject

```csharp
[CreateAssetMenu(menuName = "IdleGame/Advantage Matrix")]
public class AdvantageMatrix : ScriptableObject
{
    [System.Serializable]
    public class MatchupEntry
    {
        public StatType attacker;
        public StatType defender;
        public float damageMultiplier;
        public float statusProcModifier;   // multiplied against base proc chance
    }

    public MatchupEntry[] matchups;        // 36 entries (6×6)

    public MatchupResult GetMatchup(StatType attacker, StatType defender)
    {
        var entry = matchups.First(m =>
            m.attacker == attacker && m.defender == defender);
        return new MatchupResult
        {
            damageMultiplier = entry.damageMultiplier,
            statusProcModifier = entry.statusProcModifier,
            advantage = entry.damageMultiplier > 1.1f
                ? Advantage.Super
                : entry.damageMultiplier < 0.9f
                    ? Advantage.Weak
                    : Advantage.Neutral
        };
    }
}
```

### Combat Resolver

```csharp
public class CombatResolver
{
    private AdvantageMatrix matrix;
    private VerbPool pool;

    public CombatTickResult ProcessTick(List<Enemy> enemies, Party party)
    {
        var result = new CombatTickResult();

        // 1. Select best verb for each active enemy
        foreach (var enemy in enemies.Where(e => e.isAlive))
        {
            var bestVerb = SelectBestVerb(enemy, pool);
            if (bestVerb == null) continue; // all on cooldown

            var character = bestVerb.owner;
            var matchup = matrix.GetMatchup(bestVerb.verb.statType, enemy.statType);

            // 2. Calculate damage
            float rawDamage = bestVerb.verb.baseDamage
                * (1 + character.GetStat(bestVerb.verb.statType)
                   * bestVerb.verb.statScaling * 0.1f);

            float finalDamage = rawDamage * matchup.damageMultiplier;

            // Check for Expose on target
            if (enemy.HasStatus(StatusType.Expose))
                finalDamage *= 1.25f;

            // Check for Weaken on target (reduces THEIR damage, not incoming)
            // Weaken is checked when enemy attacks, not here

            // 3. Apply damage
            for (int i = 0; i < bestVerb.verb.hitCount; i++)
            {
                enemy.TakeDamage(finalDamage / bestVerb.verb.hitCount);
                result.totalDamage += finalDamage / bestVerb.verb.hitCount;
            }

            // 4. Attempt status effect proc
            float procChance = bestVerb.verb.effectProcChance
                * matchup.statusProcModifier
                * character.GetStatProcModifier(bestVerb.verb.statType);

            if (Random.value <= procChance)
            {
                enemy.ApplyStatus(bestVerb.verb.effect,
                    bestVerb.verb.effectDuration,
                    bestVerb.verb.effectPotency);
                result.statusProcs++;
            }

            // 5. Track for performance scoring
            if (matchup.advantage == Advantage.Super)
                result.advantageHits++;
            else if (matchup.advantage == Advantage.Weak)
                result.disadvantageHits++;

            // 6. Put verb on cooldown
            bestVerb.StartCooldown();
        }

        // 7. Tick all active status effects
        foreach (var enemy in enemies)
            enemy.TickStatuses();

        // 8. Process enemy attacks against party
        ProcessEnemyAttacks(enemies, party, result);

        return result;
    }
}
```

### Remote Config Tuning Keys

```
// Advantage multipliers (tune without updates)
advantage_strong_multiplier: 1.5
advantage_weak_multiplier: 0.67
cross_physical_to_mental_damage: 1.2
cross_mental_to_physical_damage: 0.8
cross_physical_to_mental_proc: 0.0
cross_mental_to_physical_proc: 1.0

// Status effect tuning
status_stagger_skip_ticks: 1
status_bleed_percent_per_tick: 0.05
status_bleed_max_stacks: 5
status_weaken_damage_reduction: 0.20
status_expose_damage_amplification: 0.25
status_confuse_friendly_fire_chance: 0.30
status_reveal_duration_base: 2

// Verb scaling
verb_stat_scaling_coefficient: 0.1
verb_weak_stat_threshold: 4
verb_weak_stat_scaling_penalty: 0.75
verb_weak_stat_proc_penalty: 0.50
verb_dump_stat_threshold: 2
verb_dump_stat_scaling_penalty: 0.50
verb_dump_stat_proc_penalty: 0.25

// Species
species_total_stat_points: 30
species_verb_slots_base: 2
species_verb_slots_per_tier: 1
species_verb_slot_tier_thresholds: [10, 25, 50]
```

---

## Part 11: UI Quick Reference Card

This is what appears in-game when the player taps the "?" icon on any combat screen:

```
┌────────────────────────────────────────────┐
│          ⚔️ COMBAT TYPE GUIDE              │
│                                            │
│  PHYSICAL TRIANGLE:                        │
│  🟠 STR ──beats──▶ ⚪ DEX                  │
│  ⚪ DEX ──beats──▶ 🟤 CON                  │
│  🟤 CON ──beats──▶ 🟠 STR                  │
│                                            │
│  MENTAL TRIANGLE:                          │
│  🔵 INT ──beats──▶ 🟢 WIS                  │
│  🟢 WIS ──beats──▶ 🟣 CHA                  │
│  🟣 CHA ──beats──▶ 🔵 INT                  │
│                                            │
│  CROSS-TRIANGLE:                           │
│  Physical → Mental = more damage, no effects│
│  Mental → Physical = less damage, sure effects│
│                                            │
│  ✅ = 1.5× damage    ❌ = 0.67× damage     │
│                                            │
│  TIP: Build a party that covers both       │
│  triangles for full enemy coverage!         │
└────────────────────────────────────────────┘
```

This single screen is all a player needs to understand the system. Everything else is learned through play.
