# Sprint 11: Shop, Offline Earnings, Character XP, Ads/IAP — Design

**Date:** 2026-07-01
**Status:** Designed with autonomous defaults (user AFK at ads-depth question; recommended service-layer option taken, amended: real Unity Ads now, real IAP store wiring deferred to Sprint 12's device pass because the Unity Purchasing 5.x scripted API is new — interface + mock ship now so the flow is playable in editor).
**Source:** roadmap Sprint 11 (incl. PM rulings: character XP in MVP, ads in MVP), master design §Boosts/§Idle Exploring, gameplay-loop doc.

## Workstreams

### 1. Boosts (`BoostManager`, Combat assembly — pure C#)

Two timed boosts per the design doc:
| Boost | Cost | Duration | Effect |
|---|---|---|---|
| Auto-Fire Verbs | 500g × questLevel | 5 min | drawn Verbs fire automatically 2s after appearing |
| Verb Speed-Up | 300g × questLevel | 5 min | verb cooldowns halved; draw rotation 10s → 5s |

`BoostManager`: `Activate(type, duration, now)` (re-buying extends from current expiry), `IsActive(type, now)`, `Remaining(type, now)`; expiries as unix seconds, persisted in SaveData. Time is always passed in (testable). EconomyConfig knobs: `boostAutoFireCostPerLevel=500`, `boostSpeedUpCostPerLevel=300`, `boostDurationSeconds=300`.

Effect wiring in GameManager: speed-up → `VerbPool` tick cooldowns twice + draw cooldown override 5s while active; auto-fire → in Update, any drawn verb older than 2s fires via the normal `OnVerbTapped` path when not verb-locked.

### 2. Timed chest

`chestIntervalSeconds = 14400` (4h). SaveData stores `chestReadyAtTimestamp`. Claim when ready → gold bundle (`GoldPerKill(questLevel) × 25`) + 1 item at rarity floor Uncommon; **watch ad → double** (×2 gold, 2 items). Claiming re-arms the timer. Full-inventory items fall into the flagged overflow stopgap (converts to gold — unchanged this sprint, redesign still owed).

### 3. Offline earnings

Pure `OfflineEarningsCalculator.Calculate(offlineSeconds, questLevel, config)`:
`goldPerSecond = GoldPerKill(questLevel) × exploreKillsPerMinute / 60`, through the existing `OfflineGold()` (50% efficiency, 8h cap). On launch, when offline ≥ 60s, GameManager computes pending earnings and fires `OnOfflineEarningsPending`; an `OfflineClaimSheet` (BottomSheet) shows time away + gold with **Claim** / **Claim 2x (ad)**. Unclaimed pending earnings are granted plainly if the sheet is dismissed (never lost). No offline loot queue for MVP (design's Common/Uncommon claim queue deferred with the Destinations work).

### 4. Character XP (PM ruling: in MVP)

Income only — leveling mechanics already exist (weighted auto-allocation, 3 pts/level, manual Level Up button):
- Per kill: every active party member gains `charXpBase + questLevel` (knob `charXpBase = 3`)
- Quest completion: bonus `10 × questLevel` each (boss ×2)
Party screen's existing level-up glow now actually lights.

### 5. Ads + Remove Ads IAP (PM ruling: ads in MVP)

New `Starquill.Services` assembly (refs Core only):
- `IAdService { bool IsReady(placement); void ShowRewarded(placement, Action<bool> onResult); }` — implementations: `MockAdService` (editor: always succeeds after a frame), `UnityAdsService` (com.unity.ads 4.16.4, rewarded placements; game IDs injected at ship). Editor uses mock; device uses UnityAds via `#if UNITY_EDITOR` switch in a factory.
- `IIapService { bool RemoveAdsOwned; void PurchaseRemoveAds(Action<bool>); void Restore(); }` — `MockIapService` now (persisted flag in SaveData); real Unity Purchasing 5.x wiring is a Sprint 12 device task behind the same interface.
- Placements this sprint: `offline2x`, `chestDouble`. `removeAdsOwned` gates Sprint 12's interstitials only (rewarded placements remain — industry norm).

### 6. Shop screen (nav index 4, replaces placeholder)

Sectioned scroll (same pattern as Quests screen): **BOOSTS** (two cards: name, effect line, cost, Buy button with affordability state, live countdown chip while active) · **CHEST** (countdown / CLAIM + "2x (ad)" when ready) · **PREMIUM** (Remove Ads card: price placeholder, Owned state). `ShopScreenController` + pure `ShopPresenter` for label/state formatting (tested).

## Save additions

`boostAutoFireExpiry`, `boostSpeedUpExpiry`, `chestReadyAtTimestamp` (unix seconds), `removeAdsOwned`.

## Testing (EditMode)

BoostManager (activate/extend/expire/remaining), OfflineEarningsCalculator (cap, efficiency, <60s = zero), chest readiness/re-arm math (pure helper), char XP grant math (party split, quest bonus, boss double), ShopPresenter formatting, mock services. Play-mode captures: shop sections, boost purchase + countdown, chest claim, offline sheet (simulated timestamp), XP glow.

## Out of scope

Real IAP store wiring (Sprint 12 device pass), interstitials (Sprint 12), LevelPlay mediation (post-MVP swap behind IAdService), offline loot queue, reward-overflow redesign (still owed, tracked in roadmap).
