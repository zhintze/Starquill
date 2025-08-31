extends Node
class_name StarquillFactory

# Virtual methods to be implemented by subclasses
func _get_loader() -> StarquillResourceLoader:
	push_error("Factory._get_loader must be implemented by subclass")
	return null

func _create_instance_from_resource(resource: Resource) -> Resource:
	push_error("Factory._create_instance_from_resource must be implemented by subclass")
	return null

func _get_instance_class_name() -> String:
	push_error("Factory._get_instance_class_name must be implemented by subclass")
	return ""

# Common factory operations
static func create_from_id(factory_instance: Factory, resource_id: String) -> Resource:
	if factory_instance == null:
		push_error("Factory.create_from_id: factory_instance is null")
		return null
		
	var loader: StarquillResourceLoader = factory_instance._get_loader()
	if loader == null:
		push_error("Factory.create_from_id: loader not found")
		return null

	var resource: Resource = loader.get_by_id(resource_id)
	if resource == null:
		var known := PackedStringArray()
		for k in loader.by_id.keys():
			known.append(String(k))
		push_error("Factory: unknown %s '%s'. Known: [%s]" % [
			factory_instance._get_instance_class_name(), resource_id, ", ".join(known)
		])
		return null

	return factory_instance._create_instance_from_resource(resource)

static func create_random(factory_instance: Factory) -> Resource:
	if factory_instance == null:
		push_error("Factory.create_random: factory_instance is null")
		return null
		
	var loader: StarquillResourceLoader = factory_instance._get_loader()
	if loader == null or loader.size() == 0:
		push_error("Factory.create_random: no resources available")
		return null

	var random_index: int = randi() % loader.size()
	var resource: Resource = loader.get_by_index(random_index)
	
	if resource == null:
		push_error("Factory.create_random: failed to get resource at index %d" % random_index)
		return null

	return factory_instance._create_instance_from_resource(resource)

static func list_resource_keys(factory_instance: Factory) -> Array[String]:
	if factory_instance == null:
		return []
		
	var loader: StarquillResourceLoader = factory_instance._get_loader()
	if loader == null:
		return []

	var keys: Array[String] = []
	for key in loader.by_id.keys():
		keys.append(String(key))
	keys.sort()
	return keys

static func get_resource_count(factory_instance: Factory) -> int:
	if factory_instance == null:
		return 0
		
	var loader: StarquillResourceLoader = factory_instance._get_loader()
	if loader == null:
		return 0
		
	return loader.size()

# Error handling helpers
static func _handle_creation_error(factory_instance: Factory, resource_id: String, error: String = "") -> void:
	var class_name: String = factory_instance._get_instance_class_name() if factory_instance else "Unknown"
	if error != "":
		push_error("Factory[%s]: %s for resource '%s'" % [class_name, error, resource_id])
	else:
		push_error("Factory[%s]: Failed to create instance for resource '%s'" % [class_name, resource_id])