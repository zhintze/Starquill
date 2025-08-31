extends Resource
class_name Character

signal model_changed

var id: String
var display_name: String
var skin_color: Color
var stats: Stats
var species: SpeciesInstance

# Equipment slots (instances, not defs)
@export var head:  EquipmentInstance
@export var torso: EquipmentInstance
@export var arms:  EquipmentInstance
@export var legs:  EquipmentInstance
@export var feet:  EquipmentInstance
@export var misc1: EquipmentInstance
@export var misc2: EquipmentInstance
@export var misc3: EquipmentInstance
@export var misc4: EquipmentInstance

# Colors applied to equipment layers that are marked as color-variant in equipment.json
# key: layer_code (int) -> Color
var equipment_layer_colors: Dictionary = {}

# Optional gameplay
var base_verbs: Array[Verb]

# ---- Equipment color assignment (uses ColorManager) ----

func _init() -> void:
	base_verbs = []
	stats = Stats.new()
	stats.changed.connect(_on_stats_changed)

# Forward stats changes
func _on_stats_changed() -> void:
	emit_signal("model_changed")

# Species set (optional: forward species.stats changes)
func set_species(s: SpeciesInstance) -> void:
	if species and species.stats:
		if species.stats.changed.is_connected(_on_stats_changed):
			species.stats.changed.disconnect(_on_stats_changed)
	species = s
	if species and species.stats:
		species.stats.changed.connect(_on_stats_changed)
	emit_signal("model_changed")

enum EquipSlot { HEAD, TORSO, ARMS, LEGS, FEET, MISC1, MISC2, MISC3, MISC4 }

# Back-compat: explicit slot set/get using instances
func set_equipment(slot: int, e: EquipmentInstance) -> void:
	match slot:
		EquipSlot.HEAD:  head = e
		EquipSlot.TORSO: torso = e
		EquipSlot.ARMS:  arms = e
		EquipSlot.LEGS:  legs = e
		EquipSlot.FEET:  feet = e
		EquipSlot.MISC1: misc1 = e
		EquipSlot.MISC2: misc2 = e
		EquipSlot.MISC3: misc3 = e
		EquipSlot.MISC4: misc4 = e
		_: return
	_assign_colors_for_equipment_variants()
	_recalc_stats()
	emit_signal("model_changed")

func get_equipment(slot: int) -> EquipmentInstance:
	match slot:
		EquipSlot.HEAD:  return head
		EquipSlot.TORSO: return torso
		EquipSlot.ARMS:  return arms
		EquipSlot.LEGS:  return legs
		EquipSlot.FEET:  return feet
		EquipSlot.MISC1: return misc1
		EquipSlot.MISC2: return misc2
		EquipSlot.MISC3: return misc3
		EquipSlot.MISC4: return misc4
		_: return null

# Safer API for controllers: route by item_type with misc overflow
func equip_instance(ei: EquipmentInstance) -> bool:
	if ei == null:
		return false
	var slot_name: String = EquipmentCatalog.slot_for_item_type(ei.item_type)
	match slot_name:
		"head":
			if head == null: head = ei
			else: return _equip_misc_overflow(ei)
		"torso":
			if torso == null: torso = ei
			else: return _equip_misc_overflow(ei)
		"arms":
			if arms == null: arms = ei
			else: return _equip_misc_overflow(ei)
		"legs":
			if legs == null: legs = ei
			else: return _equip_misc_overflow(ei)
		"feet":
			if feet == null: feet = ei
			else: return _equip_misc_overflow(ei)
		"misc":
			return _equip_misc_overflow(ei)
		_:
			return _equip_misc_overflow(ei)
	_assign_colors_for_equipment_variants()
	_recalc_stats()
	emit_signal("model_changed")
	return true

func _equip_misc_overflow(ei: EquipmentInstance) -> bool:
	if misc1 == null: misc1 = ei
	elif misc2 == null: misc2 = ei
	elif misc3 == null: misc3 = ei
	elif misc4 == null: misc4 = ei
	else:
		return false
	_assign_colors_for_equipment_variants()
	_recalc_stats()
	emit_signal("model_changed")
	return true

# NOTE: placeholder — later move items to Party inventory
func unequip(slot: int) -> void:
	set_equipment(slot, null)

func clear_equipment() -> void:
	head = null; torso = null; arms = null
	legs = null; feet = null
	misc1 = null; misc2 = null; misc3 = null; misc4 = null
	equipment_layer_colors.clear()
	_recalc_stats()
	emit_signal("model_changed")

func get_all_equipment_instances() -> Array[EquipmentInstance]:
	var out: Array[EquipmentInstance] = []
	if head: out.append(head)
	if torso: out.append(torso)
	if arms: out.append(arms)
	if legs: out.append(legs)
	if feet: out.append(feet)
	for m in [misc1, misc2, misc3, misc4]:
		if m: out.append(m)
	return out

# ---------------- Stats recompute ----------------
func _recalc_stats() -> void:
	if stats == null:
		stats = Stats.new()
	else:
		# reset to a clean Stats each time (no base_stats in this variant)
		stats = Stats.new()

	for ei in get_all_equipment_instances():
		if ei and ei.stats:
			stats.add(ei.stats)

# --------------- Display pieces merge ---------------
func get_display_pieces() -> Array[DisplayPiece]:
	# Use unified DisplayBuilder for all display piece generation
	return DisplayBuilder.build_character_display(self)

# --------------- Color palette + assignment ---------------
func _assign_colors_for_equipment_variants() -> void:
	# Build a set of all layers that need color variance from equipped items
	var needed: Dictionary = {} # layer(int) -> true
	for ei in get_all_equipment_instances():
		var cat: EquipmentCatalog.CatalogItem = StarquillData.get_equipment_by_type(ei.item_type)
		if cat == null:
			continue
		for layer_code in cat.layer_color_variance:
			needed[int(layer_code)] = true

	# Assign any missing colors using ColorManager
	for k in needed.keys():
		var layer_i: int = int(k)
		if equipment_layer_colors.has(layer_i):
			continue
		# Use deterministic color selection based on layer ID
		var col: Color = ColorManager.get_random_color_from_palette("main")
		equipment_layer_colors[layer_i] = col

	# Remove colors for layers no longer needed
	var to_remove: Array = []
	for layer_key in equipment_layer_colors.keys():
		if not needed.has(int(layer_key)):
			to_remove.append(layer_key)
	for rk in to_remove:
		equipment_layer_colors.erase(rk)

# Color management now handled by ColorManager autoload
