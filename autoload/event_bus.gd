extends Node
# EventBus autoload - no class_name to avoid singleton conflict

# ============================================
# FLOW / SCENE SIGNALS (from BusFlow)
# ============================================
signal boot_completed()
signal level_will_change(from_level_id: String, to_level_id: String)
signal level_loaded(level_id: String, root: Node)
signal level_unloaded(level_id: String)

# ============================================
# PARTY SIGNALS (from BusParty)
# ============================================
# Movement
signal party_moved(from_tile: Vector2i, to_tile: Vector2i)
signal party_movement_started(path: Array[Vector2i])
signal party_movement_completed()
signal party_movement_blocked(reason: String)
signal party_teleported(to_tile: Vector2i)

# Members
signal party_member_added(character: Character, position: int)
signal party_member_removed(character: Character)
signal party_member_swapped(position_a: int, position_b: int)
signal party_size_changed(new_size: int)
signal party_leader_changed(new_leader: Character)

# State
signal party_data_saved(data: Dictionary)
signal party_data_loaded(data: Dictionary)

# ============================================
# INVENTORY SIGNALS (from BusInventory)
# ============================================
signal inventory_opened()
signal inventory_closed()
signal item_added(item_data: Variant, quantity: int)
signal item_removed(item_data: Variant, quantity: int)
signal item_used(item_data: Variant, target: Character)
signal equipment_equipped(character: Character, slot: String, equipment: EquipmentInstance)
signal equipment_unequipped(character: Character, slot: String, equipment: EquipmentInstance)

# ============================================
# UI SIGNALS (from BusUi)
# ============================================
signal modal_opened(modal_name: StringName)
signal modal_closed(modal_name: StringName)
signal tooltip_requested(text: String, position: Vector2)
signal tooltip_hidden()
signal notification_shown(message: String, type: String)

# ============================================
# COMBAT SIGNALS (from BusCombat)
# ============================================
signal combat_started(enemies: Array)
signal combat_ended(victory: bool)
signal turn_started(actor: Variant)
signal turn_ended(actor: Variant)
signal damage_dealt(attacker: Variant, target: Variant, amount: int)

# ============================================
# ACTOR SIGNALS (from BusActors)
# ============================================
signal actor_spawned(actor: Variant)
signal actor_despawned(actor: Variant)
signal actor_died(actor: Variant)
signal stat_changed(actor: Variant, stat: String, old_value: int, new_value: int)

# ============================================
# WORLD SIGNALS (from BusWorld)
# ============================================
signal world_created(world_name: String, seed: int)
signal world_saved(world_name: String)
signal tile_modified(pos: Vector2i, tile: Tile)
signal location_entered(location_data: LocationData)
signal visibility_updated(visible_tiles: Array[Vector2i])

# Debug signals
signal debug_teleport(tile_pos: Vector2i)
signal debug_reveal_map(radius: int)

# ============================================
# SAVE/LOAD SIGNALS (from BusSave)
# ============================================
signal save_started(slot: int)
signal save_completed(slot: int, success: bool)
signal load_started(slot: int)
signal load_completed(slot: int, success: bool)

# ============================================
# AUDIO SIGNALS (from BusAudio)
# ============================================
signal music_changed(track: String)
signal sfx_played(sound: String)
signal volume_changed(bus: String, volume: float)
