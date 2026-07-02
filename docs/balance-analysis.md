# Starquill Balance Analysis

**Date:** 2026-07-02 · All formulas transcribed from code; pacing numbers from `tools/balance_sim.py` (rerun after any knob change).

## The core concept for this genre

An idle clicker is a race between two exponentials. Area difficulty **must** grow exponentially (`EnemyHP = 50 × 1.12^Q`) so numbers feel explosive; therefore player power must **also** grow exponentially, tuned slightly slower, so that progress always continues but always decelerates — that soft, ever-present friction is the genre's engagement engine. When the pressure accumulates enough, prestige resets the race with a permanent multiplier and the loop restarts faster. Every successful game in this genre (including the Bounty Bash reference) is built on that spine: **exponential income → exponentially-priced upgrades → exponential power → deeper areas → more income.**

## What we have (formula inventory)

| Channel | Formula | Growth shape |
|---|---|---|
| Enemy HP | `50 × 1.12^Q` × (2 + Q/10, cap 6) enemies | **exponential** |
| Gold income | `5 × 1.10^Q` per kill | **exponential** |
| Auto-attack DPS | `topStat × 0.3` × 4 members | linear in stats |
| Verb damage | `base(30-65) × (1 + stat × 0.1) × advantage` | linear in stats |
| Character stats | +3 points/level; XP cost `100 × 1.18^L` vs income `(3+Q)/kill` | ~logarithmic levels → linear-ish stats |
| Equipment stats | budget fixed **by rarity** (C 4-6 … L 19-22), 11 slots | **bounded** (~hard cap) |
| Abilities | potency rarity-scaled, level-capped by rarity | **bounded** |
| Gold sinks | ability level-ups (capped), boosts (`500 × Q`, linear), — | linear/bounded |

## The simulation (party of 4, realistic gear/leveling, taps every ~4s)

| Q | waveHP | party DPS | secs/wave | cumulative hours |
|---|---|---|---|---|
| 10 | 466 | 148 | 3.1 | 0.1 |
| 20 | 1,929 | 176 | 11 | 0.4 |
| 30 | 7,490 | 193 | 39 | 1.5 |
| 40 | 27,915 | 211 | **132** | 5.3 |
| 50 | 86,701 | 223 | **388** | 18 |
| 60 | 269,279 | 236 | 1,142 | 56 |

Over 80 quest levels, enemy waves grow **23,000×** while party DPS grows **2.1×**. The wall is not a tuning problem — around Q35-45 (3-5 hours in) progress stops dead, because **the game has no exponential player-power channel at all.** The user-observed "Lv 1→16 in one night" is actually the healthy channel: character XP is logarithmic by design and self-limits; it just doesn't matter, because +3 stat points is additive noise against a 1.12^Q wall.

## Structural findings

1. **`EconomyConfig.UpgradeCost(10 × 1.15^level)` has zero consumers.** The exponential gold→power ladder — the genre's spine and this economy's designed centerpiece — was specced in Sprint 1 and never built. This is simultaneously the missing power channel AND the missing gold sink.
2. **Gold hyperinflates.** Income is `1.10^Q`; the biggest sink (boosts) is `500 × Q`, linear. By Q40 a boost costs ~2 minutes of income; by Q60, seconds. Ability level-ups cap out per item.
3. **Equipment stops mattering.** Budgets are rarity-fixed, and the rarity distribution saturates (~Q50 it's mostly Uncommon/Rare with the Common floor at 10%). After the first few hours, drops are lateral trades.
4. **CHA's designed identity is unwired.** `Party.GetGoldBonus()` (2%/CHA point, per the stat-identity table) exists and is never passed to `GoldPerKill` — the call site hardcodes 0.
5. **`ItemComparer.ScoreItem` ignores ability potency** — upgrade arrows and Optimize All slightly mis-rank items.
6. **Verb depth is a stub**: 4 starter verbs with flat bases; fine pre-wall, irrelevant after — but verb acquisition is deliberately post-MVP.

## Recommendations (in priority order)

**R1 — Build the Training ladder (uses the orphaned formula).** A per-character or party-wide repeatable upgrade: cost `UpgradeCost(trainLevel) = 10 × 1.15^level`, effect ×`1.06` party damage per level (compounding). With gold income at `1.10^Q`, players afford ~2 levels per quest level → power `≈1.124^Q` vs enemies `1.12^Q`: progress continues, friction slowly builds, prestige eventually pays it off. One system fixes findings 1, 2, and the wall. UI: a card in the Shop or the Party Actions tab.

**R2 — Scale equipment budgets with quest level.** `budget = rarityBudget × (1 + Q × 0.03)` (or `× 1.02^Q`): drops stay exciting forever, dovetails multiplicatively with R1, and makes rarity floors on quest rewards meaningful late. Sell values already scale with Q so the economy follows.

**R3 — Wire the CHA gold bonus** (one line) and add WIS's discovery bonus if we honor the identity table fully.

**R4 — Reprice boosts against the gold curve**: cost as minutes-of-current-income (e.g. `GoldPerKill(Q) × killsPerMinute × 3`) instead of `500 × Q`.

**R5 — `ScoreItem` += ability potency.**

**R6 — Retune after R1/R2 with the sim**: targets — reach Q20 in the first session (~30 min), soft friction from Q30, pre-prestige horizon ~Q60-70 at roughly 2 weeks of casual play; then prestige (post-MVP) resets with Stellar Ink multipliers.

## Open questions for the PM

1. **Where does the Training ladder live** — per-character (6 buttons, more decisions, more taps) or party-wide (one button, cleaner)? Recommendation: party-wide for MVP.
2. **Accept enemy-side counterweight?** R1+R2 without prestige means numbers only ever grow. Fine for MVP (prestige lands post-MVP), but confirm the pre-prestige wall target (~Q60-70?).
3. **XP curve**: with exponential channels in place, character levels stay a cozy secondary system. Keep 1.18 cost growth / 3 pts, or flatten to make levels feel steadier? Recommendation: keep, revisit post-R1.

---

## Resolution (2026-07-02, commit 05289c28)

All recommendations implemented per PM rulings (per-character Training with 50% catch-up; ~2-week first prestige; XP levels grant a felt ×1.015 damage each, curve unchanged). Tuned via grid search in `tools/balance_sim.py` v2:

| Knob | Value |
|---|---|
| Training cost | `50 × 1.25^level` (UpgradeCost, finally consumed) |
| Training effect | ×1.05 damage per level, per character |
| Catch-up discount | ×0.5 below roster's highest training level |
| Char level bonus | ×1.015 damage per level |
| Gear budget scaling | ×(1 + Q × 0.015) |
| Boost pricing | minutes of current income (5 / 3 min) |
| CHA gold | +2%/point, wired into the kill payout |

Resulting curve: Q20 ≈ 3s waves (first session), Q40 ≈ 18s, Q60 ≈ 75s, wall (>5 min waves) ≈ Q75-78 at ~15-20 active hours — the prestige hook point. Retune against live play data before ship; prestige lands post-MVP and resets the ladder.
