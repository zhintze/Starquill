extends Node
class_name EquipmentFactory
# Consider setting this as an Autoload singleton if multiple scenes use it.

var _main_palette: PackedStringArray = PackedStringArray()

# Removed: now uses ColorManager singleton instead of direct CSV access

func _ready() -> void:
	randomize()
	_main_palette = _load_palette()

# -------------------------------
# PUBLIC: Single creation entry points
# -------------------------------

# Create an EquipmentInstance from a specific catalog item.
func create_from_catalog(cat: EquipmentCatalog.CatalogItem, item_num: int = -1) -> EquipmentInstance:
	if cat == null:
		push_error("EquipmentFactory.create_from_catalog: null catalog item")
		return null

	var amt: int = max(1, cat.amount)
	var chosen_num: int = item_num if item_num > 0 else ((randi() % amt) + 1)

	var ei := EquipmentInstance.new()
	ei._init_from_catalog(cat, chosen_num, _main_palette)
	return ei

# Create a random EquipmentInstance from a slot prefix (e.g., "hd","tr","ar","lg","fe","mc","w").
func create_random_from_prefix(prefix: String) -> EquipmentInstance:
	var bucket: Array = StarquillData.get_equipment_by_slot_prefix(prefix)
	if bucket.is_empty():
		return null

	var cat: EquipmentCatalog.CatalogItem = bucket[randi() % bucket.size()] as EquipmentCatalog.CatalogItem
	if cat == null:
		return null

	return create_from_catalog(cat)

# Create random equipment with slot-specific restrictions
func create_random_from_prefix_restricted(prefix: String) -> EquipmentInstance:
	var bucket: Array = StarquillData.get_equipment_by_slot_prefix(prefix)
	if bucket.is_empty():
		return null
	
	# Apply restrictions for specific slots
	var filtered_bucket: Array = []
	for item in bucket:
		var catalog_item = item as EquipmentCatalog.CatalogItem
		if catalog_item == null:
			continue
			
		match prefix:
			"tr":
				# Only tr01-tr06 allowed for torso slot randomization
				var item_num = int(catalog_item.item_type.substr(2))
				if item_num >= 1 and item_num <= 6:
					filtered_bucket.append(catalog_item)
			"lg":
				# Only lg01-lg02 allowed for legs slot randomization  
				var item_num = int(catalog_item.item_type.substr(2))
				if item_num >= 1 and item_num <= 2:
					filtered_bucket.append(catalog_item)
			_:
				# No restrictions for other prefixes
				filtered_bucket.append(catalog_item)
	
	if filtered_bucket.is_empty():
		return null
		
	var selected_item = filtered_bucket[randi() % filtered_bucket.size()] as EquipmentCatalog.CatalogItem
	return create_from_catalog(selected_item)

# Fill a character with a randomized baseline + extras
# 
# Randomization Restrictions (Option B):
# - Torso slot: Only tr01-tr06 can be randomly equipped to torso slot
# - Legs slot: Only lg01-lg02 can be randomly equipped to legs slot  
# - tr07+ and lg03+ items are excluded from their natural slots during randomization
# - tr07+ and lg03+ items CAN still be randomly selected for misc slots
# - All items remain manually equippable to any compatible slot
# - Character randomizer fills only 2 misc slots (not all 4 available)
func equip_random_set(ch: Character, extras: int = 2) -> void:
	if ch == null:
		return
	ch.clear_equipment()

	# Apply equipment chances for main slots
	var main_prefixes: Array[String] = ["hd", "tr", "ar", "lg", "fe"]
	for p in main_prefixes:
		# Check if this prefix should be equipped based on its chance
		if randf() <= StarquillData.get_equipment_prefix_chance(p):
			var ei := create_random_from_prefix_restricted(p)
			if ei != null:
				ch.equip_instance(ei)

	# Apply equipment chances and priorities for misc slots
	var pool: Array[String] = ["mc", "hd", "tr", "ar", "lg", "fe"]
	for _i in extras:
		var selected_prefix = _select_prefix_by_priority_and_chance(pool)
		if selected_prefix != "":
			var ei2 := create_random_from_prefix(selected_prefix)
			if ei2 != null:
				# Force all extras into misc slots to ensure variety
				_force_equip_to_misc_slot(ch, ei2)

	if ch.has_signal("equipment_changed"):
		ch.emit_signal("equipment_changed")

func _select_prefix_by_priority_and_chance(pool: Array[String]) -> String:
	# Filter prefixes that pass their chance check
	var viable_prefixes: Array[String] = []
	var total_weight: float = 0.0
	
	for prefix in pool:
		# Check if this prefix passes its chance check
		if randf() <= StarquillData.get_equipment_prefix_chance(prefix):
			viable_prefixes.append(prefix)
			total_weight += StarquillData.get_equipment_prefix_misc_priority(prefix)
	
	if viable_prefixes.is_empty():
		return ""  # No prefixes passed their chance checks
	
	# Select based on weighted priority
	var roll = randf() * total_weight
	var accumulated_weight: float = 0.0
	
	for prefix in viable_prefixes:
		accumulated_weight += StarquillData.get_equipment_prefix_misc_priority(prefix)
		if roll <= accumulated_weight:
			return prefix
	
	# Fallback to last prefix (shouldn't happen)
	return viable_prefixes[-1]

func _force_equip_to_misc_slot(character: Character, equipment_instance: EquipmentInstance) -> void:
	# Directly assign to first available misc slot
	if character.misc1 == null:
		character.misc1 = equipment_instance
	elif character.misc2 == null:
		character.misc2 = equipment_instance
	elif character.misc3 == null:
		character.misc3 = equipment_instance
	elif character.misc4 == null:
		character.misc4 = equipment_instance
	else:
		# All misc slots full - replace misc1 (latest wins)
		character.misc1 = equipment_instance
	
	# Trigger character updates
	character._assign_colors_for_equipment_variants()
	character._recalc_stats()
	character.emit_signal("model_changed")

# -------------------------------
# PRIVATE: palette loading
# Replace this with your actual CSV reader.
# Must return hex strings like "RRGGBB" or "#RRGGBB".
# -------------------------------
func _load_palette() -> PackedStringArray:
	# Use ColorManager singleton to get main palette
	var hex_colors = ColorManager.get_palette_hex("main")
	if hex_colors.is_empty():
		push_warning("[EquipmentFactory] main palette not found in ColorManager (using tiny fallback)")
		var out: PackedStringArray = PackedStringArray()
		out.push_back("E8D8C3")
		out.push_back("5A4632")
		out.push_back("7B3F00")
		out.push_back("4A6FA5")
		out.push_back("8F1D1D")
		out.push_back("356859")
		return out

	# Convert array to PackedStringArray and return
	var out_packed: PackedStringArray = PackedStringArray()
	for hex in hex_colors:
		out_packed.push_back(hex)
	return out_packed



# Removed _is_hex function - no longer needed since ColorManager provides validated hex colors
