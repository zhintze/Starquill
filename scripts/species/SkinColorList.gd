extends RefCounted
class_name SkinColorList

const DOCS_DIR := "res://documents"

static var _cache: Dictionary = {}

static func get_colors(keyword: String) -> PackedStringArray:
	# Returns a list of hex strings (without validation here; callers can sanitize).
	var key := String(keyword).strip_edges().to_lower()
	if key == "":
		return PackedStringArray()
	if _cache.has(key):
		return _cache[key]

	var path := "%s/skin_color_%s.csv" % [DOCS_DIR, key]
	if not FileAccess.file_exists(path):
		# Not found -> empty list; caller will gracefully fall back.
		_cache[key] = PackedStringArray()
		return _cache[key]

	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		_cache[key] = PackedStringArray()
		return _cache[key]

	var txt := f.get_as_text()
	f.close()

	var out := PackedStringArray()

	# Extremely tolerant CSV: split lines, then split by comma; strip quotes/spaces.
	var lines := txt.split("\n", false)
	for line in lines:
		var row := String(line).strip_edges()
		if row == "" or row.begins_with("#"):
			continue
		var cells := row.split(",", false)
		for cell in cells:
			var s := String(cell).strip_edges()
			if s.length() >= 2 and s[0] == '"' and s[s.length() - 1] == '"':
				s = s.substr(1, s.length() - 2)
			s = s.strip_edges()
			if s == "":
				continue
			out.append(s)

	_cache[key] = out
	return out
