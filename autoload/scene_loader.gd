## SceneLoader.gd — persistent scene/level swapping; emits BusFlow signals
extends Node

var _current_level_id: String = ""
var _current_root: Node = null

@export var level_holder_path: NodePath = ^"/root/Main/LevelHolder"

func boot_done() -> void:
	BusFlow.emit_boot_completed()

func swap_to(level_id: String, packed: PackedScene) -> void:
	var holder: Node = get_node_or_null(level_holder_path)
	if holder == null:
		push_error("SceneLoader: LevelHolder not found at %s" % level_holder_path)
		return

	var from_id := _current_level_id
	if from_id != "":
		BusFlow.emit_level_will_change(from_id, level_id)
	else:
		BusFlow.emit_level_will_change("", level_id)

	if is_instance_valid(_current_root):
		holder.remove_child(_current_root)
		_current_root.queue_free()
		BusFlow.emit_level_unloaded(from_id)

	_current_root = packed.instantiate()
	_current_level_id = level_id
	holder.add_child(_current_root)
	BusFlow.emit_level_loaded(level_id, _current_root)
