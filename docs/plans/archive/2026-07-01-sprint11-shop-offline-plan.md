# Sprint 11: Shop, Offline Earnings, Character XP, Ads/IAP — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** The economy's spend-and-return loop: timed boosts, 4-hour chest, offline claim, character XP income, rewarded-ad placements and Remove Ads behind service abstractions.

**Architecture:** Pure logic first (BoostManager, OfflineEarningsCalculator, chest math, XP grants — all TDD), a new Services assembly for ad/IAP abstractions with editor mocks + real Unity Ads, GameManager orchestration, then the sectioned Shop screen and launch-time offline sheet on the existing UI kit.

**Tech Stack:** C# / Unity 6, NUnit EditMode, com.unity.ads 4.16.4, UiFactory kit, Coplay compile checks.

**Design doc:** `docs/plans/2026-07-01-sprint11-shop-offline-design.md`

**Verification:** compile per task; user Test Runner at checkpoints (never script runs); play-mode captures. Glyph rule applies.

---

### Task 1: EconomyConfig knobs + BoostManager (TDD)
- Modify `Assets/Scripts/Data/EconomyConfig.cs`: `boostAutoFireCostPerLevel=500f`, `boostSpeedUpCostPerLevel=300f`, `boostDurationSeconds=300f`, `chestIntervalSeconds=14400f`, `chestGoldKillMultiple=25f`, `charXpBase=3`
- Create `Assets/Scripts/Combat/BoostType.cs` (AutoFireVerbs, VerbSpeedUp) + `Assets/Scripts/Combat/BoostManager.cs`: expiry map (unix seconds, injected `now`), `Activate` extends from max(now, expiry), `IsActive`, `Remaining`, `RestoreExpiries/GetExpiry`
- Test `Assets/Tests/EditMode/Combat/BoostManagerTests.cs`: inactive by default, activate→active, expiry passes→inactive, re-buy extends, remaining math, restore
- Compile → commit `feat: BoostManager + economy knobs`

### Task 2: OfflineEarningsCalculator + chest math (TDD)
- Create `Assets/Scripts/Exploration/OfflineEarningsCalculator.cs` (static): `Gold(offlineSeconds, questLevel, config)` via GoldPerKill×killsPerMinute/60 → `config.OfflineGold`; returns 0 under 60s. `ChestReward(questLevel, config, doubled)` → (gold, itemRolls: 1 or 2, floor Uncommon)
- Tests `Assets/Tests/EditMode/Exploration/OfflineEarningsCalculatorTests.cs`: zero under 60s, 50% efficiency vs active rate, 8h cap, chest doubling
- Compile → commit `feat: offline earnings + chest math`

### Task 3: Services assembly (ads + IAP)
- Create `Assets/Scripts/Services/Starquill.Services.asmdef` (refs Core; Unity Ads referenced via `"Unity.Advertisement"`? — Unity Ads 4.x ships `UnityEngine.Advertisements` in a precompiled assembly, auto-referenced; if asmdef blocks it, add `"overrideReferences": false` default which allows precompiled)
- `IAdService`, `AdPlacement` consts (`offline2x`, `chestDouble`), `MockAdService` (succeed next frame via callback immediately), `UnityAdsService` (Initialize with gameId placeholder const, ShowRewarded via IUnityAdsShowListener → success on COMPLETED), `AdServiceFactory.Create()` → mock in editor (`#if UNITY_EDITOR`), UnityAds on device
- `IIapService`, `MockIapService` (flag via callback to GameManager persistence), no real store this sprint
- Compile → commit `feat: ad/IAP service layer (mock + Unity Ads)`

### Task 4: GameManager orchestration
- Fields: `BoostManager boosts`, `IAdService ads`, `IIapService iap`; expose `Boosts`, `Ads`, `Iap`; `double Now` helper (DateTimeOffset unix)
- SaveData: `boostAutoFireExpiry`, `boostSpeedUpExpiry`, `chestReadyAtTimestamp`, `removeAdsOwned` (+ SaveState/restore)
- Boost effects: Update() → auto-fire (drawn verb age ≥2s, not locked → OnVerbTapped); speed-up → extra `verbPool.TickCooldowns()` per tick + `verbPool.SetDrawCooldown(5f)` while active (add setter/override to VerbPool; restore 10s when inactive)
- Buys: `BuyBoost(BoostType)` (cost = perLevel × questLevel; gold gate; activate; event `OnBoostsChanged`)
- Chest: `ChestReady`, `ClaimChest(bool doubled)` → rewards via calculator + `RollRarityWithFloor`; re-arm; event `OnChestClaimed`
- Offline: in Start after load — `pendingOfflineGold = OfflineEarningsCalculator.Gold(saveManager.GetOfflineSeconds(), ...)`; if >0 fire `OnOfflineEarningsPending(seconds, gold)` (UI claims via `ClaimOfflineEarnings(bool doubled)`; auto-grant plain on quit-before-claim)
- Char XP: in kill paths — each active party member `xp += charXpBase + questLevel` per kill (CombatTickResult.EnemiesKilled); quest completion bonus `10 × questLevel` (boss ×2) + `OnRosterChanged` so party UI refreshes
- Remove Ads: `PurchaseRemoveAds()` via iap → persist flag
- Compile → commit `feat: boosts/chest/offline/char-XP orchestration`

### Task 5: ShopPresenter (TDD) + Shop screen + offline sheet
- Create `Assets/Scripts/UI/ShopPresenter.cs` (pure): boost card labels (cost string, active countdown "m:ss", affordability), chest state label (ready / countdown), offline summary ("Away 2h 13m · +N gold") + tests `Assets/Tests/EditMode/UI/ShopPresenterTests.cs`
- Create `Assets/Scripts/UI/ShopScreenController.cs`: sectioned scroll (BOOSTS / CHEST / PREMIUM) per QuestsScreenController pattern; 1s repaint tick for countdowns while visible; Buy/Claim/2x(ad via `gm.Ads.ShowRewarded`)/Remove Ads actions
- Create `Assets/Scripts/UI/OfflineClaimSheet.cs`: BottomSheet on `OnOfflineEarningsPending` (subscribe in ShopScreenController? better: ExploreSceneController) — time away, gold, Claim / Claim 2x (ad)
- Builder: replace Shop placeholder with scaffold (content root via BuildQuestContentRoot pattern), wire controller; subscribe offline sheet from explore controller root
- Compile → scene rebuild + save → commit `feat: shop screen + offline claim sheet`

### Task 6: Verification (CHECKPOINT)
1. Compile clean; user Test Runner (expect ~344 + ~25 new)
2. Play-mode: shop captures (boost buy → countdown chip; chest claim; Remove Ads mock), offline sheet (temporarily rewind lastPlayedTimestamp via script), XP glow on party screen after kills
3. Docs: sprint-review entry, roadmap complete, implemented-systems + gameplay-loop updates; commit
