# Starquill: Gameplay Loop & Systems Overview

**Audience:** product manager / commissioner
**Date:** 2026-07-01 · reflects the build as implemented (344 automated tests passing) plus locked-in design decisions. Anything not yet playable is explicitly marked.

## The pitch

Starquill is a mobile idle RPG for Android. A party of four hand-drawn "paper doll" characters battles endless waves of monsters on its own; the player's job is the fun part — firing abilities at the right moment, deciding which loot to wear, and choosing when to take on quests. Every piece of equipment is visible on the character wearing it, so getting stronger always *looks* like something. The game respects the player's absence: the party keeps earning while the app is closed.

## The core loop (every 5–30 seconds)

1. **The party fights by itself.** Four characters auto-attack the current enemy wave once per second.
2. **The player fires Verbs.** Three ability cards (e.g. Fireball, Slash, Heal) rotate through a shared hand; tapping one unleashes it. Every ability and every enemy has one of six classic stats (STR, DEX, CON, INT, WIS, CHA) arranged in two rock-paper-scissors triangles — Physical: STR beats DEX beats CON; Mental: INT beats WIS beats CHA. Matching the right ability to the right enemy is the core skill.
3. **Kills pay out.** Gold always; equipment ~15% of the time (with "pity" guarantees so rare items can never dry up forever: an Uncommon within 50 kills, up to a Legendary within 5,000).
4. **Loot changes the doll.** Equipping an item immediately redraws it on the character — hat, armor, weapon, all of it.

## The session loop (every few minutes)

- A **travel bar** under the gold counter fills as waves are cleared. Quests can be discovered randomly at any wave (3%), and the bar *guarantees* one on arrival (every ~20 waves) — the player is never starved of the next goal.
- A discovery pops a **banner**; tapping it shows the quest: name, difficulty tier, wave count, reward preview, and a line of zone flavor dialogue. Accept or decline.
- **Quests are the structured challenge.** Each zone is a ladder of 11 quests — Normals, two Elites (mini-boss), two Hards (mixed enemy types), then the **Zone Boss**. Enemies are themed to the zone (Verdant Hollow: brawny beasts; Gloomspire Ruins: arcane horrors). During a quest, the travel bar becomes a wave-progress bar.
- **No fail state.** The player can retreat anytime, keeping half the gold earned in the attempt, and retry later at no further cost — the quest waits.
- **Completion pays curated rewards:** a gold bonus and guaranteed-quality items (Elites guarantee Uncommon+, Hards and Bosses guarantee Rare+), revealed on a reward sheet with the zone's dialogue. Beating the boss unlocks the next zone.
- Completing quests is what raises **quest level** — the single dial that scales enemy strength, gold value, and loot quality everywhere. Quests are the engine of progression; exploring is the flywheel between them.

## The meta loop (across days)

- **Equipment depth.** Every item carries a primary + secondary stat and most carry an **awakened ability** — a passive bonus that levels up over time while worn, or instantly for gold. Rarity governs everything: stat budget, ability ceiling, sell value.
- **Party building.** The player owns a roster of characters across species, fields four, and swaps them to cover stat matchups. Characters gain XP from kills and quests and level up, granting stat points weighted toward their species strengths; benched roster members earn a half share so nobody stagnates.
- **Offline earnings.** The party keeps exploring while away at 50% efficiency, capped at 8 hours; a "Welcome back" claim sheet presents the haul on return — doubled by watching an ad.
- **The shop** sells timed boosts (auto-firing Verbs, faster ability rotation) and a free 4-hour chest, ad-doubleable.
- **Monetization is rewarded-ad-first and never pay-to-win** *(ships in MVP)*: watch an ad to double offline earnings or the timed chest; a one-time Remove Ads purchase. Interstitials appear only at quest completion and are removed by the purchase. If a reward arrives while the bag is full, it waits in a mailbox — never lost.

## How the systems interlock

Combat produces gold and loot → loot raises stats (and its abilities grow while worn) → stronger parties clear quests → quests raise quest level → everything scales up, demanding better loot and smarter stat coverage → which quests provide. Gold is the pressure valve: spent on ability level-ups today, shop boosts next sprint, with item-selling (single tap, or "Optimize All" to auto-equip the whole roster) recycling the inventory's 50 slots.

## Where the game is headed (designed, not yet built)

- **Destinations** — visible today as a teaser section on the Quests screen with a live "fragments" counter. Post-MVP it becomes the dungeon-key system: color-themed keys spent on targeted-loot **Encounters**, time-limited discovered **Locations**, and multi-zone **Dungeons**.
- **Prestige** — a costly reset for permanent multipliers (Stellar Ink). All economy formulas already carry the multiplier, stubbed at 1.0.
- **Verb collection** — abilities currently come with the character; the design adds Verb drops and a collection layer.

## Current status & honest gaps

| | |
|---|---|
| Playable now | Explore combat, Verbs, full loot/equip/sell loop, paper-doll rendering, party management, complete quest loop (discover → accept → fight → complete/retreat → boss → next zone), save/load |
| Sprint 12 (next) | Tutorial, animation/juice, balance pass, second full UI pass, real IAP store + ad IDs, interstitials, ship |
| Known gaps | Art is placeholder-flat outside the character/paper-doll work (Full UI Pass 2). Ad/IAP run against test/mock backends until store accounts are wired at ship. |
