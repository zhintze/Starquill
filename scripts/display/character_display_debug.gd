extends CharacterDisplay
class_name CharacterDisplayDebug

const THUMB_SIZE: Vector2i = Vector2i(224, 224)
const TRIM_PREFIX: String = "res://assets/images/"

var _win: Window
var _root_margin: MarginContainer
var _scroll: ScrollContainer
var _list: VBoxContainer

func _ready() -> void:
	super._ready() # keep base rendering + mouse filter
	mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_open_popup()
		accept_event()

# ─────────────────────────────────────────────────────────────
# Popup
# ─────────────────────────────────────────────────────────────
func _open_popup() -> void:
	if _win == null:
		_build_window_once()
	_refresh_list_contents()

	# Always fit on screen: clamp to a target size, but never exceed 90% of the viewport.
	# (popup_centered_clamped will shrink if the screen is smaller)
	var target := Vector2i(1100, 820)  # your “nice” size on big monitors
	_win.popup_centered_clamped(target, 0.90)

func _build_window_once() -> void:
	_win = Window.new()
	_win.title = "Character Debug"
	_win.min_size = Vector2(720, 480)     # sensible minimum
	_win.unresizable = false              # allow manual resize if you want
	_win.close_requested.connect(func() -> void: _win.hide())
	add_child(_win)

	# Root padding container (Control)
	_root_margin = MarginContainer.new()
	_root_margin.add_theme_constant_override("margin_left", 14)
	_root_margin.add_theme_constant_override("margin_top", 14)
	_root_margin.add_theme_constant_override("margin_right", 14)
	_root_margin.add_theme_constant_override("margin_bottom", 14)
	# Fill the window via anchors (not size_flags, since parent is Window)
	_root_margin.anchor_left = 0.0
	_root_margin.anchor_top = 0.0
	_root_margin.anchor_right = 1.0
	_root_margin.anchor_bottom = 1.0
	_root_margin.offset_left = 0
	_root_margin.offset_top = 0
	_root_margin.offset_right = 0
	_root_margin.offset_bottom = 0
	_win.add_child(_root_margin)

	# Scroll area (fills the padded root)
	_scroll = ScrollContainer.new()
	_scroll.anchor_left = 0.0
	_scroll.anchor_top = 0.0
	_scroll.anchor_right = 1.0
	_scroll.anchor_bottom = 1.0
	_scroll.offset_left = 0
	_scroll.offset_top = 0
	_scroll.offset_right = 0
	_scroll.offset_bottom = 0
	_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_root_margin.add_child(_scroll)

	# List container (expands horizontally; scrolls vertically as content grows)
	_list = VBoxContainer.new()
	_list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_list.size_flags_vertical = Control.SIZE_EXPAND
	_list.add_theme_constant_override("separation", 12)
	_scroll.add_child(_list)

func _clear_children(node: Node) -> void:
	for child in node.get_children():
		child.queue_free()

func _refresh_list_contents() -> void:
	if _list == null:
		return
	_clear_children(_list)

	if _character == null:
		_add_h1_card("No character bound")
		return

	# ── Summary card ──
	var species_name := ""
	if _character.species and _character.species.base:
		species_name = _character.species.base.name

	var insts: Array[EquipmentInstance] = _character.get_all_equipment_instances()
	var hidden_layers: PackedInt32Array = _get_hidden_layers_from_equipment(insts)

	var summary := VBoxContainer.new()
	summary.add_theme_constant_override("separation", 6)
	_add_card(summary)
	_add_h1_into(summary, "Summary")
	_add_kv_into(summary, "Display Name", String(_character.display_name))
	_add_kv_into(summary, "Species", species_name)
	_add_kv_into(summary, "Equipped Count", str(insts.size()))
	if not hidden_layers.is_empty():
		_add_kv_into(summary, "Hidden Species Layers", _join_ints_sorted_unique(hidden_layers))

	# Get equipment with actual slot information
	var equipment_with_slots = _character.get_all_equipment_with_slots()
	if not equipment_with_slots.is_empty():
		var list := VBoxContainer.new()
		list.add_theme_constant_override("separation", 2)
		for entry in equipment_with_slots:
			var ei: EquipmentInstance = entry.equipment
			var actual_slot: String = entry.slot
			_add_text_into(list, "• %s  :  %s  #%04d" % [actual_slot, ei.item_type, int(ei.item_num)])
		_add_section_divider_into(summary)
		summary.add_child(list)

	# ── Final merged stack (Species first → Equip; each by layer) ──
	var merged: Array[DisplayPiece] = _character.get_display_pieces()
	if merged.is_empty():
		_add_h1_card("Final Piece Stack (merged)")
		_add_text_card("(no pieces returned)")
		return

	# Build species-only key set (trimmed path|layer) to classify pieces
	var species_only := _gather_species_keys()

	# Partition into species/equipment
	var species_list: Array[DisplayPiece] = []
	var equip_list: Array[DisplayPiece] = []
	for p in merged:
		var path := ""
		if p.texture and p.texture.resource_path != "":
			path = _trim_path(p.texture.resource_path)
		var key := "%s|%d" % [path, int(p.layer)]
		
		# Classify based on texture path directory (more reliable than key matching)
		if path.begins_with("species/"):
			species_list.append(p)
		elif path.begins_with("equipment/"):
			equip_list.append(p)
		else:
			# Fallback to original key-based detection for unknown paths
			if species_only.has(key):
				species_list.append(p)
			else:
				equip_list.append(p)

	# Sort each bucket by layer ascending
	var cmp = func(a: DisplayPiece, b: DisplayPiece) -> bool:
		return int(a.layer) < int(b.layer)
	species_list.sort_custom(cmp)
	equip_list.sort_custom(cmp)

	# Species card
	var sp_card := VBoxContainer.new()
	sp_card.add_theme_constant_override("separation", 8)
	_add_card(sp_card)
	_add_h1_into(sp_card, "Species Pieces")
	_add_kv_into(sp_card, "Count", str(species_list.size()))
	_add_section_divider_into(sp_card)
	for p in species_list:
		_add_piece_row(sp_card, "[Species]", p)

	# Equipment card
	var eq_card := VBoxContainer.new()
	eq_card.add_theme_constant_override("separation", 8)
	_add_card(eq_card)
	_add_h1_into(eq_card, "Equipment Pieces")
	_add_kv_into(eq_card, "Count", str(equip_list.size()))
	_add_section_divider_into(eq_card)
	for p in equip_list:
		_add_piece_row(eq_card, "[Equip]", p)

# ─────────────────────────────────────────────────────────────
# Piece row helper
# ─────────────────────────────────────────────────────────────
func _add_piece_row(parent: Control, tag: String, p: DisplayPiece) -> void:
	var path := ""
	if p.texture and p.texture.resource_path != "":
		path = _trim_path(p.texture.resource_path)

	var row := HBoxContainer.new()
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_theme_constant_override("separation", 12)
	parent.add_child(row)

	# Thumbnail (NOT tinted per request)
	var thumb := TextureRect.new()
	thumb.custom_minimum_size = Vector2(THUMB_SIZE.x, THUMB_SIZE.y)
	thumb.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	thumb.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	thumb.texture = p.texture
	thumb.modulate = Color(1, 1, 1, 1) # remove color from image
	thumb.mouse_filter = Control.MOUSE_FILTER_IGNORE
	row.add_child(thumb)

	# Color chip (shows the tint)
	var chip := ColorRect.new()
	chip.color = p.modulate
	chip.custom_minimum_size = Vector2(24, 24)
	chip.mouse_filter = Control.MOUSE_FILTER_IGNORE
	row.add_child(chip)

	# Description
	var desc := "%s  layer: %03d   %s    color: #%s" % [tag, int(p.layer), path, _color_to_hex(p.modulate)]
	var lbl := Label.new()
	lbl.text = desc
	lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(lbl)

# ─────────────────────────────────────────────────────────────
# Helpers
# ─────────────────────────────────────────────────────────────
func _trim_path(full_path: String) -> String:
	return full_path.substr(TRIM_PREFIX.length()) if full_path.begins_with(TRIM_PREFIX) else full_path

func _color_to_hex(c: Color) -> String:
	var r := int(round(c.r * 255.0))
	var g := int(round(c.g * 255.0))
	var b := int(round(c.b * 255.0))
	return "%02X%02X%02X" % [r, g, b]

func _gather_species_keys() -> Dictionary:
	var out := {}
	if _character == null or _character.species == null:
		return out
	var sp_pieces: Array[DisplayPiece] = DisplayBuilder.build_species_pieces(_character.species)
	for p in sp_pieces:
		if p.texture and p.texture.resource_path != "":
			var key := "%s|%d" % [_trim_path(p.texture.resource_path), int(p.layer)]
			out[key] = true
	return out

func _get_hidden_layers_from_equipment(insts: Array[EquipmentInstance]) -> PackedInt32Array:
	var out := PackedInt32Array()
	for ei in insts:
		var cat: EquipmentCatalog.CatalogItem = StarquillData.get_equipment_by_type(ei.item_type)
		if cat == null:
			continue
		for h in cat.hidden_layers:
			out.append(int(h))
	return out

func _join_ints_sorted(pia: PackedInt32Array) -> String:
	var arr := PackedInt32Array(pia)
	if arr.is_empty():
		return "(none)"
	arr.sort()
	var parts: Array[String] = []
	for v in arr:
		parts.append("%d" % int(v))
	return ",".join(parts)

# ─────────────────────────────────────────────────────────────
# “Card” UI helpers
# ─────────────────────────────────────────────────────────────
func _add_card(content: Control) -> void:
	var card := PanelContainer.new()
	card.add_theme_constant_override("margin_left", 10)
	card.add_theme_constant_override("margin_top", 10)
	card.add_theme_constant_override("margin_right", 10)
	card.add_theme_constant_override("margin_bottom", 10)
	card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	card.size_flags_vertical = Control.SIZE_FILL

	var inner := MarginContainer.new()
	inner.add_theme_constant_override("margin_left", 12)
	inner.add_theme_constant_override("margin_top", 10)
	inner.add_theme_constant_override("margin_right", 12)
	inner.add_theme_constant_override("margin_bottom", 10)
	card.add_child(inner)

	inner.add_child(content)
	_list.add_child(card)

func _add_h1_card(title: String) -> void:
	var box := VBoxContainer.new()
	_add_card(box)
	_add_h1_into(box, title)

func _add_text_card(t: String) -> void:
	var box := VBoxContainer.new()
	_add_card(box)
	_add_text_into(box, t)

func _add_h1_into(parent: Control, title: String) -> void:
	var l := Label.new()
	l.text = title
	l.add_theme_font_size_override("font_size", 18)
	parent.add_child(l)

func _add_kv_into(parent: Control, k: String, v: String) -> void:
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", 6)
	var l1 := Label.new()
	l1.text = k + ": "
	l1.custom_minimum_size = Vector2(160, 0)
	var l2 := Label.new()
	l2.text = v
	h.add_child(l1)
	h.add_child(l2)
	parent.add_child(h)

func _add_text_into(parent: Control, t: String) -> void:
	var l := Label.new()
	l.text = t
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	parent.add_child(l)

func _add_section_divider_into(parent: Control) -> void:
	var sep := HSeparator.new()
	sep.custom_minimum_size = Vector2(0, 6)
	parent.add_child(sep)
	
func _join_ints_sorted_unique(pia: PackedInt32Array) -> String:
	if pia.is_empty():
		return "(none)"
	# make a set
	var uniq := {}
	for v in pia:
		uniq[int(v)] = true
	# sort keys
	var keys := uniq.keys()
	var cmp = func(a, b) -> bool:
		return int(a) < int(b)
	keys.sort_custom(cmp)
	# join
	var parts: Array[String] = []
	for k in keys:
		parts.append(str(int(k)))
	return ",".join(parts)
