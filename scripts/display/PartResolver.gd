extends Node
class_name PartResolver

@export var use_atlas: bool = true
@export var file_template: String = "res://art/parts/{dir}/{id}.png"
@export var atlas_index_path: String = "res://data/atlas_index.json"

# Type the registry explicitly
var _atlas_index: Dictionary         # id:String -> {atlas:String, x:int/float, y:int/float, w:int/float, h:int/float}
var _texture_cache: Dictionary       # id:String -> Texture2D

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
	var root := JSON.parse_string(f.get_as_text())
	if typeof(root) != TYPE_DICTIONARY:
		push_warning("PartResolver: bad atlas index JSON")
		return
	_atlas_index = root as Dictionary

func resolve_path(id: String) -> String:
	if use_atlas:
		return ""  # not used in atlas mode
	var dir: String = id.substr(0, 4)
	return file_template.replace("{dir}", dir).replace("{id}", id)

func resolve_texture(id: String) -> Texture2D:
	# Cache
	if _texture_cache.has(id):
		return _texture_cache[id] as Texture2D

	var tex: Texture2D = null

	if use_atlas and _atlas_index.has(id):
		# ---- Explicitly type the record pulled from the dictionary ----
		var rec: Dictionary = _atlas_index.get(id, {}) as Dictionary
		var atlas_path: String = String(rec.get("atlas", ""))
		var x: float = float(rec.get("x", 0))
		var y: float = float(rec.get("y", 0))
		var w: float = float(rec.get("w", 0))
		var h: float = float(rec.get("h", 0))

		if atlas_path != "":
			var base: Texture2D = load(atlas_path) as Texture2D
			if base != null:
				var at := AtlasTexture.new()
				at.atlas = base
				at.region = Rect2(x, y, w, h)
				tex = at
	else:
		var path: String = resolve_path(id)
		if path != "":
			tex = load(path) as Texture2D

	if tex != null:
		_texture_cache[id] = tex
	return tex
