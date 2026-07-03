# Dungeon Key & Location Discovery System
## Game Design Document — Unity Idle Clicker

> **SUPERSEDED (2026-07-02):** this doc was written for a generic idle clicker
> (generators, prestige, tap combos) and predates Starquill's implemented
> systems. The current design of record is
> `docs/plans/2026-07-02-destinations-design.md`. Kept for idea provenance only.

---

## System Overview

The game has two interconnected modes:

1. **Main Mode** — The core idle clicker loop. Players earn currency, upgrade generators, prestige, etc. Keys and discovery items drop as secondary rewards during normal play.

2. **Encounter Mode** — Players spend keys to enter color-themed encounters with targeted loot pools. This is the "spend keys, get specific gear" system.

3. **Location Discovery** — A time-limited exploration mechanic where players "discover" a location that stays open for 10–20 minutes, offering high-chance drops of specific loot if actively grinded.

These three systems feed into each other in a loop:

```
Main Mode (idle earnings + key drops)
    ↓
Keys → Encounter Mode (targeted loot)
    ↓
Loot → Powers up Main Mode (better earnings, new generators, multipliers)
    ↓
Better Main Mode → More/rarer key drops + Discovery item drops
    ↓
Discovery Items → Location Discovery (time-limited grind for rare loot)
    ↓
Location Loot → Further powers up Main Mode + unlocks harder Encounters
```

---

## Part 1: The Key System

### Key Types (Color-Themed)

Each key color corresponds to a loot category. The key itself tells the player what kind of rewards they're working toward before they ever enter the encounter.

| Key Color | Theme | Loot Category | Visual Identity |
|-----------|-------|--------------|-----------------|
| **Red** | Combat / Power | Attack multipliers, damage boosts, offensive gear | Fire/ember particles |
| **Blue** | Economy / Wealth | Gold multipliers, idle earnings boosts, currency bonuses | Water/ice shimmer |
| **Green** | Growth / Nature | Generator upgrades, unlock tokens, expansion items | Vine/leaf particles |
| **Purple** | Prestige / Arcane | Prestige multipliers, meta-progression items, rare cosmetics | Arcane/swirl effects |
| **Gold** | Legendary | Cross-category loot, guaranteed rare+ drops, exclusive items | Glowing/radiant pulse |

### Key Tiers

Each color comes in three tiers that affect encounter difficulty and loot quality:

| Tier | Name | Drop Rate from Main Mode | Encounter Duration | Loot Quality |
|------|------|--------------------------|-------------------|-------------|
| 1 | Common Key | Frequent (every few minutes) | 2–3 min | Common + Uncommon loot |
| 2 | Rare Key | Moderate (every 15–30 min) | 5–7 min | Uncommon + Rare loot |
| 3 | Epic Key | Infrequent (every 1–2 hrs) | 8–12 min | Rare + Epic + small Legendary chance |

**Gold keys** only come in one tier and are exceptionally rare — earned from milestones, events, or IAP.

### How Keys Drop

Keys drop from the main idle loop through multiple channels:

- **Passive idle earnings** — Small chance per tick cycle (increases with prestige level)
- **Milestone rewards** — Guaranteed key at specific upgrade/stage thresholds
- **Prestige resets** — Bonus keys based on prestige multiplier at reset
- **Ad rewards** — Watch rewarded video → choose from 2–3 random key offers
- **Daily login** — Escalating key rewards on consecutive days
- **Location Discovery** — Keys can also drop from location grinding

### Key Inventory

Players have a key pouch (inventory) with a soft cap. Display keys visually as a collection — players should see their keys and feel the urge to use them.

- **Soft cap:** 20 keys (configurable via Remote Config)
- **Over cap:** Keys still drop but at reduced rates, nudging players to spend them
- **No hard cap:** Never block a player from earning — just reduce rate

---

## Part 2: Encounter Mode (Using Keys)

### Encounter Flow

```
Player selects a key from inventory
    ↓
Encounter screen shows: theme, duration timer, loot pool preview
    ↓
Player confirms → Key consumed → Encounter begins
    ↓
Active gameplay: tap/click to fight enemies, clear waves, open chests
    ↓
Encounter ends (timer or completion) → Loot summary screen
    ↓
Player collects loot → Returns to Main Mode
```

### Encounter Mechanics

Encounters are **active gameplay sessions** — not idle. This creates a satisfying contrast with the main idle loop. The player is spending a resource (key) and investing time, so the payoff needs to feel rewarding.

**Wave System:**
- Encounter spawns waves of themed enemies
- Each wave cleared drops small loot (currency, fragments)
- Every 3–5 waves: a mini-boss with guaranteed loot drop
- Final wave: encounter boss with best loot chance

**Performance Bonus:**
- Track player performance metrics during the encounter:
  - Speed of wave clears
  - Combo chains (tapping speed)
  - No-hit streaks
  - Total damage dealt
- Performance score determines **bonus loot rolls** at the end
- This is how "how the user uses the key" affects loot — not just key type, but player skill/engagement

### Loot Determination Algorithm

Loot is randomized but weighted by three factors:

```
Final Loot = f(KeyColor, KeyTier, PerformanceScore)

1. KeyColor → Selects the LOOT POOL (which table to roll on)
2. KeyTier → Sets the RARITY FLOOR (minimum quality of drops)
3. PerformanceScore → Grants BONUS ROLLS (extra chances at higher rarity)
```

**Example: Blue Rare Key + High Performance**
- Pool: Economy loot table (gold multipliers, earnings boosts)
- Floor: Uncommon minimum (no common drops)
- Bonus: 2 extra rolls at Rare+ tier
- Result: 3–5 items, mostly Uncommon/Rare, with a decent shot at Epic

### Loot Rarity Tiers

| Rarity | Color Code | Drop Weight (Tier 1) | Drop Weight (Tier 2) | Drop Weight (Tier 3) |
|--------|-----------|---------------------|---------------------|---------------------|
| Common | White/Gray | 60% | 30% | 10% |
| Uncommon | Green | 30% | 40% | 30% |
| Rare | Blue | 8% | 22% | 35% |
| Epic | Purple | 1.8% | 7% | 20% |
| Legendary | Orange | 0.2% | 1% | 5% |

### Key Combining (Optional Depth)

Allow players to combine keys for strategic loot targeting:

- **3 Common keys of same color → 1 Rare key** (crafting)
- **2 different color Rare keys → 1 Epic key of player's choice** (cross-category)
- **Any 5 Epic keys → 1 Gold key** (endgame chase)

This gives players agency and prevents key types they don't want from feeling like dead drops.

---

## Part 3: Location Discovery System

This is the "find a temporary grind spot" mechanic — distinct from key encounters.

### Discovery Mechanic

**How locations are discovered:**
- Players find **Discovery Fragments** during main mode gameplay (idle drops, milestones, encounter rewards)
- Collecting enough fragments of the same type reveals a location on the map
- Fragments come in themed sets matching biomes/location types

**Fragment Thresholds:**

| Location Tier | Fragments Needed | Average Time to Discover | Window Duration |
|--------------|-----------------|------------------------|----------------|
| Common Location | 3–5 | 30 min – 1 hr | 20 minutes |
| Rare Location | 8–12 | 2–4 hours | 15 minutes |
| Epic Location | 15–20 | 6–12 hours | 12 minutes |
| Legendary Location | 30+ | 1–3 days | 10 minutes |

**Design intent:** Higher-tier locations have shorter windows but dramatically better loot. This creates urgency — when a Legendary location pops, the player needs to drop what they're doing and grind it.

### Location Types

Each location has a themed biome that determines its loot specialty:

| Location | Biome | Specialty Loot | Visual Theme |
|----------|-------|---------------|-------------|
| Sunken Vault | Underwater ruins | Gold/currency multipliers | Blue-green, bubbles, coral |
| Ember Forge | Volcanic cavern | Attack/power items | Red-orange, lava, sparks |
| Crystal Garden | Enchanted grove | Generator upgrades, growth items | Green, crystals, vines |
| Void Rift | Dark dimension | Prestige items, meta-currency | Purple, dark energy, stars |
| Sky Citadel | Floating fortress | Mixed high-tier loot | Gold, clouds, light beams |
| Frozen Archive | Ice cavern | Blueprint/schematic drops | White-blue, frost, ancient runes |

### Location Grinding

Once a location is discovered and activated, the player enters a **timed active-play session**:

**The Grind Loop (repeating cycle within the window):**
```
Enter Location → Timer starts (10–20 min countdown visible)
    ↓
Kill enemies / Clear obstacles → Loot drops in real-time
    ↓
Every 60–90 seconds: Mini-event (treasure chest, rare spawn, bonus wave)
    ↓
Every 3–5 minutes: Location Boss (high loot chance, brief cooldown after)
    ↓
Timer expires → Location closes → Summary screen with all collected loot
```

**Loot Pacing Within Location:**
- **First 5 minutes:** Steady common/uncommon drops, establishing the grind rhythm
- **Middle stretch:** Increasing rare drop rates, mini-bosses get harder but more rewarding
- **Final 2–3 minutes:** "Fever mode" — drop rates spike, enemies worth more, last-chance rush

This pacing curve keeps players engaged for the full window instead of checking out after the first few minutes.

### Location-Specific Loot Tables

Each location has a **focused loot table** — this is what makes locations feel special vs. random encounters. Players should be able to think "I need a gold multiplier, so I want to find a Sunken Vault."

**Example: Sunken Vault (Economy Location)**

| Item | Rarity | Drop Chance per Kill | Effect |
|------|--------|---------------------|--------|
| Barnacled Coin | Common | 40% | +0.5% idle gold |
| Treasure Map Fragment | Uncommon | 25% | Combine 5 → permanent +2% gold |
| Mermaid's Purse | Rare | 8% | +10% gold for 1 hour |
| Kraken's Hoard | Epic | 2% | +25% gold permanently |
| Poseidon's Trident | Legendary | 0.3% | +50% gold + unique visual |

### Discovery Notifications

When a location is discovered, make it feel special:

- **Push notification** (if app is backgrounded): "A Sunken Vault has been discovered! 20 minutes to explore."
- **In-app alert:** Full-screen reveal animation with the biome theme
- **Map marker:** Location appears on a discovery map with countdown timer
- **Sound design:** Distinct discovery chime that players learn to associate with opportunity

---

## Part 4: Interconnection & Economy Balance

### The Virtuous Loop

The three systems should feed each other without creating runaway inflation:

```
Main Mode earnings → Buy upgrades → Faster key drops
Keys → Encounters → Loot that multiplies Main Mode
Main Mode progress → Discovery fragments → Locations
Locations → Rare loot + keys + fragments for other locations
```

### Anti-Inflation Safeguards

- **Diminishing returns on loot stacking** — 10 gold multipliers don't give 10× the bonus; use logarithmic scaling
- **Loot expiration on time-limited buffs** — Temporary boosts from locations expire, creating ongoing demand
- **Prestige resets affect encounter scaling** — Higher prestige = harder encounters but better loot floor
- **Daily/weekly caps on Legendary drops** — Prevent grinding exploits from breaking progression
- **Key drop rate ceiling** — Even with maxed upgrades, keys drop at a controlled rate

### Monetization Touchpoints

These systems create natural, non-aggressive monetization opportunities:

| Touchpoint | What's Offered | Player Value |
|-----------|---------------|-------------|
| **Key Packs (IAP)** | Bundles of specific key colors/tiers | Skip key farming, target desired loot |
| **Location Extender (Rewarded Ad)** | +5 minutes on an active location | More time in a good grind spot |
| **Key Upgrade (Rewarded Ad)** | Upgrade a Common key to Rare before use | Better loot from existing keys |
| **Instant Discovery (IAP)** | Immediately discover a random location | Skip fragment collection |
| **Loot Reroll (Rewarded Ad)** | Reroll one item from encounter results | Second chance at better rarity |
| **Fragment Doubler (Rewarded Ad)** | Double fragments earned for 30 min | Faster location discovery |

---

## Part 5: Unity Implementation Architecture

### ScriptableObject Data Model

Define all keys, encounters, locations, and loot as ScriptableObjects for easy balancing:

```
Assets/
├── Data/
│   ├── Keys/
│   │   ├── SO_Key_Red_Common.asset
│   │   ├── SO_Key_Red_Rare.asset
│   │   ├── SO_Key_Red_Epic.asset
│   │   ├── SO_Key_Blue_Common.asset
│   │   └── ... (5 colors × 3 tiers = 15 key definitions)
│   ├── Encounters/
│   │   ├── SO_Encounter_Red.asset      (encounter template per color)
│   │   ├── SO_Encounter_Blue.asset
│   │   └── ...
│   ├── Locations/
│   │   ├── SO_Location_SunkenVault.asset
│   │   ├── SO_Location_EmberForge.asset
│   │   └── ...
│   ├── LootTables/
│   │   ├── SO_LootTable_Red_Tier1.asset
│   │   ├── SO_LootTable_Red_Tier2.asset
│   │   ├── SO_LootTable_SunkenVault.asset
│   │   └── ...
│   └── Items/
│       ├── SO_Item_BarnacledCoin.asset
│       ├── SO_Item_MermaidsPurse.asset
│       └── ...
```

### Key ScriptableObject Definition

```csharp
[CreateAssetMenu(menuName = "IdleGame/Key Definition")]
public class KeyDefinition : ScriptableObject
{
    public string keyId;
    public string displayName;
    public KeyColor color;           // enum: Red, Blue, Green, Purple, Gold
    public KeyTier tier;             // enum: Common, Rare, Epic
    public Sprite icon;
    public Color themeColor;
    public ParticleSystem vfxPrefab;
    public float encounterDuration;  // seconds
    public LootTable associatedLootTable;
    public float baseDropRate;       // chance per idle tick
}
```

### Loot Table ScriptableObject

```csharp
[CreateAssetMenu(menuName = "IdleGame/Loot Table")]
public class LootTable : ScriptableObject
{
    public string tableId;
    public LootEntry[] entries;

    [System.Serializable]
    public class LootEntry
    {
        public ItemDefinition item;
        public Rarity rarity;
        public float weight;           // relative weight for weighted random
        public int minQuantity;
        public int maxQuantity;
        public KeyTier minimumKeyTier; // only drops if key tier >= this
    }

    public List<ItemDrop> Roll(KeyTier tier, int bonusRolls)
    {
        // Filter entries by tier, then do weighted random selection
        // bonusRolls from performance score grant extra picks
    }
}
```

### Location Discovery ScriptableObject

```csharp
[CreateAssetMenu(menuName = "IdleGame/Location")]
public class LocationDefinition : ScriptableObject
{
    public string locationId;
    public string displayName;
    public string description;
    public Sprite biomeIcon;
    public Color biomeColor;
    public LocationTier tier;           // Common, Rare, Epic, Legendary
    public int fragmentsRequired;
    public float windowDurationMinutes; // 10–20 min
    public LootTable locationLootTable;
    public float bossSpawnIntervalSeconds;
    public EnemyWaveConfig[] waves;
    public AudioClip discoveryChime;
    public GameObject environmentPrefab;
}
```

### Core Managers

```
Managers/
├── KeyManager.cs           — Key inventory, drop logic, combining
├── EncounterManager.cs     — Encounter lifecycle, wave spawning, scoring
├── LocationManager.cs      — Fragment tracking, discovery, timer management
├── LootManager.cs          — Roll resolution, loot distribution, inventory
├── NotificationManager.cs  — Push notifications for discoveries
└── SaveManager.cs          — Persist keys, fragments, loot, active timers
```

### Key Manager Responsibilities

```csharp
public class KeyManager : MonoBehaviour
{
    // Inventory
    public Dictionary<string, int> keyInventory;  // keyId → count
    public int totalKeys => keyInventory.Values.Sum();
    public int softCap = 20;  // from Remote Config

    // Drop system
    public void ProcessIdleTick(float deltaTime, float prestigeMultiplier);
    public void AwardKey(KeyDefinition key, int count = 1);
    public bool TryConsumeKey(KeyDefinition key);

    // Combining
    public bool CanCombine(KeyDefinition source, int count, KeyDefinition target);
    public void CombineKeys(KeyDefinition source, int count, KeyDefinition target);

    // Drop rate modifiers
    public float GetDropRateModifier();  // reduced if over soft cap
}
```

### Encounter Manager Flow

```csharp
public class EncounterManager : MonoBehaviour
{
    public EncounterState state;  // Idle, Active, BossFight, Complete
    private float timer;
    private int currentWave;
    private float performanceScore;
    private List<ItemDrop> pendingLoot;

    public void StartEncounter(KeyDefinition key);
    public void ProcessWaveClear(WaveClearData data);
    public void SpawnBoss();
    public EncounterResult EndEncounter();

    // Performance tracking
    private void TrackCombo(int comboCount);
    private void TrackClearSpeed(float seconds);
    private int CalculateBonusRolls();  // from performanceScore
}
```

### Location Manager with Timer Persistence

```csharp
public class LocationManager : MonoBehaviour
{
    // Fragment tracking
    public Dictionary<string, int> fragmentCounts;  // locationId → count

    // Active location
    public LocationDefinition activeLocation;
    public DateTime activationTime;
    public DateTime expirationTime;
    public bool isLocationActive => activeLocation != null
                                    && DateTime.UtcNow < expirationTime;

    // Discovery
    public void AddFragment(string locationId, int count = 1);
    public bool CanDiscover(LocationDefinition location);
    public void DiscoverLocation(LocationDefinition location);

    // Persistence — save activation/expiration times so closing
    // the app doesn't cheat the timer
    public void SaveState();
    public void LoadState();

    // Notifications
    public void ScheduleExpirationNotification();
}
```

### Remote Config Keys (Tune Without Updates)

Define these in Unity Remote Config so you can balance live:

```
key_soft_cap: 20
key_drop_rate_base: 0.02
key_drop_rate_overcap_penalty: 0.5
encounter_performance_bonus_max: 3
location_fever_mode_multiplier: 2.5
location_common_window_minutes: 20
location_legendary_window_minutes: 10
fragment_drop_rate_base: 0.01
loot_legendary_daily_cap: 2
combine_ratio_common_to_rare: 3
combine_ratio_rare_to_epic: 2
```

### Save Data Structure

```csharp
[System.Serializable]
public class EncounterSaveData
{
    // Key inventory
    public List<KeySlot> keys;          // keyId + count pairs

    // Fragment progress
    public List<FragmentSlot> fragments; // locationId + count pairs

    // Active location (if any)
    public string activeLocationId;
    public long activationTimestamp;     // Unix time
    public long expirationTimestamp;     // Unix time

    // Loot inventory
    public List<OwnedItem> items;       // itemId + count + metadata

    // Stats
    public int totalEncountersCompleted;
    public int totalLocationsDiscovered;
    public Dictionary<string, int> keysUsedByColor;  // for analytics
}
```

---

## Part 6: UX/UI Flow

### Key Inventory Screen

```
┌─────────────────────────────────────┐
│  🔑 KEY POUCH (14/20)              │
│                                     │
│  🔴 Red:    ■■■ Common(3)  ■ Rare(1)│
│  🔵 Blue:   ■■ Common(2)           │
│  🟢 Green:  ■■■■ Common(4) ■ Rare(1)│
│  🟣 Purple: ■■ Common(2)  ■ Epic(1) │
│  🟡 Gold:   (none)                  │
│                                     │
│  [USE KEY]    [COMBINE]    [INFO]   │
└─────────────────────────────────────┘
```

### Encounter Entry Screen (after selecting a key)

```
┌─────────────────────────────────────┐
│  ⚔️ RED ENCOUNTER — Rare Tier      │
│                                     │
│  Theme: Combat / Power              │
│  Duration: 5:00                     │
│  Loot Pool: Attack items            │
│                                     │
│  Possible Drops:                    │
│  ⬜ Flame Shard (Common)            │
│  🟢 Ember Blade (Uncommon)         │
│  🔵 Inferno Ring (Rare)            │
│  🟣 Dragon's Breath (Epic)         │
│  🟠 Phoenix Core (Legendary) ⭐    │
│                                     │
│  [🔑 USE RED RARE KEY]             │
│  [📺 UPGRADE KEY (Watch Ad)]       │
│  [← BACK]                          │
└─────────────────────────────────────┘
```

### Location Discovery Notification

```
┌─────────────────────────────────────┐
│                                     │
│  🗺️ LOCATION DISCOVERED!           │
│                                     │
│  ══════════════════════════          │
│  🌊 SUNKEN VAULT                   │
│  Tier: Rare                         │
│  ══════════════════════════          │
│                                     │
│  Specialty: Gold & Currency Items   │
│  Window: 15:00 remaining            │
│                                     │
│  [⚡ EXPLORE NOW]                   │
│  [⏰ REMIND ME]                     │
└─────────────────────────────────────┘
```

### Location Active Grind HUD

```
┌─────────────────────────────────────┐
│ 🌊 SUNKEN VAULT    ⏱️ 12:34 left   │
├─────────────────────────────────────┤
│                                     │
│     [Active gameplay area]          │
│     Enemies, chests, bosses         │
│                                     │
├─────────────────────────────────────┤
│ Loot collected this session:        │
│ 🪙×24  💎×3  📜×1  ⚔️×1           │
│                                     │
│ Next boss in: 0:45                  │
│ ████████████░░░░ FEVER MODE SOON    │
│                                     │
│ [📺 +5 MIN (Watch Ad)]             │
└─────────────────────────────────────┘
```

---

## Part 7: Analytics Events to Track

Wire these into Unity Analytics to understand how players interact with the systems:

```
key_earned        — {color, tier, source}
key_used          — {color, tier, encounter_id}
key_combined      — {source_color, source_tier, target_tier}
encounter_started — {key_color, key_tier}
encounter_ended   — {key_color, key_tier, performance_score, loot_count, loot_rarities}
fragment_earned   — {location_id, source}
location_discovered — {location_id, location_tier}
location_entered  — {location_id, location_tier, window_remaining}
location_expired  — {location_id, was_entered, time_spent}
location_extended — {location_id, method: "ad" | "iap"}
loot_collected    — {item_id, rarity, source: "encounter" | "location"}
```

These tell you: Are players earning enough keys? Are they using them or hoarding? Which colors are most/least popular? Are location windows long enough? Where do players drop off during encounters?

---

## Part 8: Content Scaling Plan

### Launch Content
- 5 key colors × 3 tiers = 15 key types
- 5 encounter themes (one per color)
- 4 location biomes
- ~40 unique loot items across all tables

### Month 1–3 Updates (via Remote Config + small patches)
- 2 additional location biomes
- Seasonal event keys (limited-time color, e.g., "Frost Key" for winter)
- ~20 more loot items
- Key combining recipes

### Month 3–6 Updates
- Location chaining (discover 2+ locations in sequence for combo bonuses)
- Encounter modifiers (key + consumable item = modified encounter with different rules)
- Leaderboard for encounter performance scores
- Guild/social key sharing

This system gives you months of content pipeline without fundamental code changes — just new ScriptableObject assets and loot table entries.
