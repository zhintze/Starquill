# EquipmentFactory.gd
extends Node
class_name EquipmentFactory

static func create_instance(equip_id: String) -> EquipmentInstance:
	var def := equipment_loader.get_by_id(equip_id)
	if def == null:
		push_error("EquipmentFactory: unknown equipment id '%s'" % equip_id)
		return null
	var ei := EquipmentInstance.new()
	ei.def = def
	# Resolve modular numbers for any parts that declare a max
	for part in def.modular_max_by_part.keys():
		var max_n := int(def.modular_max_by_part[part])
		if max_n > 0:
			# Pick deterministically? Here: random for now; later seed by character id
			var rng := RandomNumberGenerator.new()
			rng.randomize()
			ei.resolved_image_nums[part] = rng.randi_range(1, max_n)
	return ei

static func equip_if_allowed(ch: Character, ei: EquipmentInstance) -> bool:
	if ch == null or ei == null or ei.def == null:
		return false
	# basic species restriction
	var sid := ch.species.species_id if ch.species and ch.species.species_id != "" else (ch.species.base.name if ch.species and ch.species.base else "")
	if ei.def.restricted_species.has(sid):
		return false
	# basic item restriction (from species)
	for tag in ei.def.restricted_item_types:
		if ch.species and ch.species.itemRestrictions.has(tag):
			return false
	# OK — place in slot
	_set_slot(ch, ei)
	return true

static func _set_slot(ch: Character, ei: EquipmentInstance) -> void:
	match ei.def.slot:
		Equipment.Slot.HEAD:  ch.head = ei
		Equipment.Slot.TORSO: ch.torso = ei
		Equipment.Slot.ARMS:  ch.arms = ei
		Equipment.Slot.LEGS:  ch.legs = ei
		Equipment.Slot.FEET:  ch.feet = ei
		_:                      ch.misc1 = ei  # fallback; adjust to your layout
	ch.emit_signal("model_changed")
