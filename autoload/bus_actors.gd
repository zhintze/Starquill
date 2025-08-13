## BusActors.gd — entities, stats, species, visuals
extends Node

signal actor_spawned(character_id: String, kind: String, data: Dictionary)
signal actor_despawned(character_id: String)
signal stat_changed(character_id: String, stat_id: String, old_value: float, new_value: float)
signal species_changed(character_id: String, species_id: String, skin_color: Color)
signal displayables_changed(character_id: String, pieces: Array)

# Helpers (payloads are skinny, using IDs + value snapshots)
func emit_actor_spawned(character_id: String, kind: String, data: Dictionary) -> void:
	actor_spawned.emit(character_id, kind, data)

func emit_actor_despawned(character_id: String) -> void:
	actor_despawned.emit(character_id)

func emit_stat_changed(character_id: String, stat_id: String, old_value: float, new_value: float) -> void:
	stat_changed.emit(character_id, stat_id, old_value, new_value)

func emit_species_changed(character_id: String, species_id: String, skin_color: Color) -> void:
	species_changed.emit(character_id, species_id, skin_color)

func emit_displayables_changed(character_id: String, pieces: Array) -> void:
	displayables_changed.emit(character_id, pieces)
