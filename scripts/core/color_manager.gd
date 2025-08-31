extends Node
class_name StarquillColorManager

# Unified color management for Starquill
# Consolidates Character, SpeciesLoader, and ColorPalette functionality

# Single color cache with proper invalidation
var _color_palettes: Dictionary = {}
var _palettes_loaded: bool = false

func _ready() -> void:
	_load_color_palettes()

# ================================
# Public API
# ================================

# Get colors as Color objects
func get_palette(palette_name: String) -> Array[Color]:
	_ensure_palettes_loaded()
	
	var hex_colors = _get_palette_hex(palette_name)
	var colors: Array[Color] = []
	
	for hex in hex_colors:
		colors.append(hex_to_color(hex))
	
	return colors

# Get colors as hex strings (no # prefix)
func get_palette_hex(palette_name: String) -> Array[String]:
	_ensure_palettes_loaded()
	return _get_palette_hex(palette_name)

# Convert hex string to Color object
func hex_to_color(hex: String) -> Color:
	var clean_hex = hex.strip_edges()
	if clean_hex.begins_with("#"):
		clean_hex = clean_hex.substr(1)
	
	if clean_hex.length() != 6:
		push_warning("ColorManager: Invalid hex color '%s', using white" % hex)
		return Color.WHITE
	
	return Color("#" + clean_hex)

# Convert Color object to hex string (no # prefix)
func color_to_hex(color: Color) -> String:
	return color.to_html(false)

# Pick random color from palette
func get_random_color_from_palette(palette_name: String) -> Color:
	var colors = get_palette(palette_name)
	if colors.is_empty():
		return Color.WHITE
	
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	return colors[rng.randi_range(0, colors.size() - 1)]

# Pick random hex from palette  
func get_random_hex_from_palette(palette_name: String) -> String:
	var hex_colors = get_palette_hex(palette_name)
	if hex_colors.is_empty():
		return "ffffff"
	
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	return hex_colors[rng.randi_range(0, hex_colors.size() - 1)]

# Reload palettes from disk
func reload_palettes() -> void:
	_palettes_loaded = false
	_color_palettes.clear()
	_load_color_palettes()

# Get all available palette names
func get_available_palettes() -> Array[String]:
	_ensure_palettes_loaded()
	var names: Array[String] = []
	for key in _color_palettes.keys():
		names.append(str(key))
	return names

# ================================
# Color Processing for Species/Equipment
# ================================

# Resolve species color field (array of hex colors OR palette keyword)
func resolve_species_color_field(color_field: Array) -> Array[String]:
	if color_field.is_empty():
		return ["ffffff"]  # fallback white
	
	# If first element is a valid hex color, treat as hex array
	var first = str(color_field[0])
	if _is_valid_hex(first):
		var hex_colors: Array[String] = []
		for color in color_field:
			var hex_str = str(color)
			if _is_valid_hex(hex_str):
				hex_colors.append(hex_str)
		return hex_colors
	
	# Otherwise treat first element as palette keyword
	var palette_name = first
	return get_palette_hex(palette_name)

# Pick random color for species/equipment layer coloring
func pick_random_color_for_layer(color_keyword: String) -> Color:
	return get_random_color_from_palette(color_keyword)

# ================================
# Private Implementation
# ================================

func _ensure_palettes_loaded() -> void:
	if not _palettes_loaded:
		_load_color_palettes()

func _load_color_palettes() -> void:
	if _palettes_loaded:
		return
		
	_palettes_loaded = true
	_color_palettes.clear()
	
	var palette_path = ConfigManager.get_data_path("color_palettes_path")
	if not FileAccess.file_exists(palette_path):
		push_error("ColorManager: Color palettes file not found: %s" % palette_path)
		_load_fallback_palettes()
		return
	
	var file = FileAccess.open(palette_path, FileAccess.READ)
	if file == null:
		push_error("ColorManager: Cannot open color palettes file: %s" % palette_path)
		_load_fallback_palettes()
		return
	
	var json_text = file.get_as_text()
	file.close()
	
	var json = JSON.new()
	var parse_result = json.parse(json_text)
	
	if parse_result != OK:
		push_error("ColorManager: Invalid JSON in color palettes file: %s" % palette_path)
		_load_fallback_palettes()
		return
	
	var data = json.data
	if typeof(data) != TYPE_DICTIONARY:
		push_error("ColorManager: Color palettes file must contain a dictionary")
		_load_fallback_palettes()
		return
	
	# Validate and store palettes
	for palette_name in data:
		var colors = data[palette_name]
		if typeof(colors) == TYPE_ARRAY:
			var hex_array: Array[String] = []
			for color in colors:
				var hex_str = str(color)
				if _is_valid_hex(hex_str):
					hex_array.append(hex_str.to_lower())
			if not hex_array.is_empty():
				_color_palettes[str(palette_name)] = hex_array
	
	print("ColorManager: Loaded %d color palettes" % _color_palettes.size())

func _get_palette_hex(palette_name: String) -> Array[String]:
	if _color_palettes.has(palette_name):
		return _color_palettes[palette_name].duplicate()
	
	# Fallback to main palette
	if palette_name != "main" and _color_palettes.has("main"):
		push_warning("ColorManager: Palette '%s' not found, using 'main'" % palette_name)
		return _color_palettes["main"].duplicate()
	
	# Ultimate fallback
	push_warning("ColorManager: No palette found for '%s', using white" % palette_name)
	return ["ffffff"]

func _load_fallback_palettes() -> void:
	# Minimal fallback palette
	_color_palettes["main"] = [
		"f6e0c8", "e8c6a0", "d6a77d", "bf825b",
		"a4633f", "7e482c", "5d3421", "3f2318"
	]
	print("ColorManager: Using fallback color palette")

func _is_valid_hex(hex: String) -> bool:
	var clean = hex.strip_edges()
	if clean.begins_with("#"):
		clean = clean.substr(1)
	if clean.length() != 6:
		return false
	
	for i in clean.length():
		var c = clean[i]
		var valid = (c >= '0' and c <= '9') or (c >= 'a' and c <= 'f') or (c >= 'A' and c <= 'F')
		if not valid:
			return false
	return true