# Unity Idle Clicker Project Guide
## Packages, Services & Asset Store Recommendations

---

## Core Unity Packages (Install via Package Manager)

### 1. Ads Mediation (Unity LevelPlay) — **Critical**

This is the #1 package you need. LevelPlay is Unity's first-party ad mediation platform (born from the Unity + ironSource merger). It provides a unified auction across 20+ ad networks from a single SDK integration.

**What it does:** Serves rewarded video, interstitial, banner, and native ads. Mediates between networks like AdMob, AppLovin, Meta Audience Network, Mintegral, Pangle, InMobi, Vungle, and more — automatically selecting the highest-paying ad for each impression.

**How to install:** Window → Package Manager → Unity Registry → "Ads Mediation" → Install. Then launch the LevelPlay Network Manager from the top menu to add network adapters. You'll need a LevelPlay account (free signup via ironSource).

**Key features for idle clickers:**
- Rewarded video ads (watch ad → get 2x idle earnings, speed boost, etc.)
- Interstitial ads between prestige resets or major milestones
- Banner ads on passive screens
- Built-in A/B testing for ad placement strategy
- Real-time revenue dashboards with per-network breakdowns

### 2. In-App Purchasing (Unity IAP) — **Critical**

Unity's unified IAP API handles both Google Play Billing and Apple StoreKit from one codebase. Currently on v5.x with Google Play Billing Library v8 and StoreKit 2 support.

**How to install:** Window → Package Manager → Unity Registry → "In-App Purchasing" → Install. Then enable via Edit → Project Settings → Services → In-App Purchasing toggle.

**Product types you'll want for an idle clicker:**
- **Consumables:** Gem packs, currency bundles, time skips
- **Non-consumables:** "Remove Ads" permanent unlock, premium skins
- **Subscriptions:** VIP pass (2x earnings, exclusive content, daily rewards)

**Pro tip:** Unity IAP includes a Codeless IAP option with drag-and-drop IAP Buttons — great for rapid prototyping. v5.0+ also includes a fake store for Editor-mode testing without needing real store accounts.

### 3. Unity Analytics — **Important**

Pre-built dashboards for player behavior, revenue tracking, and retention analysis. Automatically captures sessions, DAU/MAU, and revenue when paired with IAP.

**Key metrics for idle clickers:** Session length, return frequency, prestige reset rates, ad engagement rates, conversion funnels for IAP offers.

### 4. Remote Config — **Important**

Tune your game's economy without shipping an update. Store key-value pairs in the cloud and modify them from the Unity Dashboard.

**Idle clicker uses:**
- Adjust upgrade costs and scaling curves live
- Tune idle earnings rates if players progress too fast/slow
- Toggle limited-time events or seasonal multipliers
- A/B test different economy balances across player segments

### 5. Cloud Save — **Recommended**

Server-side save data so players can transfer progress between devices. Especially important for idle games where progress represents weeks/months of play.

**Why it matters:** Losing months of idle progress is the #1 reason players leave 1-star reviews. Cloud Save with Unity Authentication (anonymous or platform sign-in) solves this.

### 6. Economy — **Recommended**

Server-authoritative virtual currency and inventory management. Pairs with IAP for secure purchase fulfillment and with Cloud Code for server-validated transactions.

**Idle clicker uses:** Define currencies (gold, gems, prestige points), virtual purchases, and bundles in the dashboard. Prevents client-side cheating on premium currency.

### 7. Cloud Code — **Optional but powerful**

Run server-side JavaScript/C# logic. Useful for validating purchases, calculating offline earnings server-side, or running seasonal events without app updates.

### 8. Authentication — **Recommended**

Required for most UGS services. Supports anonymous auth (no login required — just works), plus optional platform sign-in (Google Play Games, Apple Game Center, Facebook).

---

## Unity Editor Features to Configure

### IL2CPP Scripting Backend
Switch from Mono to IL2CPP for both Android and iOS builds. Produces smaller, faster native code. **Required for iOS** and strongly recommended for Android.

**Set via:** Edit → Project Settings → Player → Other Settings → Scripting Backend → IL2CPP

### Managed Stripping Level
Set to "High" to strip unused code from builds. Combined with IL2CPP, this gets your APK down toward the ~15 MB range for a 2D idle game.

### Addressables
For managing asset loading efficiently — load UI screens, upgrade art, and prestige content on-demand instead of all at startup. Reduces initial load time and memory footprint.

### Unity UI Toolkit (or TextMeshPro + Canvas)
Idle clickers are UI-heavy games. UI Toolkit is Unity's newer system (CSS-like styling, better performance for complex UIs). TextMeshPro on traditional Canvas is more mature with more community resources. Either works — pick based on your comfort level.

---

## Asset Store — Worth Investigating

### Complete Templates (Study, Don't Ship As-Is)

These are useful for understanding architecture patterns, not for reskinning and publishing directly:

| Asset | Why It's Useful |
|-------|----------------|
| **Solo Clicker — Idle Clicker Kit** | Has prestige systems, CPS (clicks per second), franchise mechanics, number formatting, offline earnings, and autosave. Closest to AdVenture Capitalist architecture. |
| **IDLE RPG CLICKER — Complete Game (Mobile)** | Full mobile-ready project with progression systems. Good reference for UI flow and monetization integration. |
| **Idle Tycoon Clicker** / **Business Tycoon — Idle Clicker** | Tycoon-style templates with upgrade trees and manager systems. |
| **Clicker-Idle Game Template (SoloQ)** | Long-running template with multiple game variants (car, farmer, factory). Mature codebase. |

### Systems & Libraries

| Asset / Library | What It Solves |
|----------------|---------------|
| **uClicker** (free, GitHub) | ScriptableObject-based clicker library. Handles upgrades, buildings, currencies, and tick systems without boilerplate. Install via Unity Package Manager with GitHub URL. |
| **Big Number libraries** | C# has native `System.Numerics.BigInteger`, but for formatted display (1.5M, 2.3B, 4.7aa) look for "Idle Number" or "Big Double" implementations on the Asset Store or GitHub. |
| **DOTween Pro** | Animation tweening for satisfying number popups, UI transitions, and juice effects. The free DOTween works too but Pro adds visual editor + shortcuts. |
| **Odin Inspector** | Editor tooling for managing ScriptableObject-heavy architectures (upgrade definitions, prestige tiers, etc.). Massive quality-of-life improvement for data-driven idle games. |
| **NaughtyAttributes** (free) | Lighter alternative to Odin for inspector customization. |

### UI Assets

| Asset | Use Case |
|-------|----------|
| **Modern UI Pack** | Clean, mobile-friendly UI components (buttons, sliders, toggles, progress bars) |
| **GUI Pro - Simple Casual** | Specifically designed for casual/idle mobile games |
| **TextMeshPro** (built-in) | Already included — use it for all text rendering. Critical for displaying formatted big numbers. |

### Audio

| Asset | Use Case |
|-------|----------|
| **Casual Game Sounds** / **UI Sound Effects** | Satisfying click sounds, upgrade chimes, prestige fanfares |
| **Master Audio** | Audio management system with pooling — important because idle clickers play hundreds of short SFX clips |

---

## Recommended Architecture Pattern

Idle clickers are data-driven games. The proven architecture uses:

1. **ScriptableObjects** for all game definitions (upgrades, generators, prestige tiers, achievements). This separates data from logic and makes balancing easy.

2. **A central GameManager** singleton that handles the core idle loop: calculate earnings per tick, apply multipliers, process offline time on resume.

3. **Save/Load system** using JSON serialization to local storage + Cloud Save for cross-device sync. Save frequently (every 30s or on any purchase).

4. **Offline earnings calculation** on app resume: `offlineEarnings = earningsPerSecond * min(elapsedSeconds, maxOfflineSeconds)`. Cap offline time to prevent absurd accumulation.

5. **Event-driven UI** that subscribes to currency/upgrade changes rather than polling every frame.

---

## UGS Free Tier Limits

All Unity Gaming Services have generous free tiers that cover most indie games:

- **Analytics:** 100 events/user/hour, unlimited users
- **Cloud Save:** 5 GB storage, 10M API calls/month
- **Remote Config:** Included with Analytics
- **Cloud Code:** 1M API calls/month
- **Economy:** 5M API calls/month
- **Authentication:** Unlimited

You won't hit these limits until you have significant traction — and by then revenue should cover the scaling costs.

---

## Quick-Start Checklist

1. Create a Unity 6 (LTS) project, 2D template
2. Set scripting backend to IL2CPP, stripping to High
3. Install packages: Ads Mediation, In-App Purchasing, Analytics, Remote Config, Cloud Save, Authentication
4. Sign up for LevelPlay (ironSource dashboard) and configure ad networks
5. Set up IAP products in Google Play Console and App Store Connect
6. Study Solo Clicker Kit or uClicker for architecture patterns
7. Build your core idle loop with ScriptableObject-driven generators
8. Integrate ads at natural breakpoints (prestige, milestone rewards, optional 2x boosts)
9. Test with Unity's fake store and LevelPlay test mode before going live
10. Deploy to both stores from the same codebase
