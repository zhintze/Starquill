@tool
extends Node

const DELIM := ","

# =========================
# EQUIPMENT CONVERTER
# =========================
func convert_equipment_csv_to_json(csv_path: String, json_out_path: String) -> void:
	var f := FileAccess.open(csv_path, FileAccess.READ)
	if f == null:
		push_error("csv_to_json_converter: Cannot open CSV: %s" % csv_path)
		return

	var headers: Array = _read_csv_line(f)
	if headers.is_empty():
		push_error("csv_to_json_converter: CSV has no header row: %s" % csv_path)
		return
	var idx: Dictionary = _index_headers(headers)

	var rows: Array = []
	while not f.eof_reached():
		var line: Array = _read_csv_line(f)
		if line.is_empty():
			continue

		var desc: String = _csv_get(line, idx, "description", "").strip_edges()
		if desc.to_upper().begins_with("SPECIES"):
			continue

		var item_type: String = _csv_get(line, idx, "item type", "")
		var layer_codes: Array = _collect_layer_codes_int(line, idx)  # INT array
		var hidden_layers: Array = _split_ints(_csv_get(line, idx, "hidden layer numbers", ""))
		var layer_color_variance: Array = _split_ints(_csv_get(line, idx, "layer color variance", ""))
		var modular: bool = _to_bool_y(_csv_get(line, idx, "modular", ""))
		var amount: int = _to_int_default(_csv_get(line, idx, "amount", ""), 0)

		if item_type == "" and layer_codes.is_empty():
			continue

		rows.append({
			"description": desc,
			"item_type": item_type,
			"layer_codes": layer_codes,
			"hidden_layers": hidden_layers,
			"layer_color_variance": layer_color_variance,
			"modular": modular,
			"amount": amount,
		})

	f.close()
	_write_json(rows, json_out_path)

# =========================
# SPECIES CONVERTER
# =========================
# Required/parsed columns (case-insensitive):
# name : string
# x scale : float
# y scale : float
# backArm, legs, body, head, frontArm, eyes, nose, mouth, ears, facialHair, facialDetail : string
# hair : array<string> (space-separated)
# otherBodyParts : array<string> (space-separated)
# skin color : array<string> (space-separated; if single token present, becomes ["token"])
# itemRestrictions : array<string> (space-separated)
# skinVariance : split → skinVariance_hex (array<string> of 6-char hex), skinVariance_indices (array<int>)
func convert_species_csv_to_json(csv_path: String, json_out_path: String) -> void:
	var f := FileAccess.open(csv_path, FileAccess.READ)
	if f == null:
		push_error("csv_to_json_converter: Cannot open CSV: %s" % csv_path)
		return

	var headers: Array = _read_csv_line(f)
	if headers.is_empty():
		push_error("csv_to_json_converter: CSV has no header row: %s" % csv_path)
		return
	var idx: Dictionary = _index_headers(headers)

	var rows: Array = []
	while not f.eof_reached():
		var line: Array = _read_csv_line(f)
		if line.is_empty():
			continue

		var name: String = _csv_get(line, idx, "name", "")
		if name == "":
			continue

		var x_scale: float = _to_float_default(_csv_get(line, idx, "x scale", _csv_get(line, idx, "xscale", "")), 1.0)
		var y_scale: float = _to_float_default(_csv_get(line, idx, "y scale", _csv_get(line, idx, "yscale", "")), 1.0)

		var back_arm: String = _first_non_empty([_csv_get(line, idx, "backarm", ""), _csv_get(line, idx, "back arm", "")])
		var legs: String = _csv_get(line, idx, "legs", "")
		var body: String = _csv_get(line, idx, "body", "")
		var head: String = _csv_get(line, idx, "head", "")
		var front_arm: String = _first_non_empty([_csv_get(line, idx, "frontarm", ""), _csv_get(line, idx, "front arm", "")])
		var eyes: String = _csv_get(line, idx, "eyes", "")
		var nose: String = _csv_get(line, idx, "nose", "")
		var mouth: String = _csv_get(line, idx, "mouth", "")
		var ears: String = _csv_get(line, idx, "ears", "")
		var facial_hair: String = _first_non_empty([_csv_get(line, idx, "facialhair", ""), _csv_get(line, idx, "facial hair", "")])
		var facial_detail: String = _first_non_empty([_csv_get(line, idx, "facialdetail", ""), _csv_get(line, idx, "facial detail", "")])

		var hair_arr: Array = _split_tokens(_csv_get(line, idx, "hair", ""))
		var other_body_parts_arr: Array = _split_tokens(_first_non_empty([_csv_get(line, idx, "otherbodyparts", ""), _csv_get(line, idx, "other body parts", "")]))
		var skin_color_arr: Array = _split_tokens(_first_non_empty([_csv_get(line, idx, "skin color", ""), _csv_get(line, idx, "skincolor", "")]))
		if skin_color_arr.is_empty():
			var raw_skin := _first_non_empty([_csv_get(line, idx, "skin color", ""), _csv_get(line, idx, "skincolor", "")])
			if raw_skin != "":
				skin_color_arr = [raw_skin]

		var item_restrictions: Array = _split_tokens(_first_non_empty([_csv_get(line, idx, "itemrestrictions", ""), _csv_get(line, idx, "item restrictions", "")]))

		var raw_skin_variance := _first_non_empty([_csv_get(line, idx, "skinvariance", ""), _csv_get(line, idx, "skin variance", "")])
		var sv_hex: Array
		var sv_idx: Array
		var sv := _split_skin_variance(raw_skin_variance)
		sv_hex = sv["hex"]
		sv_idx = sv["idx"]

		rows.append({
			"name": name,
			"x_scale": x_scale,
			"y_scale": y_scale,
			"backArm": back_arm,
			"legs": legs,
			"body": body,
			"head": head,
			"frontArm": front_arm,
			"eyes": eyes,
			"nose": nose,
			"mouth": mouth,
			"ears": ears,
			"facialHair": facial_hair,
			"facialDetail": facial_detail,
			"hair": hair_arr,
			"otherBodyParts": other_body_parts_arr,
			"skin_color": skin_color_arr,
			"itemRestrictions": item_restrictions,
			"skinVariance_hex": sv_hex,
			"skinVariance_indices": sv_idx
		})

	f.close()
	_write_json(rows, json_out_path)

# =========================
# SHARED HELPERS
# =========================
func _read_csv_line(f: FileAccess) -> Array:
	var arr: PackedStringArray = f.get_csv_line(DELIM)
	var line: Array = []
	for s in arr:
		line.append(String(s))
	return line

func _index_headers(headers: Array) -> Dictionary:
	var idx := {}
	for i in range(headers.size()):
		var key: String = String(headers[i]).strip_edges().to_lower()
		idx[key] = i
	return idx

func _csv_get(line: Array, idx: Dictionary, header_lc: String, default_val: String = "") -> String:
	if not idx.has(header_lc):
		return default_val
	var i: int = int(idx[header_lc])
	if i < 0 or i >= line.size():
		return default_val
	return String(line[i]).strip_edges()

func _collect_layer_codes_int(line: Array, idx: Dictionary) -> Array:
	# 1) Try the explicit, preferred keys first (case-insensitive, since idx keys are lowercased).
	var preferred := ["LayerCode1","LayerCode2","LayerCode3"]

	var candidate_keys: Array = []
	for k in preferred:
		if idx.has(k):
			candidate_keys.append(k)

	if candidate_keys.is_empty():
		var discovered: Array = []
		for k in idx.keys():
			var ks := String(k)
			if ks.begins_with("layercode") or ks.begins_with("layer code"):
				var digits := ""
				for i in range(ks.length()):
					var ch := ks[i]
					if ch >= "0" and ch <= "9":
						digits += ch
				var ord := digits.to_int()
				if digits == "":
					ord = 999
				discovered.append({"key": ks, "ord": ord})
		discovered.sort_custom(func(a, b): return int(a.ord) < int(b.ord))
		for d in discovered:
			candidate_keys.append(d.key)

	var out: Array = []
	for k in candidate_keys:
		var raw: String = _csv_get(line, idx, k, "")
		var n: int = _extract_first_int(raw)
		if n != -1:
			out.append(n)

	# Deduplicate while preserving order (optional)
	var seen := {}
	var dedup: Array = []
	for n in out:
		if not seen.has(n):
			seen[n] = true
			dedup.append(n)
	return dedup
	
	
func _split_ints(text: String) -> Array:
	if text == "":
		return []
	var out: Array = []
	for tok in text.split(" ", false):
		tok = tok.strip_edges()
		if tok == "":
			continue
		if tok.is_valid_int():
			out.append(int(tok))
	return out

func _split_tokens(text: String) -> Array:
	if text == "":
		return []
	var out: Array = []
	for tok in text.split(" ", false):
		tok = tok.strip_edges()
		if tok != "":
			out.append(tok)
	return out

func _to_bool_y(text: String) -> bool:
	return text.to_lower() == "y"

func _to_int_default(text: String, def: int) -> int:
	if text == "":
		return def
	if text.is_valid_int():
		return text.to_int()
	var f := text.to_float()
	if f != 0.0:
		return int(f)
	return def

func _to_float_default(text: String, def: float) -> float:
	if text == "":
		return def
	return text.to_float()

func _first_non_empty(arr: Array) -> String:
	for s in arr:
		var t := String(s).strip_edges()
		if t != "":
			return t
	return ""

func _is_hex6(token: String) -> bool:
	var t := token.strip_edges()
	if t.begins_with("#"):
		t = t.substr(1) # omit the '#'
	if t.length() != 6:
		return false
	for i in range(6):
		var ch := t[i]                 # 1-char String
		var is_digit := (ch >= "0" and ch <= "9")
		var is_low_hex := (ch >= "a" and ch <= "f")
		var is_up_hex := (ch >= "A" and ch <= "F")
		if not (is_digit or is_low_hex or is_up_hex):
			return false
	return true

func _split_skin_variance(text: String) -> Dictionary:
	# Returns {"hex": Array[String], "idx": Array[int]}
	var hexes: Array = []
	var idxs: Array = []
	if text == "":
		return {"hex": hexes, "idx": idxs}

	for raw in text.split(" ", false):
		var tok := raw.strip_edges()
		if tok == "":
			continue
		if _is_hex6(tok):
			var t := tok
			if t.begins_with("#"):
				t = t.substr(1, t.length() - 1)
			hexes.append(t.to_upper())
		elif tok.is_valid_int():
			idxs.append(int(tok))
		# silently ignore non-hex, non-int junk

	return {"hex": hexes, "idx": idxs}

func _write_json(rows: Array, json_out_path: String) -> void:
	var json_text := JSON.stringify(rows, "\t")
	var out := FileAccess.open(json_out_path, FileAccess.WRITE)
	if out == null:
		push_error("csv_to_json_converter: Cannot open output JSON: %s" % json_out_path)
		return
	out.store_string(json_text)
	out.close()
	print("csv_to_json_converter: Wrote %d rows -> %s" % [rows.size(), json_out_path])

func _write_json_dict(data: Dictionary, json_out_path: String) -> void:
	var json_text := JSON.stringify(data, "\t")
	var out := FileAccess.open(json_out_path, FileAccess.WRITE)
	if out == null:
		push_error("csv_to_json_converter: Cannot open output JSON: %s" % json_out_path)
		return
	out.store_string(json_text)
	out.close()
	print("csv_to_json_converter: Wrote dictionary -> %s" % json_out_path)

	
func _extract_first_int(text: String) -> int:
	# Returns an int if found; otherwise -1.
	if text == "":
		return -1
	for tok in text.split(" ", false):
		tok = tok.strip_edges()
		if tok == "":
			continue
		# Exact int?
		if tok.is_valid_int():
			return tok.to_int()
		# Float that is an integer (e.g., "156.0")?
		var has_digit := false
		for i in range(tok.length()):
			var ch := tok[i]
			if ch >= "0" and ch <= "9":
				has_digit = true
				break
		if has_digit:
			var f := tok.to_float()
			var i_val := int(f)
			if abs(f - float(i_val)) < 0.0001:
				return i_val
	return -1

# =========================
# SPECIES MODULAR PARTS CONVERTER
# =========================
# CSV columns (case-insensitive):
# type : string
# amount : int
func convert_species_modular_parts_csv_to_json(csv_path: String, json_out_path: String) -> void:
	var f := FileAccess.open(csv_path, FileAccess.READ)
	if f == null:
		push_error("csv_to_json_converter: Cannot open CSV: %s" % csv_path)
		return

	var headers: Array = _read_csv_line(f)
	if headers.is_empty():
		push_error("csv_to_json_converter: CSV has no header row: %s" % csv_path)
		return
	var idx: Dictionary = _index_headers(headers)

	var rows: Array = []
	while not f.eof_reached():
		var line: Array = _read_csv_line(f)
		if line.is_empty():
			continue

		var t: String = _csv_get(line, idx, "type", "")
		var amt: int = _to_int_default(_csv_get(line, idx, "amount", ""), 0)

		# Skip rows that have neither type nor amount info
		if t == "" and amt == 0:
			continue

		rows.append({
			"type": t,
			"amount": amt
		})

	f.close()
	_write_json(rows, json_out_path)

# =========================
# COLOR PALETTE CONVERTER
# =========================
# Scans documents/color_*.csv files and converts them to a single color_palettes.json
# Format: { "main": ["ddc98a", "b6a37e", ...], "skin_human": [...], "weapon": [...] }
func convert_all_color_csvs_to_json(output_path: String = "res://assets/data/color_palettes.json") -> void:
	var color_dir = "res://documents"
	var palettes: Dictionary = {}
	
	# Scan for all color_*.csv files
	var dir = DirAccess.open(color_dir)
	if dir == null:
		push_error("csv_to_json_converter: Cannot open documents directory: %s" % color_dir)
		return
	
	dir.list_dir_begin()
	var file_name = dir.get_next()
	
	while file_name != "":
		if file_name.begins_with("color_") and file_name.ends_with(".csv"):
			# Extract palette name: color_main.csv -> main, color_skin_human.csv -> skin_human
			var palette_name = file_name.trim_prefix("color_").trim_suffix(".csv")
			var file_path = color_dir + "/" + file_name
			
			var colors = _read_color_csv(file_path)
			if not colors.is_empty():
				palettes[palette_name] = colors
				print("Converted color palette: %s (%d colors)" % [palette_name, colors.size()])
		
		file_name = dir.get_next()
	
	dir.list_dir_end()
	
	if palettes.is_empty():
		push_warning("csv_to_json_converter: No color CSV files found in %s" % color_dir)
		return
	
	_write_json_dict(palettes, output_path)
	print("Color palettes written to: %s (%d palettes)" % [output_path, palettes.size()])

func _read_color_csv(csv_path: String) -> Array[String]:
	var colors: Array[String] = []
	var f := FileAccess.open(csv_path, FileAccess.READ)
	
	if f == null:
		push_error("csv_to_json_converter: Cannot open color CSV: %s" % csv_path)
		return colors
	
	while not f.eof_reached():
		var line = f.get_line().strip_edges()
		if line != "" and not line.begins_with("#"):
			# Validate hex color (6 chars, no # prefix)
			if line.length() == 6 and _is_valid_hex(line):
				colors.append(line.to_lower())
			else:
				push_warning("csv_to_json_converter: Invalid hex color '%s' in %s" % [line, csv_path])
	
	f.close()
	return colors

func _is_valid_hex(hex: String) -> bool:
	for i in hex.length():
		var c = hex[i]
		var valid = (c >= '0' and c <= '9') or (c >= 'a' and c <= 'f') or (c >= 'A' and c <= 'F')
		if not valid:
			return false
	return true
