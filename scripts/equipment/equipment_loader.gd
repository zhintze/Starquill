# File: res://scripts/equipment/EquipmentLoader.gd
extends Node
class_name EquipmentLoader
# Ensure this script is Autoloaded as:  Name = equipment_loader, Path = this file

# ================================
# Instance registry
# ================================
var all: Array[Equipment] = []
var by_id: Dictionary = {}    # id/name (String) -> Equipment

func _ready() -> void:
	# no-op; explicit loading is preferred
	pass

func clear() -> void:
	all.clear()
	by_id.clear()

func register_equipment(e: Equipment) -> void:
	if e == null: 
		return
	all.append(e)
	var key := String(e.item_type)   # or use e.name if that’s your primary key
	by_id[key] = e

func size() -> int:
	return all.size()

func get_by_index(i: int) -> Equipment:
	if i < 0 or i >= all.size():
		return null
	return all[i]

func get_by_id(id: String) -> Equipment:
	return by_id.get(id, null)

# ================================
# JSON loading
# ================================
func load_from_json(path: String) -> void:
	if not FileAccess.file_exists(path):
		push_warning("EquipmentLoader: JSON not found: %s" % path)
		return
	var text: String = FileAccess.get_file_as_string(path)
	var data: Variant = JSON.parse_string(text)

	if typeof(data) != TYPE_ARRAY:
		push_warning("EquipmentLoader: JSON root must be an array: %s" % path)
		return

	for item_v in data:
		if typeof(item_v) != TYPE_DICTIONARY:
			continue
		var item: Dictionary = item_v
		var e: Equipment = Equipment.new()
		_map_dict_to_equipment(item, e)
		register_equipment(e)

# Map JSON dictionary → Equipment resource
func _map_dict_to_equipment(d: Dictionary, e: Equipment) -> void:
	if d.has("id"):
		e.item_type = String(d.id)
	elif d.has("name"):
		e.item_type = String(d.name)

	if d.has("layer_codes"):
		e.layer_codes = PackedInt32Array(d.layer_codes)

	if d.has("layer_color_variance"):
		e.layer_color_variance = PackedInt32Array(d.layer_color_variance)

	if d.has("modular_max_by_part"):
		e.modular_max_by_part = d.modular_max_by_part

# ================================
# Instance creation helpers
# ================================
func create_instance_from_id(id: String) -> EquipmentInstance:
	var e := get_by_id(id)
	if e == null: 
		return null
	var inst := EquipmentInstance.new()
	inst.from_equipment(e)
	return inst
