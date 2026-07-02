#!/usr/bin/env python3
"""Starquill balance simulation v2 — post balance-pass (Training ladder,
quest-scaled gear, CHA gold, level damage bonus). Rerun after knob changes:
python3 tools/balance_sim.py"""

import math

BASE_HP, HP_GROWTH = 50.0, 0.12
BASE_GOLD, GOLD_GROWTH = 5.0, 0.10
AUTO_ATK_FRACTION = 0.3
CHAR_XP_BASE = 3
TRAIN_BASE_COST, TRAIN_COST_GROWTH = 50.0, 0.25
TRAIN_DMG = 0.05
LEVEL_DMG = 0.015
GEAR_PER_LEVEL = 0.015

RARITY_BUDGET = {0: 5, 1: 8.5, 2: 12.5, 3: 16.5, 4: 20.5}


def rarity_weights(q):
    u, r, e, l = 15 + q * 0.5, 3 + q * 0.3, 0.5 + q * 0.1, 0.05 + q * 0.02
    c = max(100 - u - r - e - l, 10)
    t = c + u + r + e + l
    return [c / t, u / t, r / t, e / t, l / t]


def expected_item_budget(q):
    w = rarity_weights(q)
    base = sum(w[i] * RARITY_BUDGET[i] for i in range(5))
    return base * (1 + q * GEAR_PER_LEVEL)


def top_stat(char_level, q):
    gear_total = 11 * expected_item_budget(q) + 11 * 4
    return 8 + char_level * 1.0 + gear_total * 0.30


def cha_sum(char_level, q):
    return 4 * (top_stat(char_level, q) * 0.4)


def party_dps(char_level, q, train_level):
    top = top_stat(char_level, q)
    mult = (1 + TRAIN_DMG) ** train_level * (1 + LEVEL_DMG) ** char_level
    auto = 4 * top * AUTO_ATK_FRACTION * mult
    verb = 50 * (1 + top * 0.1) * 1.1 / 4 * mult
    return auto + verb


def wave_hp(q):
    count = min(2 + q // 10, 6)
    return count * BASE_HP * (1 + HP_GROWTH) ** q, count


def xp_to_level(l):
    return int(100 * 1.18 ** l)


def train_cost(level):
    return TRAIN_BASE_COST * (1 + TRAIN_COST_GROWTH) ** level


def simulate():
    print(f"{'Q':>3} {'lvl':>4} {'train':>5} {'waveHP':>10} {'DPS':>9} "
          f"{'s/wave':>7} {'hrs@Q':>6} {'cum-hrs':>8}")

    char_level, xp = 1, 0.0
    train = 0          # per-character train level (4 equal ladders)
    gold = 0.0
    cum_hours = 0.0

    for q in range(1, 81):
        hp, count = wave_hp(q)
        dps = party_dps(char_level, q, train)
        secs = hp / dps

        waves_this_q = 18.5
        hours = waves_this_q * secs / 3600
        cum_hours += hours

        kills = waves_this_q * count
        cha_bonus = cha_sum(char_level, q) * 0.02
        gold += kills * BASE_GOLD * (1 + GOLD_GROWTH) ** q * (1 + cha_bonus)

        # Spend ~70% of gold on training, 4 ladders round-robin
        budget = gold * 0.6
        while budget >= 4 * train_cost(train):
            budget -= 4 * train_cost(train)
            gold -= 4 * train_cost(train)
            train += 1

        xp += kills * (CHAR_XP_BASE + q)
        while xp >= xp_to_level(char_level):
            xp -= xp_to_level(char_level)
            char_level += 1

        if q <= 10 or q % 5 == 0:
            print(f"{q:>3} {char_level:>4} {train:>5} {hp:>10.0f} {dps:>9.0f} "
                  f"{secs:>7.1f} {hours:>6.2f} {cum_hours:>8.1f}")


if __name__ == "__main__":
    simulate()
