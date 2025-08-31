class_name StarquillColorPalette

# Uses ConfigManager autoload for configuration access

static var _color_palettes: Dictionary = {}
static var _color_palettes_loaded: bool = false

static func is_hex6(s: String) -> bool:
	if s.length() != ConfigManager.get_validation_constant("hex_color_length"):
		return false
	for i in s.length():
		var c := s.substr(i, 1)
		var is_digit := c >= "0" and c <= "9"
		var is_af_uc := c >= "A" and c <= "F"
		var is_af_lc := c >= "a" and c <= "f"
		if not (is_digit or is_af_uc or is_af_lc):
			return false
	return true

static func normalize_hex(token: String) -> String:
	var t := token.strip_edges()
	if t.begins_with("#"):
		t = t.substr(1)
	return t.to_upper()

static func _read_text(path: String) -> String:
	var fa: FileAccess = FileAccess.open(path, FileAccess.READ)
	return "" if fa == null else fa.get_as_text()

static func _ensure_color_palettes_loaded() -> void:
	if _color_palettes_loaded:
		return
	_color_palettes_loaded = true
	_color_palettes.clear()

	var raw: String = _read_text(ConfigManager.get_data_path("color_palettes_path"))
	if raw.is_empty():
		push_error(ConfigManager.get_error_message("file_not_found", [ConfigManager.get_data_path("color_palettes_path")]))
		return

	var parsed: Variant = JSON.parse_string(raw)
	if typeof(parsed) != TYPE_DICTIONARY:
		push_error(ConfigManager.get_error_message("invalid_json", [ConfigManager.get_data_path("color_palettes_path")]))
		return

	# Ensure arrays are Array[String]; input is already uppercase w/o '#'
	for k in (parsed as Dictionary).keys():
		var key: String = String(k)
		var arr_any: Variant = (parsed as Dictionary)[k]
		if typeof(arr_any) == TYPE_ARRAY:
			var out: Array[String] = []
			for v in (arr_any as Array):
				if typeof(v) == TYPE_STRING and (v as String).length() == ConfigManager.get_validation_constant("hex_color_length"):
					out.append(v as String)
			_color_palettes[key] = out

static func get_palette_or_main(keyword: String) -> Array[String]:
	_ensure_color_palettes_loaded()
	if not _color_palettes.has("main"):
		return []

	var main_any: Variant = _color_palettes["main"]
	var main: Array[String] = []
	if typeof(main_any) == TYPE_ARRAY:
		for s in (main_any as Array):
			main.append(String(s))

	var kw: String = keyword.strip_edges()
	if kw.is_empty():
		return main.duplicate()

	if _color_palettes.has(kw):
		var arr_any: Variant = _color_palettes[kw]
		if typeof(arr_any) == TYPE_ARRAY:
			var out: Array[String] = []
			for s in (arr_any as Array):
				out.append(String(s))
			return out

	# Fallback per spec: use "main" and report
	push_error(ConfigManager.get_error_message("palette_missing", [kw]))
	return main.duplicate()

static func get_main_palette() -> Array[String]:
	return get_palette_or_main("main")

static func build_color_list_from_strings(color_strings: PackedStringArray) -> Array[Color]:
	# Heuristic:
	# - If exactly one token and it is NOT a 6-hex => treat as a palette keyword.
	# - Otherwise, parse as explicit hex list.
	if color_strings.size() == 1:
		var only := normalize_hex(String(color_strings[0]))
		if not is_hex6(only):
			return build_color_list_from_keyword(String(color_strings[0]).strip_edges())
		# If it *is* a single hex, treat as explicit list of one.
	return build_color_list_from_hex_array(color_strings)

static func build_color_list_from_keyword(keyword: String) -> Array[Color]:
	var colors: Array[Color] = []
	var hexes: Array[String] = get_palette_or_main(keyword)
	for h in hexes:
		colors.append(Color("#" + (h as String)))
	return colors

static func build_color_list_from_hex_array(tokens: PackedStringArray) -> Array[Color]:
	var colors: Array[Color] = []
	for raw in tokens:
		var h := normalize_hex(String(raw))
		if is_hex6(h):
			colors.append(Color("#" + h))
		else:
			# Non-hex tokens inside an explicit list are ignored but reported
			push_error("[ColorPalette] Skipped invalid hex token '" + raw + "'.")
	return colors

static func get_random_color(colors: Array[Color]) -> Color:
	if colors.is_empty():
		return Color.WHITE
	return colors[randi() % colors.size()]

static func load_main_palette() -> Array[Color]:
	return build_color_list_from_keyword("main")

static func load_keyword_palette(keyword: String) -> Array[Color]:
	return build_color_list_from_keyword(keyword)