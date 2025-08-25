extends CharacterDisplay
class_name CharacterDisplayDebug

var _debug_win: Window
var _debug_list: VBoxContainer

func _ready() -> void:
	super._ready()
	# Only the debug subclass is clickable.
	mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_show_debug_popup()
		accept_event()

func _on_model_changed() -> void:
	super._on_model_changed()
	_refresh_debug_panel_if_visible()

# ---------- Debug UI ----------

func _show_debug_popup() -> void:
	_ensure_debug_window()
	_rebuild_debug_content()
	_position_debug_window_near_mouse()
	_debug_win.show()
	if _debug_win.has_method("move_to_foreground"):
		_debug_win.move_to_foreground()
	else:
		_debug_win.grab_focus()

func _ensure_debug_window() -> void:
	if _debug_win and is_instance_valid(_debug_win):
		return
	_debug_win = Window.new()
	_debug_win.title = "Character Debug"
	_debug_win.size = Vector2i(420, 520)
	_debug_win.unresizable = true
	_debug_win.visible = false
	_debug_win.always_on_top = true
	get_tree().root.add_child(_debug_win)

	var root: VBoxContainer = VBoxContainer.new()
	root.name = "Root"
	root.custom_minimum_size = Vector2(380, 480)
	_debug_win.add_child(root)

	var header: HBoxContainer = HBoxContainer.new()
	header.name = "Header"
	root.add_child(header)

	var title: Label = Label.new()
	title.name = "Title"
	title.text = "species: "
	title.autowrap_mode = TextServer.AUTOWRAP_OFF
	header.add_child(title)

	var spacer: Control = Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(spacer)

	var close_btn: Button = Button.new()
	close_btn.text = "Close"
	close_btn.pressed.connect(func() -> void: _debug_win.hide())
	header.add_child(close_btn)

	var sep: HSeparator = HSeparator.new()
	root.add_child(sep)

	var scroll: ScrollContainer = ScrollContainer.new()
	scroll.name = "Scroll"
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(scroll)

	_debug_list = VBoxContainer.new()
	_debug_list.name = "List"
	_debug_list.add_theme_constant_override("separation", 6)
	scroll.add_child(_debug_list)

func _rebuild_debug_content() -> void:
	if not _debug_win:
		return

	var title: Label = _debug_win.get_node("Root/Header/Title") as Label
	if _character and _character.species:
		title.text = "species: " + String(_character.species.species_id)
	else:
		title.text = "species: (none)"

	for c in _debug_list.get_children():
		(c as Node).queue_free()

	if not _character or not _character.species:
		return

	var disp: SpeciesDisplayable = SpeciesDisplayable.new(_character.species)
	var pieces: Array = disp.get_display_pieces()

	for p in pieces:
		var piece: Resource = p

		var row: HBoxContainer = HBoxContainer.new()
		row.add_theme_constant_override("separation", 8)
		_debug_list.add_child(row)

		var preview: TextureRect = TextureRect.new()
		preview.custom_minimum_size = Vector2(48, 48)
		preview.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
		var tex: Texture2D = _get_prop_any(piece, ["texture", "tex"], null) as Texture2D
		preview.texture = tex
		row.add_child(preview)

		var info: Label = Label.new()
		info.autowrap_mode = TextServer.AUTOWRAP_WORD
		info.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		info.text = _fmt_piece_line(piece)
		row.add_child(info)

func _fmt_piece_line(piece: Resource) -> String:
	var slot_raw: String = String(_get_prop_any(piece, ["slot", "part", "type", "slot_name", "category"], ""))
	var slot: String = "Part"
	if slot_raw.length() > 0:
		slot = slot_raw.substr(0, 1).to_upper() + slot_raw.substr(1).to_lower()

	var tex: Texture2D = _get_prop_any(piece, ["texture", "tex"], null) as Texture2D
	var file: String = "(inline)"
	if tex and tex.resource_path != "":
		file = tex.resource_path.get_file()
	else:
		var id_str: String = String(_get_prop_any(piece, ["id", "code", "name", "key"], ""))
		if id_str != "":
			file = id_str

	var layer: int = int(_get_prop_any(piece, ["layer", "z", "order", "z_index"], 0))
	var col: Color = _get_prop_any(piece, ["modulate", "color", "tint"], Color(1, 1, 1, 1)) as Color
	var col_hex: String = "#" + col.to_html(true)

	return "%s: %s    Layer: %d    Color: %s" % [slot, file, layer, col_hex]

func _position_debug_window_near_mouse() -> void:
	if not _debug_win:
		return
	var mp: Vector2 = get_viewport().get_mouse_position()
	var vp_size: Vector2 = Vector2(get_viewport_rect().size)
	var sz: Vector2 = Vector2(_debug_win.size)
	var px: float = clamp(mp.x, 0.0, vp_size.x - sz.x)
	var py: float = clamp(mp.y, 0.0, vp_size.y - sz.y)
	_debug_win.position = Vector2i(int(px), int(py))

func _refresh_debug_panel_if_visible() -> void:
	if _debug_win and _debug_win.visible:
		_rebuild_debug_content()

# ---------- Reflection helpers (schema-agnostic) ----------

func _has_property(obj: Object, prop: String) -> bool:
	var plist: Array = obj.get_property_list()
	for d in plist:
		if d is Dictionary and (d as Dictionary).has("name") and String((d as Dictionary)["name"]) == prop:
			return true
	return false

func _get_prop(obj: Object, prop: String, default_value: Variant = null) -> Variant:
	var getter: String = "get_" + prop
	if obj.has_method(getter):
		return obj.call(getter)
	if _has_property(obj, prop):
		return obj.get(prop)
	return default_value

func _get_prop_any(obj: Object, names: Array, default_value: Variant = null) -> Variant:
	for n in names:
		var key: String = String(n)
		var v: Variant = _get_prop(obj, key, null)
		if v != null:
			return v
	return default_value
