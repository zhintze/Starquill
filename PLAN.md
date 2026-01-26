# Implementation Plan: Random Equipment Stats

## Goal
Add random stat generation to equipment items created by EquipmentFactory for testing purposes. Stats will display in the inventory panel's selected item info area (already implemented).

## Requirements
- Generate both combat stats (armor, damage) and attribute bonuses (str, dex, con, int, wis, cha)
- Stats are completely random, not based on equipment type
- Simple random value ranges (e.g., 1-5)
- Logic lives in EquipmentFactory with specialized methods

## Implementation Steps

### Step 1: Add random stat generation method to EquipmentFactory
**File:** `scripts/equipment/equipment_factory.gd`

Add a new method `_generate_random_stats() -> Dictionary` that:
- Randomly selects 1-3 stats from the available pool
- Available stats: `armor`, `damage`, `str`, `dex`, `con`, `int`, `wis`, `cha`
- Assigns random values (e.g., 1-5) to each selected stat
- Returns a Dictionary of stats

### Step 2: Add method to apply stats to EquipmentInstance
**File:** `scripts/equipment/equipment_factory.gd`

Add a new method `_apply_stats_to_instance(instance: EquipmentInstance, stats: Dictionary)` that:
- Calls a new internal setter on EquipmentInstance to merge stats
- Or directly sets stats if we add a factory-only method

**File:** `scripts/equipment/equipment_instance.gd`

Add a factory-only method `_set_stat_mods_from_factory(stats: Dictionary)` that:
- Allows EquipmentFactory to set/merge stats after initialization
- Maintains immutability from outside code

### Step 3: Integrate random stats into creation methods
**File:** `scripts/equipment/equipment_factory.gd`

Modify these methods to apply random stats:
- `create_from_catalog()` - after creating instance, apply random stats
- `create_from_handheld_dict()` - after creating instance, apply random stats

### Step 4: Verify inventory display works
The inventory panel already has stat display code at lines 330-340 that formats stats as "+X Stat". Verify this displays correctly with the new random stats.

## Files to Modify
1. `scripts/equipment/equipment_factory.gd` - Add stat generation and application methods
2. `scripts/equipment/equipment_instance.gd` - Add factory-only stat setter method

## Testing
- Start new game or load world test scene
- Open inventory panel
- Select equipment items
- Verify stats display in info panel as "+X Stat, +Y Stat" format
