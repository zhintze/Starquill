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
	_assign_colors_for_equipment_variants()

	# (1) Equipment → pieces + hidden species layers
	var equip_res: EquipmentDisplayBuilder.EquipmentDisplayResult = EquipmentDisplayBuilder.build_for_character(self)
	var equip_pieces: Array[DisplayPiece] = equip_res.pieces
	var hidden: PackedInt32Array = equip_res.hidden_species_layers

	# Apply color-variance tints to equipment pieces (CharacterDisplay reads 'modulate')
	if not equipment_layer_colors.is_empty():
		for i in equip_pieces.size():
			var dp: DisplayPiece = equip_pieces[i]
			var layer_i: int = int(dp.layer)
			if equipment_layer_colors.has(layer_i):
				var tint_col: Color = equipment_layer_colors[layer_i]
				dp.modulate = (dp.modulate * tint_col) if dp.modulate != Color(1,1,1,1) else tint_col
				equip_pieces[i] = dp

	# (2) Species via your proven method, then filter hidden locally
	var sp_disp := SpeciesDisplayable.new(species)
	var species_pieces: Array[DisplayPiece] = sp_disp.get_display_pieces()

	if not hidden.is_empty():
		var hidden_set := {}
		for h in hidden:
			hidden_set[int(h)] = true
		var keep = func(p: DisplayPiece) -> bool:
			return not hidden_set.has(int(p.layer))
		species_pieces = species_pieces.filter(keep)

	# (3) Merge + sort by layer
	var all_pieces: Array[DisplayPiece] = []
	all_pieces.append_array(species_pieces)
	all_pieces.append_array(equip_pieces)

	var cmp = func(a: DisplayPiece, b: DisplayPiece) -> bool:
		return a.layer < b.layer
	all_pieces.sort_custom(cmp)

	# Debug (remove later): show counts and a peek at hidden list
	print("[Character] species=", species_pieces.size(), " equip=", equip_pieces.size(), " hidden=", hidden.size(), " total=", all_pieces.size())
	return all_pieces

# --------------- Color palette + assignment ---------------
func _assign_colors_for_equipment_variants() -> void:
	# Build a set of all layers that need color variance from equipped items
	var needed: Dictionary = {} # layer(int) -> true
	for ei in get_all_equipment_instances():
		var cat: EquipmentCatalog.CatalogItem = equipment_catalog.by_type.get(ei.item_type, null) as EquipmentCatalog.CatalogItem
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
