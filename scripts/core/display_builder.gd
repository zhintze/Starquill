extends Node
class_name StarquillDisplayBuilder

# Unified display piece builder - replaces SpeciesDisplayBuilder, SpeciesDisplayable, EquipmentDisplayBuilder
# Uses LayerManager for all layer operations and ConfigManager for centralized configuration

const SPECIES_IMG_DIR := "res://assets/images/species"
const EQUIPMENT_IMG_DIR := "res://assets/images/equipment"
const WEAPONS_IMG_DIR := "res://assets/images/weapons"

# Result wrapper for equipment building (includes hidden layers)
class EquipmentResult:
	var pieces: Array[DisplayPiece] = []
	var hidden_species_layers: PackedInt32Array = PackedInt32Array()

# ================================
# Main API - Build complete displays 
# ================================

# Build display for complete character (species + equipment, with layer filtering)
func build_character_display(character: Character) -> Array[DisplayPiece]:
	if character == null or character.species == null:
		return []

	# Apply equipment color assignments first
	character._assign_colors_for_equipment_variants()

	# Build equipment pieces and get hidden layers (filter out species-restricted items)
	var equipment_with_slots: Array[Dictionary] = character.get_all_equipment_with_slots()
	var species_restrictions: PackedStringArray = character.species.itemRestrictions
	var equipment_result: EquipmentResult = build_equipment_pieces_with_slots(equipment_with_slots, species_restrictions)
	var equipment_pieces: Array[DisplayPiece] = equipment_result.pieces
	var hidden_layers: PackedInt32Array = equipment_result.hidden_species_layers
	
	# Apply equipment layer tinting from character
	if not character.equipment_layer_colors.is_empty():
		equipment_pieces = StarquillLayerManager.apply_layer_tints(equipment_pieces, character.equipment_layer_colors)
	
	# Build species pieces
	var species_pieces: Array[DisplayPiece] = build_species_pieces(character.species)
	
	# Use LayerManager to merge, filter hidden layers, and sort
	return StarquillLayerManager.merge_and_sort_pieces(species_pieces, equipment_pieces, hidden_layers)

# Build display for species only
func build_species_display(species_instance: SpeciesInstance) -> Array[DisplayPiece]:
	if species_instance == null:
		return []
	return build_species_pieces(species_instance)

# Build display for equipment only
func build_equipment_display(equipment: Array[EquipmentInstance]) -> Array[DisplayPiece]:
	var result: EquipmentResult = build_equipment_pieces(equipment, PackedStringArray())
	return StarquillLayerManager.sort_pieces_by_layer(result.pieces)

# ================================
# Core Building Functions
# ================================

# Build display pieces for a species instance
func build_species_pieces(species_instance: SpeciesInstance) -> Array[DisplayPiece]:
	var pieces: Array[DisplayPiece] = []
	
	# Get color assignments from the instance
	var skin_color: Color = species_instance.skinColor
	var hair_color: Color = species_instance.hairColor
	var eyes_color: Color = species_instance.eyesColor
	var facial_detail_color: Color = species_instance.facialDetailColor
	
	# Build pieces for each body part (pass species_instance for variance lookup)
	_add_species_field("backArm", species_instance.backArm, pieces, skin_color, species_instance)
	_add_species_field("body", species_instance.body, pieces, skin_color, species_instance)
	_add_species_field("legs", species_instance.legs, pieces, skin_color, species_instance)
	_add_species_field("head", species_instance.head, pieces, skin_color, species_instance)
	_add_species_field("ears", species_instance.ears, pieces, skin_color, species_instance)
	_add_species_field("eyes", species_instance.eyes, pieces, eyes_color, species_instance)
	_add_species_field("nose", species_instance.nose, pieces, skin_color, species_instance)
	_add_species_field("mouth", species_instance.mouth, pieces, skin_color, species_instance)
	_add_species_field("facialHair", species_instance.facialHair, pieces, hair_color, species_instance)
	_add_species_field("facialDetail", species_instance.facialDetail, pieces, facial_detail_color, species_instance)
	_add_species_field("hair", species_instance.hair, pieces, hair_color, species_instance)
	_add_species_field("frontArm", species_instance.frontArm, pieces, skin_color, species_instance)
	
	# Other body parts
	for token in species_instance.otherBodyParts:
		_add_species_field("otherBodyParts", String(token), pieces, skin_color, species_instance)
	
	return StarquillLayerManager.sort_pieces_by_layer(pieces)

# Build display pieces for equipment with slot information (for off-hand transformations)
func build_equipment_pieces_with_slots(equipment_with_slots: Array[Dictionary], restrictions: PackedStringArray = PackedStringArray()) -> EquipmentResult:
	var result := EquipmentResult.new()

	# Build slot map using item_type as key (more reliable than object reference)
	var slot_map: Dictionary = {}  # item_type (String) -> slot_name (String)
	var equipment_instances: Array[EquipmentInstance] = []

	for item_dict in equipment_with_slots:
		var ei: EquipmentInstance = item_dict.get("equipment", null)
		var slot: String = item_dict.get("slot", "")
		if ei != null:
			equipment_instances.append(ei)
			# Use item_type as key since deduplication might change instance references
			slot_map[ei.item_type] = slot

	# Apply "latest wins" deduplication for identical item_types
	var deduplicated_equipment: Array[EquipmentInstance] = _deduplicate_equipment_by_type(equipment_instances)

	for ei in deduplicated_equipment:
		if ei == null:
			continue

		# Skip equipment that is restricted by species
		if restrictions.has(ei.item_type):
			continue

		# Get slot for this equipment using item_type as key
		var slot_name: String = slot_map.get(ei.item_type, "")

		# Check for equipment in regular catalog first, then handheld catalog for weapons
		var catalog_item: EquipmentCatalog.CatalogItem = StarquillData.get_equipment_by_type(ei.item_type)
		var handheld_dict: Dictionary = {}
		var layer_codes: Array = []
		var hidden_layers: Array = []

		if catalog_item != null:
			# Regular equipment from EquipmentCatalog
			layer_codes = catalog_item.layer_codes
			hidden_layers = catalog_item.hidden_layers
		elif ei.item_type.begins_with("w"):
			# Check handheld catalog for weapons
			handheld_dict = StarquillData.get_handheld_by_type(ei.item_type)
			if not handheld_dict.is_empty():
				layer_codes = handheld_dict.get("layer_codes", [])
				hidden_layers = handheld_dict.get("hidden_layers", [])
			else:
				push_warning("DisplayBuilder: unknown weapon type '%s'" % ei.item_type)
				continue
		else:
			push_warning("DisplayBuilder: unknown equipment type '%s'" % ei.item_type)
			continue

		# Collect hidden species layers
		for hidden_layer in hidden_layers:
			result.hidden_species_layers.append(int(hidden_layer))

		# Build pieces for each layer of this equipment item
		var item_code: String = ei.item_type
		var item_num: int = int(ei.item_num)

		for i in range(layer_codes.size()):
			var layer_code = layer_codes[i]
			var layer: int = int(layer_code)

			# For modular weapons, use layer variants; for non-modular items (including non-modular weapons), use item_num
			var variant_num: int = item_num
			if not handheld_dict.is_empty() and handheld_dict.get("modular", false) and ei.modular:
				# Use layer variant for modular weapons if available
				if ei.layer_variants.size() > i:
					variant_num = ei.layer_variants[i]

			# Use weapons directory for weapons, equipment directory for equipment
			var img_dir: String = WEAPONS_IMG_DIR if item_code.begins_with("w") else EQUIPMENT_IMG_DIR

			# Weapons use different naming convention: w01-164-0001.png vs equipment: hd01-0001-160.png
			var path: String
			if item_code.begins_with("w"):
				path = "%s/%s-%03d-%04d.png" % [img_dir, item_code, layer, variant_num]
			else:
				path = "%s/%s-%04d-%03d.png" % [img_dir, item_code, variant_num, layer]

			# Get color tint for this layer
			var tint: Color = ei.tint_for_layer(layer_code)

			# Create display piece (let load() handle missing files rather than FileAccess.file_exists)
			var piece: DisplayPiece = DisplayPiece.from_path(path, layer, tint)
			if piece != null:
				# Apply off-hand transformations
				if slot_name == "off_hand":
					piece.is_offhand_weapon = true  # Mark as off-hand for flip handling
					if StarquillData.is_handheld_shield(ei.item_type):
						# Shields: translate position to align with off-hand
						# Note: X offset will be mirrored when character faces left
						piece.offset = DisplayConstants.OFFHAND_SHIELD_OFFSET
					else:
						# One-handed weapons: rotate counter-clockwise and translate
						# Note: X offset will be mirrored when character faces left
						piece.rotation_degrees = DisplayConstants.OFFHAND_WEAPON_ROTATION
						piece.offset = DisplayConstants.OFFHAND_WEAPON_OFFSET

				result.pieces.append(piece)
			else:
				# Only warn if in debug build, since FileAccess.file_exists() doesn't work reliably in exports
				if OS.is_debug_build():
					push_warning("DisplayBuilder: equipment texture not found: %s" % path)

	return result

# Build display pieces for equipment instances
func build_equipment_pieces(equipment: Array[EquipmentInstance], restrictions: PackedStringArray = PackedStringArray()) -> EquipmentResult:
	var result := EquipmentResult.new()
	
	# Apply "latest wins" deduplication for identical item_types
	var deduplicated_equipment: Array[EquipmentInstance] = _deduplicate_equipment_by_type(equipment)
	
	for ei in deduplicated_equipment:
		if ei == null:
			continue
		
		# Skip equipment that is restricted by species
		if restrictions.has(ei.item_type):
			continue
		
		# Check for equipment in regular catalog first, then handheld catalog for weapons
		var catalog_item: EquipmentCatalog.CatalogItem = StarquillData.get_equipment_by_type(ei.item_type)
		var handheld_dict: Dictionary = {}
		var layer_codes: Array = []
		var hidden_layers: Array = []

		if catalog_item != null:
			# Regular equipment from EquipmentCatalog
			layer_codes = catalog_item.layer_codes
			hidden_layers = catalog_item.hidden_layers
		elif ei.item_type.begins_with("w"):
			# Check handheld catalog for weapons
			handheld_dict = StarquillData.get_handheld_by_type(ei.item_type)
			if not handheld_dict.is_empty():
				layer_codes = handheld_dict.get("layer_codes", [])
				hidden_layers = handheld_dict.get("hidden_layers", [])
			else:
				push_warning("DisplayBuilder: unknown weapon type '%s'" % ei.item_type)
				continue
		else:
			push_warning("DisplayBuilder: unknown equipment type '%s'" % ei.item_type)
			continue

		# Collect hidden species layers
		for hidden_layer in hidden_layers:
			result.hidden_species_layers.append(int(hidden_layer))

		# Build pieces for each layer of this equipment item
		var item_code: String = ei.item_type
		var item_num: int = int(ei.item_num)

		for i in range(layer_codes.size()):
			var layer_code = layer_codes[i]
			var layer: int = int(layer_code)

			# For modular weapons, use layer variants; for non-modular items (including non-modular weapons), use item_num
			var variant_num: int = item_num
			if not handheld_dict.is_empty() and handheld_dict.get("modular", false) and ei.modular:
				# Use layer variant for modular weapons if available
				if ei.layer_variants.size() > i:
					variant_num = ei.layer_variants[i]

			# Use weapons directory for weapons, equipment directory for equipment
			var img_dir: String = WEAPONS_IMG_DIR if item_code.begins_with("w") else EQUIPMENT_IMG_DIR

			# Weapons use different naming convention: w01-164-0001.png vs equipment: hd01-0001-160.png
			var path: String
			if item_code.begins_with("w"):
				path = "%s/%s-%03d-%04d.png" % [img_dir, item_code, layer, variant_num]
			else:
				path = "%s/%s-%04d-%03d.png" % [img_dir, item_code, variant_num, layer]
			
			# Get color tint for this layer
			var tint: Color = ei.tint_for_layer(layer_code)
			
			# Create display piece (let load() handle missing files rather than FileAccess.file_exists)
			var piece: DisplayPiece = DisplayPiece.from_path(path, layer, tint)
			if piece != null:
				result.pieces.append(piece)
			else:
				# Only warn if in debug build, since FileAccess.file_exists() doesn't work reliably in exports
				if OS.is_debug_build():
					push_warning("DisplayBuilder: equipment texture not found: %s" % path)
	
	return result

# Apply "latest wins" deduplication for identical item_type items
func _deduplicate_equipment_by_type(equipment: Array[EquipmentInstance]) -> Array[EquipmentInstance]:
	var item_type_tracker: Dictionary = {}  # item_type -> latest EquipmentInstance
	var hat_set_latest: EquipmentInstance = null  # Latest item from hd01-hd08 hat set

	# Define the hat set (hd01-hd08)
	var hat_set: PackedStringArray = ["hd01", "hd02", "hd03", "hd04", "hd05", "hd06", "hd07", "hd08"]

	# Process in order - later items override earlier ones with same item_type
	for ei in equipment:
		if ei == null:
			continue

		# Special handling for hat set - track latest across the entire group
		if hat_set.has(ei.item_type):
			hat_set_latest = ei

		item_type_tracker[ei.item_type] = ei

	# Return only the latest instance of each item_type
	var deduplicated: Array[EquipmentInstance] = []
	for ei in equipment:
		if ei == null:
			continue

		# Special rule for hat set: only include if this is the latest hat set item
		if hat_set.has(ei.item_type):
			if ei == hat_set_latest:
				deduplicated.append(ei)
		else:
			# Normal rule: only include if this is the latest instance of this item_type
			if item_type_tracker[ei.item_type] == ei:
				deduplicated.append(ei)

	return deduplicated

# ================================
# Species Field Processing (from SpeciesDisplayable)
# ================================

func _add_species_field(field_name: String, token: String, pieces: Array[DisplayPiece], base_color: Color, species_instance: SpeciesInstance) -> void:
	if token == "":
		return
	
	var parsed := ImageId.parse(token)
	match parsed.get("kind", ImageId.Kind.INVALID):
		ImageId.Kind.EMPTY:
			return  # intentionally empty
		ImageId.Kind.INVALID:
			push_warning("DisplayBuilder: invalid species token '%s' for %s" % [token, field_name])
			return
		ImageId.Kind.STATIC:
			_add_static_species_piece(parsed, pieces, field_name, base_color, species_instance)
		ImageId.Kind.MODULAR_FULL:
			_add_modular_full_species_piece(parsed, pieces, field_name, base_color, species_instance)
		ImageId.Kind.MODULAR_GROUP_ONLY:
			_add_modular_group_species_piece(parsed, pieces, field_name, base_color, species_instance)

func _add_static_species_piece(parsed: Dictionary, pieces: Array[DisplayPiece], field_name: String, base_color: Color, species_instance: SpeciesInstance) -> void:
	var image_num: String = parsed["imageNum"]
	var layer: int = int(parsed["layer"])
	var id_str := ImageId.to_static_id(image_num, layer)
	var path := "%s/%s.png" % [SPECIES_IMG_DIR, id_str]
	_create_species_piece(path, layer, field_name, base_color, species_instance, pieces)

func _add_modular_full_species_piece(parsed: Dictionary, pieces: Array[DisplayPiece], field_name: String, base_color: Color, species_instance: SpeciesInstance) -> void:
	var group: String = parsed["groupType"]
	var image_num: String = parsed["imageNum"]
	var layer: int = int(parsed["layer"])
	var id_str := ImageId.to_modular_id(group, image_num, layer)
	var path := "%s/%s.png" % [SPECIES_IMG_DIR, id_str]
	_create_species_piece(path, layer, field_name, base_color, species_instance, pieces)

func _add_modular_group_species_piece(parsed: Dictionary, pieces: Array[DisplayPiece], field_name: String, base_color: Color, species_instance: SpeciesInstance) -> void:
	var type_code: String = parsed["groupType"]
	var layers: Array[int] = StarquillLayerManager.get_layer_mapping(type_code)
	
	if layers.is_empty():
		push_warning("DisplayBuilder: no layer mapping for modular type '%s' in %s" % [type_code, field_name])
		return
	
	# Get persistent image number from species instance (or generate if missing)
	var img_num: String = species_instance.modular_image_nums.get(type_code, "")
	if img_num == "":
		# Fallback: generate and store for consistency
		img_num = StarquillData.pick_modular_image_num(type_code)
		species_instance.modular_image_nums[type_code] = img_num
	
	# Create pieces for all layers of this modular type
	for layer in layers:
		var id_str := ImageId.to_modular_id(type_code, img_num, layer)
		var path := "%s/%s.png" % [SPECIES_IMG_DIR, id_str]
		_create_species_piece(path, layer, field_name, base_color, species_instance, pieces)

func _create_species_piece(path: String, layer: int, field_name: String, base_color: Color, species_instance: SpeciesInstance, pieces: Array[DisplayPiece]) -> void:
	if not ResourceLoader.exists(path):
		push_warning("DisplayBuilder: species texture not found: %s" % path)
		return
	
	var tint: Color = _calculate_species_tint(field_name, layer, base_color, species_instance)
	var piece: DisplayPiece = DisplayPiece.from_path(path, layer, tint)
	if piece != null:
		pieces.append(piece)

# ================================
# Color Tinting Logic (from SpeciesDisplayable)
# ================================

func _calculate_species_tint(field_name: String, layer: int, base_color: Color, species_instance: SpeciesInstance) -> Color:
	# Hair and eyes use their specific colors, not skin tinting
	match field_name:
		"hair", "facialHair":
			return base_color
		"eyes":
			return base_color
		_:
			# All other parts (including facialDetail): check for variance first, then use base color
			var variance_color: Color = species_instance.get_variance_color_for_layer(layer)
			if variance_color != Color(1,1,1,1):  # If a variance color was found
				return variance_color
			else:
				return base_color
