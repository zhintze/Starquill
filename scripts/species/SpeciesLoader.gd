extends Node
class_name SpeciesLoader

signal species_loaded(count: int)

var by_name: Dictionary = {}   # name -> Species
var all: Array = []            # Array[Species]

static func _has_property(o: Object, prop: String) -> bool:
	for p in o.get_property_list():
		if typeof(p) == TYPE_DICTIONARY and p.has("name") and p["name"] == prop:
			return true
	return false

static func _set_if_present(o: Object, prop: String, val) -> void:
	if _has_property(o, prop):
		o.set(prop, val)

func load_from_json(path: String) -> void:
	by_name.clear()
	all.clear()

	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		push_error("SpeciesLoader: Cannot open %s" % path)
		return

	var text := f.get_as_text()
	var root = JSON.parse_string(text)
	if typeof(root) != TYPE_ARRAY:
		push_error("SpeciesLoader: Root must be an array in %s" % path)
		return

	for entry in root:
		if typeof(entry) != TYPE_DICTIONARY:
			continue

		var s := Species.new()
		for k in entry.keys():
			_set_if_present(s, k, entry[k])

		# name key (no ?:)
		var nm := ""
		if _has_property(s, "name") and typeof(s.name) == TYPE_STRING:
			nm = s.name

		if nm != "":
			by_name[nm] = s
		all.append(s)

	emit_signal("species_loaded", all.size())

func create_random_instance(species_name: String) -> SpeciesInstance:
	var s: Species = by_name.get(species_name, null)
	if s == null:
		push_error("SpeciesLoader: Unknown species '%s'" % species_name)
		return null
	var inst := SpeciesInstance.new()
	inst.from_species(s)
	return inst

func create_random_instance_from_index(idx: int) -> SpeciesInstance:
	if idx < 0 or idx >= all.size():
		push_error("SpeciesLoader: Index out of range %d" % idx)
		return null
	var inst := SpeciesInstance.new()
	inst.from_species(all[idx])
	return inst
