# Starquill MVP Roadmap

**Last updated:** 2026-07-01
**Baseline:** Sprints 1-8 + equipment stat redesign complete (see `docs/sprint-review.md`). All 262 EditMode tests pass. Current-state reference: `docs/implemented-systems.md`.

Each sprint follows spec-driven development: design doc → implementation plan → task-by-task execution with tests → review. Plan docs live in `docs/plans/` with the `YYYY-MM-DD-<topic>-design.md` / `-plan.md` naming convention.

---

## Sprint 8.5: Equipment & Loot UI Readability — IMPLEMENTED 2026-07-01, extended into the Mobile UI Redesign (UiTheme/UiFactory design system, ItemCard v2, bottom-sheet flow, app-wide mobile sizing — see `2026-07-01-mobile-ui-redesign-design.md`). Explore/Loot/Party visually verified in play mode; final Test Runner pass + ClearSave play-test pending.

**Why first:** the stat pair + AwakenedAbility redesign passes all tests but cannot be verified in gameplay — the current item displays don't legibly present the new model. Every later sprint (quest rewards, shop) renders items through these same components, so fixing them first prevents rework.

**Goal:** a player can look at any item and immediately understand what it is, what it does, and whether it's an upgrade.

**Scope:**
- Equipment card redesign: rarity color treatment, primary stat emphasized over secondary, ability name + level shown, slot/type identification at a glance
- `ItemDetailPanel` redesign: full stat pair breakdown, ability description with current potency, ability XP progress bar, gold level-up button with cost (wired to existing `GameManager.LevelUpAbility`)
- Upgrade comparison: equipped-vs-candidate stat deltas surfaced on cards and in the detail panel (`ItemComparer` already provides scoring)
- Loot drop toast/feed readability on the explore screen

**Exit criteria:** play-test confirms stat pairs, ability rolls, ability XP gain, and gold level-up all behave per the redesign plan — the gameplay half of the original Task 9.

---

## Sprint 9: Quest System Backend — IMPLEMENTED 2026-07-01 (Test Runner pass pending)

**Goal:** quests as structured multi-wave challenges discovered during exploration, pure logic with test coverage, no UI.

**Scope:**
- Quest generation from `QuestZoneDefinition` (data model already exists, currently unconsumed)
- Quest discovery roll during exploration (`questDiscoveryRate = 0.03` knob already in EconomyConfig)
- Quest state machine: Discovered → Accepted → InProgress (wave N of M) → BossWave → Completed / Retreated
- Curated rewards: gold payout + guaranteed loot rolls at quest rarity floor; retreat penalty (`questRetreatGoldPenalty = 0.50`)
- GameManager integration: quest mode vs explore mode wave spawning, quest events for UI
- Save/load: active quest state, discovered-but-unaccepted quests
- Full EditMode test coverage on generation, state transitions, and reward math

---

## Sprint 10: Quests Screen UI

**Goal:** the player-facing quest loop.

**Scope:**
- Quest discovery popup on the explore screen (accept / dismiss)
- Quests screen: available and active quest list, quest detail (zone, waves, rewards preview)
- In-quest explore screen state: wave progress indicator (x of M), boss wave presentation, retreat button
- Completion flow: reward reveal, return to exploring
- Quest retry affordance after retreat

---

## Sprint 11: Shop Screen + Offline Earnings

**Goal:** the economy's spend-and-return loop.

**Scope:**
- Shop screen: purchasable timed boosts (Auto-Fire Verbs, Verb Speed-Up per design doc §Boosts), boost timers persisted in save
- Timed exploration chest (4-hour cycle)
- Offline earnings: elapsed-time calculation on resume (`OfflineGold()` formula already implemented), claim modal
- Boost effects wired into the combat tick and verb systems

**Note:** `com.unity.purchasing` 5.4.0 is installed and unused. Whether real IAP/rewarded ads ship in MVP is an open decision (below); this sprint builds the shop against gold only.

---

## Full UI Pass 2 (scheduled after Sprints 9-11 land)

The 2026-07-01 mobile UI redesign established the design system and fixed density/readability, but it was executed while Quests, Shop, and offline earnings didn't exist. Once those are functional, a second full UI pass is needed: unify the new screens onto UiTheme/UiFactory, revisit information architecture with real content, art-direct the placeholder surfaces (buttons, frames, backgrounds are flat color blocks), and re-run the heuristic evaluation across the complete app. Track alongside Sprint 12 polish.

## Sprint 12: Polish + MVP Ship

**Goal:** shippable Android build.

**Scope:**
- First-time-user tutorial (tap verbs, equip loot, accept a quest)
- Animation/juice pass: verb activation, loot drops, quest completion, screen transitions
- Balance pass across EconomyConfig knobs via play-testing
- Bug sweep and performance check on device
- Android build validation, icon/splash, store metadata prep

---

## Open Decisions (flagged, not blocking Sprints 8.5-10)

| Decision | Options | Current lean |
|---|---|---|
| Character level-up / stat allocation | In original MVP scope, never built. Add as Sprint 11.5, fold into Sprint 12, or cut to post-MVP | Post-MVP: equipment + abilities already provide progression; adding a second progression system before ship risks the timeline |
| Monetization wiring (rewarded ads, IAP) | Ship MVP gold-only, or wire ads/IAP in Sprint 11-12 | Gold-only MVP; monetization as first post-MVP sprint once retention is observed |
| Cloud save / analytics / Remote Config | Sprint 12 or post-MVP | Post-MVP |

## Post-MVP Backlog (existing designs)

- Dungeon key / fragment destinations (`docs/dungeon-key-system-design-doc.md`)
- Prestige with Stellar Ink (design doc §Prestige; `prestigeMultiplier` stubbed at 1.0 throughout)
- Additional species, quest zones, seasonal events, social features
