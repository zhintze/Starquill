# Sprint 9: Quest System Backend — Design

**Date:** 2026-07-01
**Status:** Designed with autonomous defaults (user AFK at progression question; recommended option taken). All decisions listed below are revisable before implementation completes.
**Source:** master design doc §5, `docs/roadmap.md` Sprint 9. Pure logic + tests; UI is Sprint 10.

## Decisions (with rationale)

1. **Sequential ladder + discovery gate.** Each zone is an ordered ladder of 11 quests (design table: 1-3 Normal, 4 Elite, 5-7 Normal, 8 Elite, 9-10 Hard, 11 Boss). The 3% discovery roll on explore wave-clears always offers the NEXT quest in the ladder; one pending offer at a time; boss completion unlocks the next zone. No offer inventory to manage.
2. **No wipe, no timer.** Combat has no party HP (enemies never damage the party), so there is no fail state — consistent with the design's "no hard fail." Retreat is manual. The 3-minute anti-AFK timer is deferred to balance work (Sprint 12).
3. **Retry restarts at wave 1.** Retreat keeps 50% of gold earned during the attempt (the other half is deducted); the retreated quest stays re-enterable until completed (design's retry icon).
4. **Quest completion is the questLevel driver.** `questLevel` currently never increments anywhere. Completing any quest bumps it by 1 (boss +2), which scales explore waves, loot, and the next quest.
5. **Zones live in `quest_zones.json`** (project norm: content in JSON, not ScriptableObjects). The unused `QuestZoneDefinition` ScriptableObject is deleted. MVP ships 2 zones themed on dominant stat types with dialogue strings carried in data for Sprint 10.
6. **Quests assembly stays low in the chain** (references Core + Data only). Reward *specs* are computed in Quests; GameManager executes them (loot rolls need EquipmentFactory, gold needs economy) — avoids Quests→Equipment coupling.

## Data model (`Assets/Scripts/Quests/`, new `Starquill.Quests` asmdef)

- `QuestZone` + `QuestZoneTable`: parsed from `Assets/Resources/Data/quest_zones.json` — id, name, dominant `StatType[]`, dialogue arrays, quest count (11).
- `QuestTier` enum: Normal, Elite, Hard, Boss.
- `QuestSpec` (generated, deterministic): zoneIndex, questIndex, tier, display name, `WaveSpec[]`.
- `WaveSpec`: enemyCount, `StatType[]` enemyTypes (drawn from zone dominants; Hard mixes any), hpMultiplier, isBoss (adds = boss wave has 1 boss at 8x HP + typed adds).
- `QuestRewardSpec`: goldBonus multiplier, lootRolls, `Rarity` floor. Per tier: Normal 1 roll/no floor/1.5x gold · Elite 1 roll/Uncommon/2x · Hard 1 roll/Rare/2.5x + 1 extra no-floor roll · Boss 2 rolls/Rare/4x.

### Generation (`QuestGenerator`)

`Generate(zone, questIndex, questLevel, seed)` — deterministic from (zoneIndex, questIndex, questLevel):
- Tier from the ladder table; wave count per design (Normal 3-5 → index-scaled 4-6 for quests 5-7, Elite 3-4 + mini-boss wave, Hard 5-7, Boss 1 boss wave + 2 lead-in waves).
- Wave HP multiplier ramps within the quest (1.0 → 1.3); enemies per wave 2-4 (+1 at Hard).
- Enemy stat types: weighted to zone dominants (80%), any (20%); Hard forces ≥2 distinct types per wave.

### State machine (`QuestLog`)

```
Idle ──discovery roll──▶ Offered ──Accept──▶ Active(wave n/N) ──last wave──▶ Completed ─▶ Idle (next quest)
  ▲                        │Decline                │Retreat
  └────────────────────────┘                       ▼
                                    Retreated (retry ⇒ Active wave 1) 
```
`QuestLog` holds: zoneIndex, nextQuestIndex, phase, activeWaveIndex, goldEarnedInQuest. Pure C# — no Unity dependencies; all transitions guarded (invalid transitions return false, never throw). Boss completion: zoneIndex++, nextQuestIndex=0 (last zone: stays on final zone, repeatable boss for MVP).

## GameManager integration

- Explore wave-clear (`ExplorationManager.ProcessWaveCleared` already rolls `questDiscoveryRate` and fires `OnQuestDiscovered`): GameManager asks `QuestLog.TryOffer(spec)` and raises `OnQuestOffered(QuestSpec)`.
- `AcceptQuest()` / `DeclineQuest()` / `RetreatQuest()` / `RetryQuest()` public API; `ExplorationManager` state transitions reused (`EnterQuest`, `QuestCompleted`, `QuestRetreated`, `RetryQuest`).
- While `InQuest`, `SpawnWave` consumes `QuestSpec.Waves[activeWaveIndex]` (typed enemies, HP multiplier) instead of the explore formula; wave-clear advances the quest; last wave triggers completion.
- Completion: gold bonus = `GoldPerKill(questLevel) * enemiesInQuest * tier multiplier`; loot rolls via `EquipmentFactory` with rarity floor (roll rarity, clamp up to floor); `questLevel += 1` (boss +2); events `OnQuestCompleted(spec, rewards)` / `OnQuestOffered` / `OnQuestRetreated` for Sprint 10 UI.
- Gold earned during the quest is tracked; retreat deducts half of it.

## Save / load

`SaveData`: `questZoneIndex`, `questNextIndex`, `questPhase` (int), `questActiveWave`, `questGoldEarned`. Offered-but-unanswered offers do not persist (re-roll on next session — cheap and avoids stale offers). Active quests persist and resume at their current wave.

## Testing (EditMode, pure logic)

- `QuestZoneTable`: parses zones, dominant types valid, 11 quests per zone.
- `QuestGenerator`: determinism (same inputs → same spec), tier ladder mapping, wave counts per tier, boss wave shape, HP ramp monotonic, zone-dominant type weighting (statistical over seeds).
- `QuestLog`: every legal transition; every illegal transition rejected; retreat gold math; boss → next zone; retry resets wave.
- Reward spec: per-tier floors/rolls/multipliers; rarity clamp-to-floor logic.
- GameManager-level logic that is testable headless (reward gold math) via extracted pure helpers.

## Out of scope (YAGNI)

Fragments, keys, milestone unlocks (locations/dungeons/blueprints/verbs), dialogue presentation, offline quest progress (design: none), quest timer, party wipe.
