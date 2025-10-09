extends Node

# Party movement signals
signal party_moved(from_tile: Vector2i, to_tile: Vector2i)
signal party_movement_started(path: Array[Vector2i])
signal party_movement_completed()
signal party_movement_blocked(reason: String)
signal party_teleported(to_tile: Vector2i)

# Formation signals
signal formation_changed(new_formation)  # FormationType enum
signal formation_position_changed(character_id: String, position: int)
signal party_leader_changed(new_leader_id: String)

# Party member signals
signal member_added(character, position: int)  # Character type
signal member_removed(character)  # Character type
signal member_swapped(position_a: int, position_b: int)
signal party_size_changed(new_size: int)

# Party state signals
signal party_resting()
signal party_camping()
signal party_in_combat()
signal party_exploring()

# Interaction signals
signal party_interacted_with_tile(tile_position: Vector2i, tile)  # Tile type
signal party_interacted_with_npc(npc_id: String)
signal party_interacted_with_object(object_id: String)

# Path management signals
signal path_calculated(path: Array[Vector2i], destination: Vector2i)
signal path_recalculated(new_path: Array[Vector2i])
signal path_blocked(blocking_position: Vector2i)
signal path_cleared()

# Movement input signals
signal movement_input_tap(world_position: Vector2i)
signal movement_input_swipe(direction: Vector2i)  # Normalized to cardinal directions
signal movement_input_cancelled()

# Party stats signals
signal party_speed_changed(new_speed: float)
signal party_visibility_changed(new_radius: int)
signal party_stealth_changed(is_stealthy: bool)

# Save/Load signals
signal party_data_saved(data: Dictionary)
signal party_data_loaded(data: Dictionary)

# Helper properties
var current_position: Vector2i = Vector2i.ZERO
var current_formation = 0  # FormationType.LINE - will be set from WorldConstants
var party_members: Array = []  # Array of Character objects
var is_moving: bool = false
var current_path: Array[Vector2i] = []

# Helper functions
func get_party_leader():  # Returns Character or null
	if party_members.size() > 0:
		return party_members[0]
	return null

func get_party_size() -> int:
	return party_members.size()

func is_party_full() -> bool:
	return party_members.size() >= WorldConstants.MAX_PARTY_SIZE

func get_member_at_position(position: int):  # Returns Character or null
	if position >= 0 and position < party_members.size():
		return party_members[position]
	return null

func get_formation_offsets() -> Array[Vector2i]:
	var offsets: Array[Vector2i] = []
	match current_formation:
		WorldConstants.FormationType.LINE:
			offsets = [
				Vector2i(0, 0),   # Leader
				Vector2i(0, 1),   # Member 2
				Vector2i(0, 2),   # Member 3
				Vector2i(0, 3)    # Member 4
			]
		WorldConstants.FormationType.SQUARE:
			offsets = [
				Vector2i(0, 0),   # Leader
				Vector2i(1, 0),   # Member 2
				Vector2i(0, 1),   # Member 3
				Vector2i(1, 1)    # Member 4
			]
		WorldConstants.FormationType.DIAMOND:
			offsets = [
				Vector2i(0, 0),   # Leader
				Vector2i(-1, 1),  # Member 2
				Vector2i(1, 1),   # Member 3
				Vector2i(0, 2)    # Member 4
			]
		WorldConstants.FormationType.WEDGE:
			offsets = [
				Vector2i(0, 0),   # Leader
				Vector2i(-1, 1),  # Member 2
				Vector2i(1, 1),   # Member 3
				Vector2i(0, 1)    # Member 4
			]
	return offsets

func emit_movement_event(from: Vector2i, to: Vector2i) -> void:
	current_position = to
	party_moved.emit(from, to)

func start_movement(path: Array[Vector2i]) -> void:
	if not is_moving and path.size() > 0:
		is_moving = true
		current_path = path
		party_movement_started.emit(path)

func complete_movement() -> void:
	is_moving = false
	current_path.clear()
	party_movement_completed.emit()

func log_party_event(message: String, level: String = "info") -> void:
	if WorldConstants.DEBUG_MODE:
		print("[BusParty][%s] %s" % [level.to_upper(), message])