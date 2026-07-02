#!/usr/bin/env python3
"""Starquill balance simulation — models player power vs enemy scaling.

All formulas transcribed from code (EconomyConfig, DamageCalculator,
CombatTickProcessor, EquipmentFactory, CharacterInstance). Run to regenerate
the pacing table after balance changes: python3 tools/balance_sim.py
"""

import math

# --- EconomyConfig ---
BASE_HP, HP_GROWTH = 50.0, 0.12
BASE_GOLD, GOLD_GROWTH = 5.0, 0.10
AUTO_ATK_FRACTION = 0.3
CHAR_XP_BASE = 3

# --- Equipment (EquipmentFactory) ---
RARITY_BUDGET = {0: 5, 1: 8.5, 2: 12.5, 3: 16.5, 4: 20.5}  # midpoints C..L
ABILITY_POTENCY = {0: 3 * 0.8, 1: 3 * 1.0, 2: 3 * 1.2, 3: 3 * 1.5, 4: 3 * 2.0}  # base at L1


def rarity_weights(q):
    u = 15 + q * 0.5
    r = 3 + q * 0.3
    e = 0.5 + q * 0.1
    l = 0.05 + q * 0.02
    c = max(100 - u - r - e - l, 10)
    total = c + u + r + e + l
    return [c / total, u / total, r / total, e / total, l / total]


def expected_item_budget(q):
    w = rarity_weights(q)
    return sum(w[i] * RARITY_BUDGET[i] for i in range(5))


def party_top_stat(char_level, q):
    """One character's highest stat: base ~8, +~1/level into the top stat
    (weighted allocation), + gear share. 11 items; a stat pair spreads across
    6 stats but prefix pools bias; assume ~30% of total gear budget lands in
    the top stat, + ~4 ability potency average."""
    base = 8
    from_levels = char_level * 1.0
    gear_total = 11 * expected_item_budget(q) + 11 * 4  # budget + abilities
    from_gear = gear_total * 0.30
    return base + from_levels + from_gear


def party_dps(char_level, q):
    top = party_top_stat(char_level, q)
    auto = 4 * top * AUTO_ATK_FRACTION  # per tick (1s)
    # One verb tap every ~4s (3s lock): base 50, statMult 1 + stat*0.1,
    # avg advantage ~1.1
    verb = 50 * (1 + top * 0.1) * 1.1 / 4
    return auto + verb


def wave_hp(q):
    count = min(2 + q // 10, 6)
    return count * BASE_HP * (1 + HP_GROWTH) ** q, count


def xp_to_level(l):
    return int(100 * 1.18 ** l)


def simulate():
    print(f"{'Q':>3} {'lvl':>4} {'waveHP':>10} {'DPS':>7} {'s/wave':>7} "
          f"{'gold/kill':>9} {'hrs@Q':>6} {'cum-hrs':>8}")

    char_level = 1
    xp = 0.0
    cum_hours = 0.0

    for q in range(1, 81):
        hp, count = wave_hp(q)
        dps = party_dps(char_level, q)
        secs = hp / dps
        gold_kill = BASE_GOLD * (1 + GOLD_GROWTH) ** q

        # Quest pace: ~1 quest per ~14 explore waves (travel bar 20 waves,
        # 3% rolls shorten) + quest's own ~4.5 waves.
        waves_this_q = 14 + 4.5
        hours_at_q = waves_this_q * secs / 3600
        cum_hours += hours_at_q

        # XP during those waves: kills*(3+Q) each of 4+bench
        kills = waves_this_q * count
        xp += kills * (CHAR_XP_BASE + q)
        while xp >= xp_to_level(char_level):
            xp -= xp_to_level(char_level)
            char_level += 1

        if q <= 10 or q % 5 == 0:
            print(f"{q:>3} {char_level:>4} {hp:>10.0f} {dps:>7.0f} {secs:>7.1f} "
                  f"{gold_kill:>9.0f} {hours_at_q:>6.2f} {cum_hours:>8.1f}")


if __name__ == "__main__":
    simulate()
