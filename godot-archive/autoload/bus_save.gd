## BusSave.gd — save/load/checkpoints
extends Node

signal save_requested(slot: int)
signal save_completed(slot: int, ok: bool)
signal load_requested(slot: int)
signal load_completed(slot: int, ok: bool, snapshot: Dictionary)

# Helpers
func request_save(slot: int) -> void:
	save_requested.emit(slot)

func complete_save(slot: int, ok: bool) -> void:
	save_completed.emit(slot, ok)

func request_load(slot: int) -> void:
	load_requested.emit(slot)

func complete_load(slot: int, ok: bool, snapshot: Dictionary) -> void:
	load_completed.emit(slot, ok, snapshot)
