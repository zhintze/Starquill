# Sprint 10: Quests Screen UI — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Player-facing quest loop — discovery banner, offer/retreat/completion sheets, in-quest wave HUD, and a tabbed Quests screen with a future-slot Destinations tab.

**Architecture:** Pure formatting in `QuestPresenter` (unit-tested); UI composed from UiFactory (+ two new primitives: Banner, LadderRow); controllers follow the LootScreen pattern (self-init Start, event-driven, zero owned state); scene scaffold via ExploreSceneBuilder.

**Tech Stack:** C# / Unity 6, UGUI + TMP procedural, NUnit EditMode, Coplay compile checks.

**Design doc:** `docs/plans/2026-07-01-sprint10-quests-ui-design.md`

**Verification:** compile check per task; user Test Runner at checkpoints (never script test runs); play-mode captures for flows. Glyph rule applies (no Unicode symbols in TMP text — pips/stars/locks are Images or ASCII).

---

### Task 1: QuestPresenter (TDD)

**Files:** Create `Assets/Scripts/UI/QuestPresenter.cs`; Test `Assets/Tests/EditMode/UI/QuestPresenterTests.cs`

Static, pure. API:
- `BannerText(QuestPhase, QuestSpec, int waveIndex)` → "Quest discovered · tap to view" / "Retry {name}" / "{name} · Wave n/N" (boss wave: "{name} · BOSS") / "" (Idle)
- `WaveFraction(QuestSpec, int waveIndex)` → (waveIndex)/(waves-1) clamped 0-1 for the progress fill (0 when null)
- `RewardsPreview(QuestRewardSpec, double estGold)` → "Guaranteed {floor}+ item · ~{N}g" / "Loot chance · ~{N}g" when no floor; extra roll appends "+ bonus item"
- `LadderStates(int nextQuestIndex, QuestPhase)` → `LadderNodeState[11]` enum (Done/Current/Ahead/Boss/BossCurrent); node 10 always Boss-flavored
- `PickDialogue(string[] lines, int questIndex)` → lines[questIndex % lines.Length], "" for null/empty
- `TierLabel(QuestTier)` → "Normal"/"Elite"/"Hard"/"BOSS"

Tests: every phase's banner string; boss-wave banner; fraction bounds; preview per tier (floor/no-floor/extra roll); ladder states for fresh/mid/boss-current logs; dialogue modulo + empty safety. Compile → commit `feat: QuestPresenter formatting layer`.

### Task 2: UiFactory primitives — Banner + LadderRow

**Files:** Modify `Assets/Scripts/UI/UiFactory.cs`

- `Banner(Transform parent, out BannerHandle)`: full-width strip, Card surface + 4px accent border Images (top/bottom), Body text left, optional compact action Button right (120px); handle: `SetText`, `SetAccent(Color)`, `SetVisible(bool)`, `SetAction(string, Action)`, `Pulse(bool)` (CanvasGroup alpha ping-pong via a small component added to the banner root).
- `LadderRow(Transform parent, LadderNodeState[] states)`: horizontal row of 64px Image nodes — Done: filled accent; Current: filled + white ring (outer Image); Ahead: 25% white; Boss: gold diamond (rotated square Image); BossCurrent: gold + ring. Caption markers "E"/"H" (Text) under indices 3,7,8,9.

Compile → commit `feat: Banner + LadderRow UI primitives`.

### Task 3: QuestOfferSheet + QuestCompletionSheet

**Files:** Create `Assets/Scripts/UI/QuestOfferSheet.cs`, `Assets/Scripts/UI/QuestCompletionSheet.cs`

`QuestOfferSheet.Show(Transform root, Mode mode)` — Mode enum Offer/Retry/RetreatConfirm; pulls spec from `GameManager.Instance.QuestLog.ActiveSpec` and zone via QuestZoneTable exposure (add `public QuestZoneTable QuestZones => questZones;` to GameManager):
- Offer: name (Heading), tier `Chip`, dialogue line (Caption, from `QuestPresenter.PickDialogue(zone.IntroDialogue, questIndex)`), "{N} waves" caption, rewards preview (est. gold = GoldPerKill*TotalEnemies*multiplier — add `public double EstimateQuestGold(QuestSpec)` to GameManager to reuse the exact formula), ACCEPT primary → `AcceptQuest()`, Decline secondary → `DeclineQuest()`
- Retry: same info, Retry primary → `RetryQuest()`, Dismiss destructive → `DismissRetreatedQuest()`
- RetreatConfirm: "Keep only half the gold earned this attempt — retreat?" + earned amount; Retreat destructive → `RetreatQuest()`, Keep fighting secondary → close

`QuestCompletionSheet.Show(root, QuestSpec, double goldBonus, List<EquipmentInstance> rewards, QuestZone zone, bool zoneCleared, string nextZoneName)`: completion dialogue, "+{gold}g" (Display, gold color), reward ItemCards (HideDelta, tap → ItemDetailSheet), teaser caption; boss: "Zone cleared — {next} unlocked"; Continue primary closes. Compile → commit `feat: quest offer/retreat/completion sheets`.

### Task 4: QuestBannerDisplay + explore wiring

**Files:** Create `Assets/Scripts/UI/QuestBannerDisplay.cs`; Modify `Assets/Editor/ExploreSceneBuilder.cs` (banner container above verb bar, shift LootToastFeed up), Modify `Assets/Scripts/Managers/GameManager.cs` (QuestZones + EstimateQuestGold exposure if not done in Task 3)

Component with self-init Start (one-frame defer): builds its Banner via UiFactory into its own RectTransform; subscribes `OnQuestOffered/OnQuestRetreated/OnQuestCompleted/OnWaveStarted` + refresh from current state; taps route per phase (Offered/Retreated → QuestOfferSheet modes; InQuest action button "Retreat" → RetreatConfirm mode). Completion event → `QuestCompletionSheet.Show`. Compile → scene rebuild + save → commit `feat: quest banner + wave HUD on explore screen`.

### Task 5: QuestsScreenController + Destinations tab

**Files:** Create `Assets/Scripts/UI/QuestsScreenController.cs`; Modify `Assets/Editor/ExploreSceneBuilder.cs` (replace CreatePlaceholderPanel quests placeholder with scaffold: tabs container + two content roots, controller wiring via SetPrivateField)

Quests tab (built procedurally into content root on Refresh): current-quest card per phase (UiFactory card surface; Offered → inline ACCEPT/Decline buttons calling GameManager; Active → wave ProgressBar + "Return to battle" (ScreenManager.ShowScreen(0)) + Retreat (confirm sheet); Retreated → Retry/Dismiss; Idle → hint caption), zone header, `LadderRow` from `QuestPresenter.LadderStates`. Destinations tab: fragment ProgressBar (`gm.Exploration.FragmentProgress`, target 100), three locked cards (lock = dim card + "LOCKED" chip + teaser caption: "Spend keys on targeted loot hunts" / "Discovered places, open briefly" / "Extended multi-zone challenges"). Refresh on OnScreenChanged(1) + quest events. Compile → scene rebuild + save → commit `feat: quests screen with ladder + destinations future-slot`.

### Task 6: Verification (CHECKPOINT)

1. Compile clean; user Test Runner (~350+ expected).
2. Play-mode: scripted offer (reflection HandleQuestDiscovered) → capture banner; open sheet → accept → capture wave HUD; clear waves (reflection) → capture completion sheet; retreat/retry flow; Quests screen both tabs captures.
3. Docs: sprint-review entry, roadmap Sprint 10 complete, implemented-systems UI section; commit `docs: Sprint 10 quests UI complete`.
