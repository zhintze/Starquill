extends Resource
class_name EquipmentInstance

# -------- Public, read-only API --------
var item_type: String                      : set = _no_set, get = _get_item_type
var item_num: int                          : set = _no_set, get = _get_item_num
var layer_codes: PackedInt32Array          : set = _no_set, get = _get_layer_codes
var layer_color_variance: PackedInt32Array : set = _no_set, get = _get_layer_color_variance
var hidden_layers: PackedInt32Array        : set = _no_set, get = _get_hidden_layers
var modular: bool                          : set = _no_set, get = _get_modular

var base_color: Color                      : set = _no_set, get = _get_base_color
var variance_colors: Dictionary = {}       # int -> Color; read-only by convention

# Stat bonuses (read-only). `stats` is an alias for backward-compat.
var stat_mods: Dictionary                  : set = _no_set, get = _get_stat_mods
var stats: Dictionary                      : set = _no_set, get = _get_stats

# --------- Internal backing fields ---------
var _item_type: String = ""
var _item_num: int = 1
var _layer_codes: PackedInt32Array = PackedInt32Array()
var _layer_color_variance: PackedInt32Array = PackedInt32Array()
var _hidden_layers: PackedInt32Array = PackedInt32Array()
var _modular: bool = false
var _base_color: Color = Color(1, 1, 1, 1)
var _stat_mods: Dictionary = {}    # e.g., {"armor": 3, "str": 1}

# --------- Accessors (read-only) ---------
func _no_set(_v) -> void:
	push_error("EquipmentInstance is immutable from outside. Use EquipmentFactory.")
func _get_item_type() -> String: return _item_type
func _get_item_num() -> int: return _item_num
func _get_layer_codes() -> PackedInt32Array: return _layer_codes
func _get_layer_color_variance() -> PackedInt32Array: return _layer_color_variance
func _get_hidden_layers() -> PackedInt32Array: return _hidden_layers
func _get_modular() -> bool: return _modular
func _get_base_color() -> Color: return _base_color
func _get_stat_mods() -> Dictionary: return _stat_mods
func _get_stats() -> Dictionary: return _stat_mods   # alias for legacy code

# --------- Public helpers ---------
func tint_for_layer(code: int) -> Color:
	return variance_colors.get(code, _base_color)

func get_stat_mod(key: String, default_val: float = 0.0) -> float:
	return float(_stat_mods.get(key, default_val))

# --------- Factory-only initializer (private) ---------
func _init_from_catalog(cat: EquipmentCatalog.CatalogItem, item_num: int, palette: PackedStringArray) -> void:
	_item_type = cat.item_type
	_item_num = max(1, item_num)

	_layer_codes = PackedInt32Array(cat.layer_codes)
	_layer_color_variance = PackedInt32Array(cat.layer_color_variance)
	_hidden_layers = PackedInt32Array(cat.hidden_layers)
	_modular = cat.modular

	# Pull stat mods from the catalog item, supporting either 'stats' or 'stat_mods'
	_stat_mods.clear()
	if cat is Object:
		var s_any: Variant = (cat as Object).get("stats")
		if typeof(s_any) == TYPE_DICTIONARY:
			_stat_mods = (s_any as Dictionary).duplicate()
		else:
			var sm_any: Variant = (cat as Object).get("stat_mods")
			if typeof(sm_any) == TYPE_DICTIONARY:
				_stat_mods = (sm_any as Dictionary).duplicate()

	_initialize_colors(palette)

# --------- Private: color setup ---------
func _initialize_colors(palette: PackedStringArray) -> void:
	_base_color = _pick_from_palette(palette)
	variance_colors.clear()
	for code in _layer_color_variance:
		var layer_code: int = int(code)
		variance_colors[layer_code] = _pick_from_palette(palette)

# Minimal, robust color picker from a hex palette (fallback to white)
func _pick_from_palette(palette: PackedStringArray) -> Color:
	if palette.is_empty():
		return Color(1, 1, 1, 1)

	var hex: String = palette[randi() % palette.size()]

	# Accept "RRGGBB"
	if hex.length() == 6:
		return Color("#" + hex)

	# Accept "#RRGGBB" (or other valid Color strings)
	if hex.begins_with("#"):
		return Color(hex)

	# Last chance: parse arbitrary strings (e.g., "red", "rgb(…)")
	return Color.from_string(hex, Color(1, 1, 1, 1))
