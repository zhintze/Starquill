extends RefCounted
class_name SkinColorList

const DOCS_DIR := "res://documents"

static var _cache: Dictionary = {}

static func get_colors(keyword: String) -> PackedStringArray:
	# Returns a list of hex strings using ColorManager
	var key := String(keyword).strip_edges().to_lower()
	if key == "":
		return PackedStringArray()
	if _cache.has(key):
		return _cache[key]

	# Use ColorManager to get palette
	var hex_colors = ColorManager.get_palette_hex(key)
	if hex_colors.is_empty():
		# Try with "skin_" prefix for backwards compatibility
		hex_colors = ColorManager.get_palette_hex("skin_" + key)
	
	if hex_colors.is_empty():
		# Still not found -> empty list; caller will gracefully fall back.
		_cache[key] = PackedStringArray()
		return _cache[key]

	# Convert to PackedStringArray and cache
	var out := PackedStringArray()
	for hex in hex_colors:
		out.append(hex)
	
	_cache[key] = out
	return out
