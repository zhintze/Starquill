extends Node
class_name StarquillResourceLoader

const StarquillJSONLoader = preload("res://scripts/core/json_data_loader.gd")

# Generic registry for any resource type
var all: Array = []
var by_id: Dictionary = {}

# Virtual methods to be implemented by subclasses
func _create_resource_from_dict(data: Dictionary) -> Resource:
	push_error("ResourceLoader._create_resource_from_dict must be implemented by subclass")
	return null

func _get_resource_id(resource: Resource) -> String:
	push_error("ResourceLoader._get_resource_id must be implemented by subclass")
	return ""

func _get_resource_name(resource: Resource) -> String:
	push_error("ResourceLoader._get_resource_name must be implemented by subclass")
	return ""

# Common registry operations
func clear() -> void:
	all.clear()
	by_id.clear()

func size() -> int:
	return all.size()

func get_by_id(resource_id: String) -> Resource:
	if by_id.has(resource_id):
		return by_id[resource_id] as Resource
	return null

func get_by_index(index: int) -> Resource:
	if index < 0 or index >= all.size():
		return null
	return all[index]

func register_resource(resource: Resource) -> void:
	if resource == null:
		return
		
	all.append(resource)
	
	var id: String = _get_resource_id(resource)
	if id != "" and id.length() >= ConfigManager.get_validation_constant("min_id_length"):
		by_id[id] = resource
	
	var name: String = _get_resource_name(resource)
	if name != "" and not by_id.has(name):
		by_id[name] = resource

# Common JSON loading operations
func load_from_json(path: String) -> void:
	var response: StarquillJSONLoader.LoadResponse = StarquillJSONLoader.load_json_file(path)
	
	match response.result:
		StarquillJSONLoader.LoadResult.SUCCESS:
			_process_json_data(response.data, path)
		StarquillJSONLoader.LoadResult.FILE_NOT_FOUND:
			push_warning("[%s] JSON not found: %s" % [get_script().get_global_name(), path])
		StarquillJSONLoader.LoadResult.INVALID_FORMAT, StarquillJSONLoader.LoadResult.PARSE_ERROR:
			push_warning("[%s] %s" % [get_script().get_global_name(), response.error_message])
		_:
			push_error("[%s] Failed to load JSON: %s - %s" % [get_script().get_global_name(), path, response.error_message])

func load_from_dir(dir_path: String) -> void:
	var json_files: Array[String] = StarquillJSONLoader.scan_directory_for_json(dir_path)
	if json_files.is_empty():
		push_warning("[%s] No JSON files found in directory: %s" % [get_script().get_global_name(), dir_path])
		return
		
	for file_path in json_files:
		load_from_json(file_path)

func _process_json_data(data: Variant, path: String) -> void:
	print("[%s] load_from_json: %s root_type=%s" % [get_script().get_global_name(), path, typeof(data)])
	
	match typeof(data):
		TYPE_ARRAY:
			_load_from_array(data as Array)
		TYPE_DICTIONARY:
			var d: Dictionary = data as Dictionary
			if d.has("species") and typeof(d["species"]) == TYPE_ARRAY:
				_load_from_array(d["species"] as Array)
			elif d.has("equipment") and typeof(d["equipment"]) == TYPE_ARRAY:
				_load_from_array(d["equipment"] as Array)
			else:
				# Support dictionary keyed by id/name: { "human": {...}, "elf": {...} }
				var values: Array = []
				for k in d.keys():
					var v: Variant = d[k]
					if typeof(v) == TYPE_DICTIONARY:
						values.append(v)
				if values.size() > 0:
					_load_from_array(values)
				else:
					push_warning("[%s] JSON root is Dictionary but no recognizable structure: %s" % [get_class(), path])
		_:
			push_warning("[%s] JSON root must be Array or Dictionary: %s" % [get_class(), path])
	
	print("[%s] after load_from_json: count=%d ids=%s" % [get_class(), size(), by_id.keys()])

func _load_from_array(arr: Array) -> void:
	for entry in arr:
		if not StarquillJSONLoader.validate_dictionary_entry(entry):
			continue
		
		var resource: Resource = _create_resource_from_dict(entry as Dictionary)
		if resource != null:
			register_resource(resource)