Title
Claude Master Prompt — Starquill “Bounty Bash”-Style Idle RPG Clicker (Unity Mobile)

User Story
As the game/system designer, I want Claude to design an idle RPG clicker clone in the “Bounty Bash” gameplay style using Starquill’s core entities (Species, Character, Equipment, DisplayObjects, Verbs), so that Starquill can be remade into a casual, deterministic, math-heavy, exponentially-scaling loot-focused mobile idle game.

Acceptance Criteria

- Target engine is Unity (mobile-first).
- Godot engine files are deprecated and archived into a separate folder, as we are switching to unity via the unity-idle-clicker-project-guide.md
- Starquill core must remain central: Species, Character, Equipment, DisplayObjects, Verbs.
- Inventory, Status Effects, Combat Resolver, and Factories are documented for later review but explicitly deprecated/set aside for this design iteration.
- Economy is deterministic and math-heavy with exponential idle scaling and casual mobile progression pacing.
- Loot collection is a first-class loop (awesome loot collecting is a core pillar).
- Ads + monetization approach must follow the Unity Idle Clicker Project Guide (LevelPlay mediation, rewarded, interstitial, banner placements, and “Remove Ads” style IAP). :contentReference[oaicite:0]{index=0}
- Claude output is architecture-first (no code yet), includes formulas, tuning knobs, and UI flows.

Checklist

- [ ] Use Unity-first assumptions (UGS-friendly, mobile UX, offline progress).
- [ ] Define the core loop: quest → fight/tap/idle → loot → upgrades → next quest.
- [ ] Map Starquill entities into clicker/idle equivalents without breaking the model.
- [ ] Provide deterministic scaling formulas + example numeric tables.
- [ ] Include ad placement strategy aligned to idle clicker best practices (rewarded primary).
- [ ] Provide data-driven content plan (definitions, tables, ScriptableObject-friendly shapes).
- [ ] Explicitly mark deprecated systems as “document-only” for this pass.

——— BEGIN PROMPT TO CLAUDE ———

You are designing a mobile idle RPG clicker game inspired by “Bounty Bash”-style gameplay, targeting Unity for Android/iOS.

Your job: Reimagine the Starquill RPG into an idle/clicker experience while keeping Starquill’s core entities at the center of the design:

- Species
- Character
- Equipment
- DisplayObjects (visual/representation layer objects)
- Verbs (actions)

The following systems must be documented ONLY for later review, and otherwise set aside unless they are instrumental to the core entities (do not make them central; do not depend on them):

- Inventory
- Status Effects
- Combat Resolver
- Factories
  Treat these as deprecated for this design iteration, as we must rebuild our GOF setup, combat resolution, status effects, and player inventory system in the style of an idle-clicker and for the Unity engine.

Design pillars (must drive every decision):

1) Deterministic, math-heavy economy (no RNG that affects core progression fairness; if RNG exists, confine it to loot variance within deterministic bounds).
2) Casual mobile progression pacing (short sessions feel productive; long-term retention supported).
3) Exponential idle scaling (numbers grow meaningfully; use big-number notation strategy).
4) Awesome loot collecting (loot is exciting, frequent, and meaningful; upgrade decisions feel good, cosmetic display of loot on characters and varying colors is a must).

Monetization + ads must align to this guide’s approach:

- Unity LevelPlay (Unity Ads Mediation / ironSource) as primary ad mediation
- Rewarded video as the main monetization lever (2x idle earnings, boosts, time-skip, bonus chest, etc.)
- Interstitials only at natural breakpoints (prestige reset, major milestone)
- Optional banners on passive screens
- Include IAP plan (consumables, non-consumables like “Remove Ads”, optional VIP sub)
  (Reference: “Unity Idle Clicker Project Guide” content.) :contentReference[oaicite:1]{index=1}

Output requirements (do NOT write code yet):
Produce a complete architecture + systems design document with the following sections, in order:

1) One-Paragraph Game Summary
- The fantasy and what the player does minute-to-minute.
2) Core Loop and Meta Loop
- Core loop: what happens every 5–30 seconds.
- Meta loop: what changes every 5–30 minutes and across days.
- Explicitly list player inputs: taps, selections, upgrades, claim buttons, ad watches.
3) Entity Mapping: Starquill → Idle Clicker
   For each core entity (Species, Character, Equipment, DisplayObjects, Verbs):
- Define its role in the idle game.
- Define what data it must contain.
- Define what it is allowed to influence mathematically.
- Define how it appears in UI.
4) Verbs as the Primary System
   Design Verbs as the foundation of combat/progression without relying on the deprecated combat resolver.
- Explain how Verbs execute in an idle/tick world (tap-triggered, auto-proc, cooldown, priority, etc.).
- Provide an event/tick model that is deterministic.
- Include at least 6 example verb archetypes (basic attack, execute, AoE, buff-like without “status effects”, loot magnet, tempo boost).
  Do NOT introduce “status effects” formally; if you need similar behavior, model it as verb-driven temporary multipliers with clear rules.
5) Bounty Board / Stage Structure (“Bounty Bash” feel)
- Define “bounties” as the progression ladder (stages, tiers, zones, contracts).
- Define enemy/boss cadence and milestone rewards.
- Define fail states (if any) and how idle progression continues.
6) Deterministic Math Model (Core Requirement)
   Provide formulas and tuning knobs:
- Damage/Power model
- Enemy HP scaling (exponential)
- Gold/currency earned per kill/tick
- Loot drop model (bounded RNG allowed only for item rolls; progression must remain fair)
- Upgrade costs and scaling curves
- Offline earnings formula with caps and tuning knobs
  Include:
- Example tables for early/mid/late game values (at least 3 tiers), showing how numbers scale.
- A “knobs list” (10–20 variables designers can tune).
7) Loot System (First-Class)
   Design loot so it feels “awesome”:
- Rarity tiers
- Affix/mod system (if used)
- Deterministic guardrails (pity timers, deterministic chest progression, bounded RNG)
- Equipment progression and replacement cadence
- How loot integrates with Species/Character/Verbs
  Include at least:
- 3 loot acquisition sources (bounty rewards, bosses, ad chest, timed claim)
- 2 “collection goals” systems (codex, sets, museum, crafting-lite) WITHOUT inventory as a dependency (treat it as a collection log if needed).
8) Progression Systems
- Character progression (levels, talent-like choices, verb unlocks)
- Species progression (species traits as passive identity + scaling levers)
- Equipment progression (upgrade, reroll, ascend, merge, etc.)
- Prestige/reset system: propose one, but keep it optional if it complicates the first MVP.
  If prestige is included:
- Define when it unlocks, what resets, what persists, what currency is earned, and the math.
9) UX/UI Flow (Mobile-First)
   Provide screen list + navigation:
- Home/combat screen
- Quest screen
- Loot/equipment screen
- Character/species screen
- Verb management screen
- Dungeon screen
- Location screen
- Shop/IAP screen
- Settings + cloud save
  Include:
- What updates live vs on claim.
- How to avoid UI spam (number popups, dopamine hits, but readable).
- Idle-specific affordances (claim buttons, timers, boosts).
10) Ads + IAP Placement Plan (Aligned to the Guide)
- Rewarded ad placements (primary)
- Interstitial placements (only natural breaks)
- Banner placements (passive screens)
- “Remove Ads” behavior (what it removes, what it does NOT remove)
- Example offer catalog (3–6 items)
- Retention-friendly cadence (don’t punish non-spenders)
11) Data-Driven Content Structure (Unity-Friendly)
    Describe how you would structure data assets (ScriptableObject-friendly) without writing code:
- Suggested asset types for Species, Character definitions, Equipment templates, Verb definitions, Bounty tiers, loot tables, economy config.
- What is static vs remotely configurable (remote config knobs list).
12) MVP Build Plan
- A 2-sprint MVP that proves the loop, loot, and scaling using the unity-idle-clicker-project-guide.md 
- A “later” section that adds our dungeon key system

Hard constraints (do not violate):

- Keep math deterministic; any RNG must be bounded and cannot create progression dead-ends.
- Keep Starquill entities central and explicit (Species/Character/Equipment/DisplayObjects/Verbs).
- Do not propose unnecessary new subsystems; prefer clean GOF-aligned architecture concepts.
- Keep output concise but complete (this is an architecture spec, not a novel).

——— END PROMPT TO CLAUDE ———
