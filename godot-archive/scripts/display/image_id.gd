extends RefCounted
class_name ImageId

enum Kind { INVALID, EMPTY, STATIC, MODULAR_GROUP_ONLY, MODULAR_FULL }

# We cache compiled regexes for speed.
var _init_done := false
var _re_static: RegEx
var _re_group_only: RegEx
var _re_modular_full: RegEx

func _ensure_init() -> void:
	if _init_done:
		return
	_re_static = RegEx.new()
	_re_static.compile("^\\d{4}-\\d{3}$")            # ####-###
	_re_group_only = RegEx.new()
	_re_group_only.compile("^[A-Za-z]\\d{2}$")       # a##
	_re_modular_full = RegEx.new()
	_re_modular_full.compile("^[A-Za-z]\\d{2}-\\d{4}-\\d{3}$")  # a##-####-###
	_init_done = true

static func to_static_id(image_num: String, layer: int) -> String:
	return "%s-%03d" % [image_num, int(layer)]

static func to_modular_id(group_type: String, image_num: String, layer: int) -> String:
	return "%s-%s-%03d" % [group_type, image_num, int(layer)]

static func parse(raw: String) -> Dictionary:
	var self_ref := ImageId.new() # to use instance methods (_ensure_init, regexes)
	return self_ref._parse_impl(raw)

func _parse_impl(raw: String) -> Dictionary:
	_ensure_init()

	var s := String(raw).strip_edges()
	if s == "":
		return { "kind": Kind.EMPTY }

	# STATIC: ####-###
	if _re_static.search(s) != null:
		return {
			"kind": Kind.STATIC,
			"imageNum": s.substr(0, 4),
			"layer": int(s.substr(5, 3))
		}

	# MODULAR GROUP ONLY: a##
	if _re_group_only.search(s) != null:
		return {
			"kind": Kind.MODULAR_GROUP_ONLY,
			"groupType": s.substr(0, 3).to_lower()
		}

	# MODULAR FULL: a##-####-###
	if _re_modular_full.search(s) != null:
		return {
			"kind": Kind.MODULAR_FULL,
			"groupType": s.substr(0, 3).to_lower(),
			"imageNum": s.substr(4, 4),
			"layer": int(s.substr(9, 3))
		}

	return { "kind": Kind.INVALID, "raw": s }
