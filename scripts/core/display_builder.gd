extends Node
class_name StarquillDisplayBuilder

# Unified display piece builder - replaces SpeciesDisplayBuilder, SpeciesDisplayable, EquipmentDisplayBuilder
# Uses LayerManager for all layer operations and ConfigManager for centralized configuration

const SPECIES_IMG_DIR := "res://assets/images/species"
const EQUIPMENT_IMG_DIR := "res://assets/images/equipment"

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
	
	# Build equipment pieces and get hidden layers
	var equipment_instances: Array[EquipmentInstance] = character.get_all_equipment_instances()
	var equipment_result: EquipmentResult = build_equipment_pieces(equipment_instances)
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
	var result: EquipmentResult = build_equipment_pieces(equipment)
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

# Build display pieces for equipment instances
func build_equipment_pieces(equipment: Array[EquipmentInstance]) -> EquipmentResult:
	var result := EquipmentResult.new()
	
	for ei in equipment:
		if ei == null:
			continue
		
		var catalog_item: EquipmentCatalog.CatalogItem = StarquillData.get_equipment_by_type(ei.item_type)
		if catalog_item == null:
			push_warning("DisplayBuilder: unknown equipment type '%s'" % ei.item_type)
			continue
		
		# Collect hidden species layers
		for hidden_layer in catalog_item.hidden_layers:
			result.hidden_species_layers.append(int(hidden_layer))
		
		# Build pieces for each layer of this equipment item
		var item_code: String = ei.item_type
		var item_num: int = int(ei.item_num)
		
		for layer_code in catalog_item.layer_codes:
			var layer: int = int(layer_code)
			var path := "%s/%s-%04d-%03d.png" % [EQUIPMENT_IMG_DIR, item_code, item_num, layer]
			
			if not FileAccess.file_exists(path):
				push_warning("DisplayBuilder: equipment texture not found: %s" % path)
				continue
			
			# Get color tint for this layer
			var tint: Color = ei.tint_for_layer(layer_code)
			
			# Create display piece
			var piece: DisplayPiece = DisplayPiece.from_path(path, layer, tint)
			if piece != null:
				result.pieces.append(piece)
	
	return result

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
	
	# Get or generate image number for this modular type
	var img_num: String = StarquillData.pick_modular_image_num(type_code)
	
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
