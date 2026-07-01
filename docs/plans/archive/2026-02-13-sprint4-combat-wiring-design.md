# Sprint 4: Minimum Viable Combat — Design Document

**Sprint:** 4
**Date:** 2026-02-13
**Status:** Draft

---

## Goal

Wire the existing backend systems (GameManager, VerbPool, CombatTickProcessor, ExplorationManager) to the ExploreScene UI built in Sprint 3. After this sprint, players will see enemies with HP bars, tap verb cards to deal damage with floating numbers, watch cards rotate with slide animations, and see gold accumulate in real-time.

## Architecture Decisions

### 1. GameManager Singleton
GameManager becomes a persistent singleton with `DontDestroyOnLoad`. The ExploreSceneController finds it via `GameManager.Instance` — no scene references needed.

### 2. Event-Driven UI Updates
GameManager exposes events that the UI subscribes to. No polling. ExploreSceneController subscribes in `OnEnable`, unsubscribes in `OnDisable`.

### 3. Verb Rotation: Pass-Count Staleness
Replace the timer-based `RotateStaleVerbs()` with a pass-count system. Each `DrawnVerb` tracks how many times it was "passed over" (not selected). At `PassCount >= 2`, the verb auto-replaces. This replaces `EconomyConfig.verbDrawCooldown` for rotation purposes (cooldown ticks still apply to used verbs).

## Verb Card Flow

```
[Bash]  [Analyze]  [Slash]     ← 3 cards visible
         ↓ tap Analyze
[Bash]  [Slash]  ← [NewVerb]   ← Analyze removed, others slide left, new slides in from right
         ↓ 3s lock (grayed out, non-interactive)
[Bash]  [Slash]  [NewVerb]     ← Unlocked, tappable again
```

1. Three cards always visible when unlocked
2. Player taps one — fires immediately via `GameManager.OnVerbTapped(slotIndex)`
3. Tapped card removed, unchosen two slide left, new card slides in from right
4. Whole verb bar locks for 3 seconds (configurable via `EconomyConfig.verbLockDuration`)
5. After lock, cards become tappable again
6. Cards passed over 2 rotations (PassCount >= 2) get auto-replaced during the fill step

## Enemy Display

Each enemy silhouette (Sprint 3 dark rectangles) gets:
- **HP bar** below the silhouette: fill color = stat type tint, background = dark gray
- **Stat type color tint** on the silhouette itself (subtle overlay)
- HP bar lerps smoothly when damage is taken (~0.3s)
- On death: silhouette + HP bar fade out over 0.5s

**Stat type colors:**
| Stat | Color |
|------|-------|
| STR | Red (0.9, 0.2, 0.2) |
| DEX | Green (0.2, 0.8, 0.3) |
| CON | Brown (0.7, 0.5, 0.2) |
| INT | Blue (0.3, 0.4, 0.9) |
| WIS | Purple (0.6, 0.3, 0.8) |
| CHA | Gold (0.9, 0.8, 0.2) |

## Combat Feedback: Damage Numbers

- Spawned at enemy position when damage is dealt
- Float upward ~100px over 1 second, then fade out
- **Color by advantage:** green (strong), white (neutral), red (weak)
- **Size:** 24pt for auto-attack ticks, 32pt for verb hits
- Gold earned floats up in gold color on kill (e.g., "+5g")
- Object pool of ~20 recycled TextMeshPro objects to avoid GC

## Gold Counter

- TopBarDisplay gold label uses smooth count-up animation
- Lerps from currently displayed value to actual value over ~0.5s
- Uses `double` formatting with suffix (1.2K, 3.4M, etc.)

## Wave Transitions

- On wave clear: 0.5s pause, then new enemies fade in from right
- Enemy count scales with quest level: `2 + questLevel / 10` (max 6)
- Wave counter in TopBar updates immediately on wave clear

## GameManager Changes

```csharp
// New: Singleton
public static GameManager Instance { get; private set; }

// New: Events
public event Action<CombatTickResult> OnCombatTick;
public event Action<double> OnGoldChanged;
public event Action<List<EnemyState>> OnWaveStarted;
public event Action OnWaveCleared;
public event Action<int, CombatTickResult> OnVerbActivated; // slotIndex, result

// New: Public read-only access
public IReadOnlyList<EnemyState> CurrentEnemies => currentEnemies;
```

## VerbPool Changes

```csharp
// DrawnVerb gets:
public int PassCount { get; set; }

// VerbPool gets:
public event Action<int, DrawnVerb> OnVerbDrawn;     // slotIndex, verb
public event Action<int, DrawnVerb> OnVerbRemoved;   // slotIndex, verb

// New method replacing RotateStaleVerbs:
public void IncrementPassCounts(int activatedSlotIndex)
// Called after activation — bumps PassCount on remaining verbs
// Removes any with PassCount >= 2

// Remove: RotateStaleVerbs (timer-based rotation)
```

## ExploreSceneController Changes

The controller becomes the bridge between GameManager and UI:

```csharp
// OnEnable: subscribe to GameManager events
// OnDisable: unsubscribe

// New methods:
void OnCombatTick(CombatTickResult result)    // Update enemy HP bars, spawn damage numbers
void OnGoldChanged(double newGold)            // Animate gold counter
void OnWaveStarted(List<EnemyState> enemies)  // Create enemy displays
void OnWaveCleared()                          // Trigger wave transition
void OnVerbCardTapped(int slotIndex)          // Forward to GameManager
void OnVerbActivated(int slot, CombatTickResult result) // Animate verb cards, spawn numbers
```

## New Files

| File | Assembly | Purpose |
|------|----------|---------|
| `Assets/Scripts/UI/VerbCardAnimator.cs` | Starquill.UI | DOTween-free slide/fade animations for verb cards using coroutines |
| `Assets/Scripts/UI/DamageNumberSpawner.cs` | Starquill.UI | Object pool of floating TextMeshPro damage numbers |
| `Assets/Scripts/UI/EnemyDisplayController.cs` | Starquill.UI | Manages HP bars and stat tint on enemy silhouettes |
| `Assets/Scripts/UI/GoldCounterAnimator.cs` | Starquill.UI | Smooth lerp counter for gold display |

## Modified Files

| File | Change |
|------|--------|
| `GameManager.cs` | Singleton pattern, events, expose enemies, verb lock timer |
| `VerbPool.cs` | PassCount mechanic, events, remove timer rotation |
| `DrawnVerb.cs` | Add PassCount field |
| `ExploreSceneController.cs` | GameManager subscription, verb tap forwarding, combat display |
| `VerbBarDisplay.cs` | Interactive card taps, lock state, card rebuild from DrawnSlots |
| `TopBarDisplay.cs` | Gold animation support, wave counter live updates |
| `EconomyConfig` | Add `verbLockDuration` field (default 3.0f) |
| `ExploreSceneBuilder.cs` | Wire new components if needed |

## Testing Strategy

**Edit Mode tests (pure C#):**
- VerbPool PassCount increment and auto-replace
- VerbPool events fire correctly
- GameManager singleton behavior
- GoldCounterAnimator lerp math
- DamageNumberSpawner pool recycling

**Play Mode / manual verification:**
- Verb card tap → damage numbers appear at enemy position
- Verb card slide animation timing and direction
- HP bar lerp on damage
- Enemy fade on death
- Wave transition (old enemies out, new enemies in)
- Gold counter smooth animation
- 3-second verb lock (bar grayed out, taps ignored)

## Not in Scope (Sprint 4)

- Path indicator bar (Sprint 5)
- Quest discovery / fragment drop UI notifications (Sprint 5)
- Bottom nav screen switching (Sprint 5)
- Sound effects
- Real art assets
- Character leveling / XP display
- Offline gold claim screen
