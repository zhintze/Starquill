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

# Create an EquipmentInstance from a handheld catalog dictionary (weapons/shields).
func create_from_handheld_dict(handheld_dict: Dictionary, item_num: int = -1) -> EquipmentInstance:
	if handheld_dict.is_empty():
		push_error("EquipmentFactory.create_from_handheld_dict: empty handheld dict")
		return null

	var amount_data = handheld_dict.get("amount", 1)
	var amt: int = 1
	if typeof(amount_data) == TYPE_ARRAY:
		amt = max(1, (amount_data as Array).size())
	else:
		amt = max(1, int(amount_data))

	var chosen_num: int = item_num if item_num > 0 else ((randi() % amt) + 1)

	var ei := EquipmentInstance.new()
	ei._init_from_handheld_dict(handheld_dict, chosen_num, _main_palette)
	return ei

# Create a random EquipmentInstance from a slot prefix (e.g., "hd","tr","ar","lg","fe","mc").
# For weapons ("w" prefix), use create_random_weapon() instead.
func create_random_from_prefix(prefix: String) -> EquipmentInstance:
	var bucket: Array = StarquillData.get_equipment_by_slot_prefix(prefix)
	if bucket.is_empty():
		return null

	var cat: EquipmentCatalog.CatalogItem = bucket[randi() % bucket.size()] as EquipmentCatalog.CatalogItem
	if cat == null:
		return null

	return create_from_catalog(cat)

# Create a random weapon from handheld catalog
func create_random_weapon() -> EquipmentInstance:
	var all_handheld: Array = StarquillData.get_all_handheld()
	if all_handheld.is_empty():
		return null

	var handheld_dict: Dictionary = all_handheld[randi() % all_handheld.size()]
	if handheld_dict.is_empty():
		return null

	return create_from_handheld_dict(handheld_dict)

# Create random equipment with slot-specific restrictions
func create_random_from_prefix_restricted(prefix: String) -> EquipmentInstance:
	# Handle weapons separately
	if prefix == "w":
		return create_random_weapon()

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

	# Determine main slot priorities and equip in order
	var main_prefixes: Array[String] = ["hd", "tr", "ar", "lg", "fe", "w"]
	var prioritized: Array[String] = []
	var normal: Array[String] = []
	var by_priority: Dictionary = {}
	for p in main_prefixes:
		var pr: int = StarquillData.get_equipment_prefix_priority(p)
		if pr > 0:
			if not by_priority.has(pr):
				by_priority[pr] = []
			(by_priority[pr] as Array).append(p)
		else:
			normal.append(p)

	# Equip prioritized prefixes: ascending priority, randomize within the same level
	var priority_keys: Array = by_priority.keys()
	priority_keys.sort()
	for pr_key in priority_keys:
		var group: Array = by_priority[pr_key] as Array
		group.shuffle()
		for p in group:
			var ei_p := create_random_from_prefix_restricted(p)
			if ei_p != null:
				ch.equip_instance(ei_p)

	# Handle non-prioritized prefixes via chance
	for p in normal:
		if randf() <= StarquillData.get_equipment_prefix_chance(p):
			var ei := create_random_from_prefix_restricted(p)
			if ei != null:
				ch.equip_instance(ei)

	# Apply equipment chances and priorities for misc slots
	var pool: Array[String] = ["mc", "hd", "tr", "ar", "lg", "fe"]
	for _i in extras:
		var selected_prefix = _select_prefix_for_misc(pool)
		if selected_prefix != "":
			var ei2 := create_random_from_prefix(selected_prefix)
			if ei2 != null:
				# Force all extras into misc slots to ensure variety
				_force_equip_to_misc_slot(ch, ei2)

	if ch.has_signal("equipment_changed"):
		ch.emit_signal("equipment_changed")

func _select_prefix_for_misc(pool: Array[String]) -> String:
	if StarquillData.get_use_misc_prefix_percentages():
		# Use normalized per-prefix weights as selection chances
		var total: float = 0.0
		var weights: Dictionary = {}
		for prefix in pool:
			var w: float = float(max(0.0, StarquillData.get_equipment_prefix_misc_priority(prefix)))
			weights[prefix] = w
			total += w
		if total <= 0.0:
			return ""
		var roll: float = randf() * total
		var acc: float = 0.0
		for prefix in pool:
			acc += float(weights[prefix])
			if roll <= acc:
				return prefix
		return pool[-1]
	else:
		# Chance-gated, uniform selection among those that passed
		var viable: Array[String] = []
		for prefix in pool:
			if randf() <= StarquillData.get_equipment_prefix_chance(prefix):
				viable.append(prefix)
		if viable.is_empty():
			return ""
		viable.shuffle()
		return viable[0]

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
