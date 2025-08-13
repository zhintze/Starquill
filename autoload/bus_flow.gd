## BusFlow.gd — levels, checkpoints, transitions
extends Node

signal boot_completed()
signal level_will_change(from_level_id: String, to_level_id: String)
signal level_loaded(level_id: String, root: Node)
signal level_unloaded(level_id: String)

# Helpers
func emit_boot_completed() -> void:
	boot_completed.emit()

func emit_level_will_change(from_level_id: String, to_level_id: String) -> void:
	level_will_change.emit(from_level_id, to_level_id)

func emit_level_loaded(level_id: String, root: Node) -> void:
	level_loaded.emit(level_id, root)

func emit_level_unloaded(level_id: String) -> void:
	level_unloaded.emit(level_id)
