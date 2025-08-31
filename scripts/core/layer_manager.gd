class_name StarquillLayerManager

# Uses ConfigManager autoload for layer constants

# Common layer sorting and filtering operations
static func sort_pieces_by_layer(pieces: Array[DisplayPiece]) -> Array[DisplayPiece]:
	var sorted_pieces: Array[DisplayPiece] = pieces.duplicate()
	var compare_func = func(a: DisplayPiece, b: DisplayPiece) -> bool:
		return a.layer < b.layer
	sorted_pieces.sort_custom(compare_func)
	return sorted_pieces

static func filter_pieces_by_hidden_layers(pieces: Array[DisplayPiece], hidden_layers: PackedInt32Array) -> Array[DisplayPiece]:
	if hidden_layers.is_empty():
		return pieces
		
	var hidden_set := {}
	for layer in hidden_layers:
		hidden_set[int(layer)] = true
		
	var filter_func = func(p: DisplayPiece) -> bool:
		return not hidden_set.has(int(p.layer))
		
	return pieces.filter(filter_func)

static func merge_and_sort_pieces(species_pieces: Array[DisplayPiece], equipment_pieces: Array[DisplayPiece], hidden_layers: PackedInt32Array = PackedInt32Array()) -> Array[DisplayPiece]:
	# Filter species pieces by hidden layers
	var filtered_species: Array[DisplayPiece] = filter_pieces_by_hidden_layers(species_pieces, hidden_layers)
	
	# Merge all pieces
	var all_pieces: Array[DisplayPiece] = []
	all_pieces.append_array(filtered_species)
	all_pieces.append_array(equipment_pieces)
	
	# Sort by layer
	return sort_pieces_by_layer(all_pieces)

static func apply_layer_tints(pieces: Array[DisplayPiece], layer_colors: Dictionary) -> Array[DisplayPiece]:
	if layer_colors.is_empty():
		return pieces
		
	var tinted_pieces: Array[DisplayPiece] = []
	for piece in pieces:
		var layer_i: int = int(piece.layer)
		if layer_colors.has(layer_i):
			var tint_color: Color = layer_colors[layer_i]
			var tinted_piece := DisplayPiece.new()
			tinted_piece.layer = piece.layer
			tinted_piece.texture = piece.texture
			tinted_piece.offset = piece.offset
			tinted_piece.scale = piece.scale
			tinted_piece.flip_h = piece.flip_h
			tinted_piece.flip_v = piece.flip_v
			tinted_piece.modulate = (piece.modulate * tint_color) if piece.modulate != Color(1,1,1,1) else tint_color
			tinted_pieces.append(tinted_piece)
		else:
			tinted_pieces.append(piece)
	return tinted_pieces

static func get_layer_mapping(group_type: String) -> Array[int]:
	# Use centralized layer constants from ConfigManager
	var species_layers = ConfigManager.get_layer_constants_dict().get("species_layers", {})
	var layers = species_layers.get(group_type, [])
	
	# Convert to Array[int] if needed
	var result: Array[int] = []
	for layer in layers:
		if typeof(layer) == TYPE_INT:
			result.append(int(layer))
	
	# Debug logging for missing mappings only
	if result.is_empty() and group_type != "":
		push_warning("[StarquillLayerManager] No layer mapping found for group_type '%s'" % group_type)
	
	return result

static func create_display_piece(texture: Texture2D, layer: int, color: Color, offset: Vector2 = Vector2.ZERO, scale: Vector2 = Vector2.ONE) -> DisplayPiece:
	var piece := DisplayPiece.new()
	piece.texture = texture
	piece.layer = layer
	piece.modulate = color
	piece.offset = offset
	piece.scale = scale
	return piece

static func validate_layer_range(layer: int, min_layer: int = 0, max_layer: int = 999) -> bool:
	return layer >= min_layer and layer <= max_layer

static func get_pieces_in_layer_range(pieces: Array[DisplayPiece], min_layer: int, max_layer: int) -> Array[DisplayPiece]:
	var filter_func = func(p: DisplayPiece) -> bool:
		return validate_layer_range(p.layer, min_layer, max_layer)
	return pieces.filter(filter_func)

static func count_pieces_by_layer(pieces: Array[DisplayPiece]) -> Dictionary:
	var counts := {}
	for piece in pieces:
		var layer: int = piece.layer
		counts[layer] = counts.get(layer, 0) + 1
	return counts

static func debug_print_layer_info(pieces: Array[DisplayPiece], label: String = "Pieces") -> void:
	var layer_counts = count_pieces_by_layer(pieces)
	var layers: Array = layer_counts.keys()
	layers.sort()
	
	print("[%s] Total pieces: %d" % [label, pieces.size()])
	print("[%s] Layers used: %s" % [label, str(layers)])
	for layer in layers:
		print("[%s] Layer %d: %d pieces" % [label, layer, layer_counts[layer]])
