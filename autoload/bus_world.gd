extends Node

# Chunk management signals
signal chunk_loaded(chunk_position: Vector2i, chunk_data)  # Chunk type
signal chunk_unloaded(chunk_position: Vector2i)
signal chunk_modified(chunk_position: Vector2i)

# Tile signals
signal tile_revealed(tile_position: Vector2i)
signal tile_hidden(tile_position: Vector2i)
signal tile_modified(tile_position: Vector2i, tile)  # Tile type
signal tile_clicked(tile_position: Vector2i)
signal tile_hovered(tile_position: Vector2i)

# Location signals
signal location_discovered(location_data)  # LocationData type
signal location_entered(location_data)  # LocationData type
signal location_exited(location_data)  # LocationData type
signal location_spawned(world_position: Vector2i, location_data)  # LocationData type

# Biome signals
signal biome_entered(biome_type, biome_name: String)  # BiomeType enum
signal biome_exited(biome_type, biome_name: String)  # BiomeType enum
signal biome_generated(biome_center: Vector2i, biome_type)  # BiomeType enum

# World generation signals
signal world_generation_started(seed: int, size: Vector2i)
signal world_generation_progress(percent: float)
signal world_generation_completed()
signal world_chunk_generated(chunk_position: Vector2i)

# Fog of war signals
signal visibility_updated(visible_tiles: Array[Vector2i])
signal fog_cleared(tile_position: Vector2i, radius: int)

# Random encounter signals
signal encounter_triggered(tile_position: Vector2i, encounter_data: Dictionary)
signal encounter_chance_rolled(success: bool, chance: float)

# World state signals
signal world_loaded(world_name: String)
signal world_saved(world_name: String)
signal world_created(world_name: String, seed: int)

# Camera signals
signal camera_moved(new_position: Vector2)
signal camera_zoomed(zoom_level: float)
signal camera_mode_changed(mode: String)  # "follow", "free"

# Debug signals
signal debug_mode_toggled(enabled: bool)
signal debug_teleport(tile_position: Vector2i)
signal debug_reveal_map(radius: int)

# Helper functions
func emit_tile_interaction(tile_pos: Vector2i, interaction_type: String) -> void:
	match interaction_type:
		"click":
			tile_clicked.emit(tile_pos)
		"hover":
			tile_hovered.emit(tile_pos)

func emit_location_event(event_type: String, location) -> void:  # LocationData type
	match event_type:
		"discovered":
			location_discovered.emit(location)
		"entered":
			location_entered.emit(location)
		"exited":
			location_exited.emit(location)

func log_world_event(message: String, level: String = "info") -> void:
	if WorldConstants.DEBUG_MODE:
		print("[BusWorld][%s] %s" % [level.to_upper(), message])