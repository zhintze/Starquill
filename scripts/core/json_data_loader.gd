class_name StarquillJSONLoader

# Uses ConfigManager autoload for error messages

enum LoadResult {
	SUCCESS,
	FILE_NOT_FOUND,
	READ_ERROR,
	PARSE_ERROR,
	INVALID_FORMAT
}

class LoadResponse:
	var result: LoadResult
	var data: Variant
	var error_message: String
	
	func _init(res: LoadResult, d: Variant = null, msg: String = ""):
		result = res
		data = d
		error_message = msg

static func read_text_file(path: String) -> String:
	var fa: FileAccess = FileAccess.open(path, FileAccess.READ)
	return "" if fa == null else fa.get_as_text()

static func file_exists(path: String) -> bool:
	return FileAccess.file_exists(path)

static func load_json_file(path: String) -> LoadResponse:
	if not file_exists(path):
		return LoadResponse.new(LoadResult.FILE_NOT_FOUND, null, ConfigManager.get_error_message("file_not_found", [path]))
	
	var raw_text: String = read_text_file(path)
	if raw_text.is_empty():
		return LoadResponse.new(LoadResult.READ_ERROR, null, "Failed to read file: " + path)
	
	var parsed: Variant = JSON.parse_string(raw_text)
	if parsed == null:
		return LoadResponse.new(LoadResult.PARSE_ERROR, null, ConfigManager.get_error_message("invalid_json", [path]))
	
	return LoadResponse.new(LoadResult.SUCCESS, parsed)

static func load_json_array(path: String) -> LoadResponse:
	var response: LoadResponse = load_json_file(path)
	if response.result != LoadResult.SUCCESS:
		return response
		
	if typeof(response.data) != TYPE_ARRAY:
		return LoadResponse.new(LoadResult.INVALID_FORMAT, null, "JSON root must be an array: " + path)
	
	return response

static func load_json_dictionary(path: String) -> LoadResponse:
	var response: LoadResponse = load_json_file(path)
	if response.result != LoadResult.SUCCESS:
		return response
		
	if typeof(response.data) != TYPE_DICTIONARY:
		return LoadResponse.new(LoadResult.INVALID_FORMAT, null, "JSON root must be a dictionary: " + path)
	
	return response

static func validate_dictionary_entry(entry: Variant) -> bool:
	return typeof(entry) == TYPE_DICTIONARY

static func validate_required_fields(dict: Dictionary, required_fields: Array[String]) -> bool:
	for field in required_fields:
		if not dict.has(field):
			return false
	return true

static func try_load_first_available(paths: Array[String]) -> LoadResponse:
	for path in paths:
		if file_exists(path):
			return load_json_file(path)
	return LoadResponse.new(LoadResult.FILE_NOT_FOUND, null, "None of the candidate paths exist: " + str(paths))

static func scan_directory_for_json(dir_path: String) -> Array[String]:
	var json_files: Array[String] = []
	var da: DirAccess = DirAccess.open(dir_path)
	if da == null:
		push_warning("JSONDataLoader: Cannot open directory: " + dir_path)
		return json_files
		
	da.list_dir_begin()
	while true:
		var file_name: String = da.get_next()
		if file_name == "":
			break
		if da.current_is_dir():
			continue
		if file_name.ends_with(".json"):
			json_files.append(dir_path.path_join(file_name))
	da.list_dir_end()
	
	return json_files

static func load_all_json_from_directory(dir_path: String) -> Array[LoadResponse]:
	var responses: Array[LoadResponse] = []
	var json_files: Array[String] = scan_directory_for_json(dir_path)
	
	for file_path in json_files:
		responses.append(load_json_file(file_path))
	
	return responses