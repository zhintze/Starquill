# Sprint 10: Quests Screen UI — Design

**Date:** 2026-07-01
**Status:** Validated with user section-by-section, then REVISED per user review (2026-07-01 evening):
1. Quest banner moved to the TOP of the explore screen (open sky area), not above the verb bar.
2. The dual-mode **path indicator bar** from the original design (Sprint 4 open questions, never built) is now implemented: always visible below the TopBar — explore mode shows travel progress toward a GUARANTEED quest discovery (arrival forces an offer if the 3% roll never fired; `travelWavesToDiscovery = 20` knob, progress persisted in save); quest mode shows wave progress. The banner's embedded wave fill was removed in its favor.
3. Quests screen: segmented tabs replaced by ONE scroll view with section headers (QUEST / DESTINATIONS) — quests alone don't warrant a full tab.
**Source:** `docs/roadmap.md` Sprint 10; Sprint 9 backend (`QuestLog`, GameManager quest API/events); UiTheme/UiFactory/BottomSheet kit.

## Requirement carried in from user

The Quests screen must reserve UI room for the designed-but-deferred systems: dungeon instances, encounters, locations (`docs/dungeon-key-system-design-doc.md`), and quest dialogue/milestones — without relayout when they land.

## Explore-screen surfaces

**QuestBannerDisplay** — one banner strip above the verb bar, state-driven:
| State | Content | Tap |
|---|---|---|
| Offered | "Quest discovered · tap to view", pulsing accent border | QuestOfferSheet |
| Retreated | "Retry {name}" (persistent retry affordance) | QuestOfferSheet (retry mode) |
| InQuest | "{name} · Wave n/N" + ProgressBar fill; "BOSS" in gold on boss waves; compact Retreat button | Retreat → confirm inside sheet |
| Idle | hidden | — |
Subscribes: `OnQuestOffered`, `OnQuestRetreated`, `OnWaveStarted`, `OnQuestCompleted`. Built on a new `UiFactory.Banner` primitive (reused later by offline earnings / shop notices).

**QuestOfferSheet** (fills BottomSheet): name + tier chip, zone intro dialogue line (deterministic pick: index = questIndex % dialogue.Length), wave count, rewards preview from `QuestRewardSpec` ("Guaranteed Rare+ item · ~Ng gold"), ACCEPT primary + Decline secondary; retry mode swaps to Retry/Dismiss; in-quest mode shows Retreat confirmation ("Keep only half the gold earned — retreat?").

**QuestCompletionSheet**: auto-opens on `OnQuestCompleted` — completion dialogue line, gold bonus, reward ItemCards (tap → item detail sheet), next-quest teaser; boss adds "Zone cleared — {next zone} unlocked". Continue closes.

## Quests screen (`QuestsScreenController`, screen index 1)

SegmentedTabs **[Quests | Destinations]**.

**Quests tab:** current-quest card mirroring `QuestLog.Phase` (Offered: inline Accept/Decline · Active: progress + Return-to-battle + Retreat · Retreated: Retry/Dismiss · Idle: "Exploring for the next quest…" hint); zone header (name, "Zone n"); **ladder row** of 11 nodes (filled done / ringed current / dim ahead / star boss; E/H caption markers) via a new `UiFactory.LadderRow` primitive derived from `NextQuestIndex`.

**Destinations tab (future-proofing, ships now):** fragment ProgressBar bound to `ExplorationManager.FragmentProgress` (live data), then locked cards — Encounters (Keys), Locations, Dungeons — lock treatment + one-line teasers from the dungeon-key doc. Post-MVP systems unlock these cards in place.

## Architecture & data flow

- New `QuestPresenter` (static, pure): formatting for banner text per phase, rewards preview, ladder node states, dialogue selection — the unit-testable layer.
- Controller pattern identical to LootScreenController: self-initializing `Start` (one-frame defer), event subscriptions, refresh on `OnScreenChanged`; zero quest state held in UI; all mutations via GameManager quest API.
- `ExploreSceneBuilder`: quests placeholder → quests panel scaffold (tabs container + Quests/Destinations content roots + serialized wiring); banner container in ExplorePanel.

## Testing

EditMode: `QuestPresenter` (banner strings per phase/wave, rewards preview per tier, ladder node states across log states, dialogue pick determinism and bounds). Play-mode captures: offer banner → sheet → accept → wave HUD → completion sheet; retreat confirm → retry; tabs + destinations teaser.

## Out of scope (YAGNI)

Mid-quest dialogue events, milestone unlock logic, fragments consumption, key/encounter/location/dungeon mechanics (UI slots only), quest offer expiry.
