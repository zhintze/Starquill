extends Node
class_name PartResolver

@export var use_atlas: bool = true
# Your real layout is a flat folder of PNGs by ID:
@export var file_template: String = "res://assets/images/species/{id}.png"
@export var atlas_index_path: String = "res://data/atlas_index.json"

var _atlas_index: Dictionary
var _texture_cache: Dictionary

func _ready() -> void:
	if use_atlas:
		_load_atlas_index()

func _load_atlas_index() -> void:
	if not FileAccess.file_exists(atlas_index_path):
		push_warning("PartResolver: atlas index missing: %s" % atlas_index_path)
		return
	var f := FileAccess.open(atlas_index_path, FileAccess.READ)
	if f == null:
		push_warning("PartResolver: cannot open atlas index: %s" % atlas_index_path)
		return
	var root := JSON.parse_string(f.get_as_text()) as Dictionary
	if typeof(root) != TYPE_DICTIONARY:
		push_warning("PartResolver: bad atlas index JSON")
		return
	_atlas_index = root as Dictionary

func resolve_path(id: String) -> String:
	if id == "":
		return ""
	return file_template.replace("{id}", id)

func resolve_texture(id: String) -> Texture2D:
	# Cache
	if _texture_cache.has(id):
		return _texture_cache[id] as Texture2D

	var tex: Texture2D = null

	# 1) Direct file (works for both "f03" and "0018-085" if files exist)
	var path := resolve_path(id)
	if path != "" and ResourceLoader.exists(path):
		tex = load(path) as Texture2D

	# 2) Optional atlas fallback (only if you later provide an index)
	if tex == null and use_atlas and _atlas_index.has(id):
		var rec := _atlas_index.get(id, {}) as Dictionary
		var atlas_path := String(rec.get("atlas", ""))
		var x := float(rec.get("x", 0))
		var y := float(rec.get("y", 0))
		var w := float(rec.get("w", 0))
		var h := float(rec.get("h", 0))
		if atlas_path != "":
			var base := load(atlas_path) as Texture2D
			if base != null:
				var at := AtlasTexture.new()
				at.atlas = base
				at.region = Rect2(x, y, w, h)
				tex = at

	if tex == null and path != "":
		push_warning("PartResolver: missing asset for %s at %s" % [id, path])

	if tex != null:
		_texture_cache[id] = tex
	return tex
