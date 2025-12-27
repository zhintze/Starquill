class_name EquipmentIconRenderer
extends RefCounted

## EquipmentIconRenderer
## Composites EquipmentInstance layers into a single Texture2D for UI slots
## Uses DisplayBuilder to get the display pieces, then renders them to an image

# Cache for rendered icons (cache_key -> ImageTexture)
static var _cache: Dictionary = {}

# Default icon size
const DEFAULT_SIZE := Vector2(96, 96)

# Per-prefix icon configuration
# offset: Vector2 - normalized shift (0-1 relative to content size)
# scale: float - multiplier after auto-fit
# allow_overflow: bool - weapons can extend beyond bounds
const ICON_CONFIG: Dictionary = {
	"hd": { "offset": Vector2(0, 0.3), "scale": 1.2, "allow_overflow": false },
	"tr": { "offset": Vector2(0, 0.1), "scale": 1.1, "allow_overflow": false },
	"ar": { "offset": Vector2(0, 0), "scale": 1.15, "allow_overflow": false },
	"lg": { "offset": Vector2(0, -0.1), "scale": 1.1, "allow_overflow": false },
	"fe": { "offset": Vector2(0, -0.25), "scale": 1.2, "allow_overflow": false },
	"w0": { "offset": Vector2(0, 0), "scale": 1.0, "allow_overflow": true },
	"w1": { "offset": Vector2(0, 0), "scale": 1.0, "allow_overflow": true },
}
const ICON_CONFIG_DEFAULT: Dictionary = { "offset": Vector2(0, 0), "scale": 1.0, "allow_overflow": false }

# Helper to extract prefix from item_type
static func _get_prefix(item_type: String) -> String:
	return item_type.substr(0, 2) if item_type.length() >= 2 else item_type

# Get config for equipment based on its prefix
static func _get_icon_config(equipment: EquipmentInstance) -> Dictionary:
	var prefix := _get_prefix(equipment.item_type)
	return ICON_CONFIG.get(prefix, ICON_CONFIG_DEFAULT)

# Get or create an icon texture for equipment
static func get_icon(equipment: EquipmentInstance, icon_size: Vector2 = DEFAULT_SIZE) -> Texture2D:
	if equipment == null:
		return null

	# Create cache key from equipment properties (includes size)
	var cache_key := _create_cache_key(equipment, icon_size)
	if _cache.has(cache_key):
		return _cache[cache_key]

	# Build display pieces using DisplayBuilder
	var builder := StarquillDisplayBuilder.new()
	var equipment_array: Array[EquipmentInstance] = [equipment]
	var result := builder.build_equipment_pieces(equipment_array, PackedStringArray())
	var pieces: Array[DisplayPiece] = result.pieces

	if pieces.is_empty():
		return null

	# Composite pieces into single texture with type-specific config
	var icon := _composite_pieces(pieces, icon_size, equipment)
	if icon:
		_cache[cache_key] = icon

	return icon

# Create a unique cache key for this equipment configuration
static func _create_cache_key(equipment: EquipmentInstance, icon_size: Vector2) -> String:
	# Include item type, number, base color, and size for uniqueness
	var color_hex := equipment.base_color.to_html(false)
	return "%s_%d_%s_%dx%d" % [equipment.item_type, equipment.item_num, color_hex, int(icon_size.x), int(icon_size.y)]

# Composite multiple display pieces into a single ImageTexture
static func _composite_pieces(pieces: Array[DisplayPiece], target_size: Vector2, equipment: EquipmentInstance = null) -> ImageTexture:
	if pieces.is_empty():
		return null

	# Get config for this equipment type
	var config := ICON_CONFIG_DEFAULT
	if equipment:
		config = _get_icon_config(equipment)

	var type_scale: float = config.get("scale", 1.0)
	var type_offset: Vector2 = config.get("offset", Vector2.ZERO)
	var allow_overflow: bool = config.get("allow_overflow", false)

	# Find bounds of all pieces
	var min_pos := Vector2.INF
	var max_pos := Vector2(-INF, -INF)

	for piece in pieces:
		if piece.texture == null:
			continue
		var tex_size: Vector2 = piece.texture.get_size()
		var pos: Vector2 = piece.offset
		min_pos.x = minf(min_pos.x, pos.x)
		min_pos.y = minf(min_pos.y, pos.y)
		max_pos.x = maxf(max_pos.x, pos.x + tex_size.x)
		max_pos.y = maxf(max_pos.y, pos.y + tex_size.y)

	if min_pos == Vector2.INF:
		return null

	var content_size := max_pos - min_pos
	if content_size.x <= 0 or content_size.y <= 0:
		return null

	# For weapons with overflow, render to larger canvas then crop
	var render_size := target_size
	if allow_overflow:
		render_size = target_size * 1.25  # 25% larger canvas for overflow

	# Create render image
	var img := Image.create(int(render_size.x), int(render_size.y), false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	# Calculate scale to fit content in target size with padding
	var padding := 4.0
	var available_size := target_size - Vector2(padding * 2, padding * 2)
	var scale_factor := minf(available_size.x / content_size.x, available_size.y / content_size.y)
	scale_factor = minf(scale_factor, 1.0)  # Don't upscale

	# Apply type-specific scale multiplier
	scale_factor *= type_scale

	# Calculate offset to center the content
	var scaled_size := content_size * scale_factor
	var base_offset := (render_size - scaled_size) / 2.0

	# Apply type-specific offset (relative to scaled content size)
	var offset := base_offset + type_offset * scaled_size

	# Sort pieces by layer for correct draw order
	var sorted_pieces := pieces.duplicate()
	sorted_pieces.sort_custom(func(a: DisplayPiece, b: DisplayPiece) -> bool:
		return a.layer < b.layer
	)

	# Render each piece
	for piece in sorted_pieces:
		if piece.texture == null:
			continue

		var piece_img: Image = piece.texture.get_image()
		if piece_img == null:
			continue

		# Apply tint/modulate color
		if piece.modulate != Color.WHITE:
			piece_img = piece_img.duplicate()
			_apply_tint(piece_img, piece.modulate)

		# Calculate piece position in target image
		var piece_pos: Vector2 = (Vector2(piece.offset) - min_pos) * scale_factor + offset

		# Scale piece if needed
		var piece_size: Vector2 = Vector2(piece_img.get_width(), piece_img.get_height())
		var scaled_piece_size: Vector2 = piece_size * scale_factor

		if scale_factor != 1.0 and scaled_piece_size.x > 0 and scaled_piece_size.y > 0:
			piece_img = piece_img.duplicate()
			piece_img.resize(int(scaled_piece_size.x), int(scaled_piece_size.y), Image.INTERPOLATE_BILINEAR)

		# Blend piece onto target image
		_blend_image(img, piece_img, Vector2i(int(piece_pos.x), int(piece_pos.y)))

	# If we rendered to a larger canvas, crop back to target size
	if allow_overflow and render_size != target_size:
		var crop_offset := (render_size - target_size) / 2.0
		var cropped := Image.create(int(target_size.x), int(target_size.y), false, Image.FORMAT_RGBA8)
		cropped.fill(Color.TRANSPARENT)
		cropped.blit_rect(img, Rect2i(int(crop_offset.x), int(crop_offset.y), int(target_size.x), int(target_size.y)), Vector2i.ZERO)
		img = cropped

	# Create texture from image
	return ImageTexture.create_from_image(img)

# Apply tint color to an image (multiply blend)
static func _apply_tint(img: Image, tint: Color) -> void:
	for y in range(img.get_height()):
		for x in range(img.get_width()):
			var pixel := img.get_pixel(x, y)
			if pixel.a > 0:
				pixel.r *= tint.r
				pixel.g *= tint.g
				pixel.b *= tint.b
				img.set_pixel(x, y, pixel)

# Blend source image onto destination with alpha
static func _blend_image(dst: Image, src: Image, pos: Vector2i) -> void:
	var src_rect := Rect2i(0, 0, src.get_width(), src.get_height())
	var dst_rect := Rect2i(pos, src_rect.size)

	# Clip to destination bounds
	if dst_rect.position.x < 0:
		src_rect.position.x -= dst_rect.position.x
		src_rect.size.x += dst_rect.position.x
		dst_rect.position.x = 0
	if dst_rect.position.y < 0:
		src_rect.position.y -= dst_rect.position.y
		src_rect.size.y += dst_rect.position.y
		dst_rect.position.y = 0

	var dst_end := dst_rect.position + dst_rect.size
	if dst_end.x > dst.get_width():
		src_rect.size.x -= dst_end.x - dst.get_width()
	if dst_end.y > dst.get_height():
		src_rect.size.y -= dst_end.y - dst.get_height()

	if src_rect.size.x <= 0 or src_rect.size.y <= 0:
		return

	# Blend pixels with alpha
	for y in range(src_rect.size.y):
		for x in range(src_rect.size.x):
			var src_pixel := src.get_pixel(src_rect.position.x + x, src_rect.position.y + y)
			if src_pixel.a > 0:
				var dst_x := dst_rect.position.x + x
				var dst_y := dst_rect.position.y + y
				var dst_pixel := dst.get_pixel(dst_x, dst_y)

				# Alpha blending
				var out_alpha := src_pixel.a + dst_pixel.a * (1.0 - src_pixel.a)
				if out_alpha > 0:
					var out_color := Color(
						(src_pixel.r * src_pixel.a + dst_pixel.r * dst_pixel.a * (1.0 - src_pixel.a)) / out_alpha,
						(src_pixel.g * src_pixel.a + dst_pixel.g * dst_pixel.a * (1.0 - src_pixel.a)) / out_alpha,
						(src_pixel.b * src_pixel.a + dst_pixel.b * dst_pixel.a * (1.0 - src_pixel.a)) / out_alpha,
						out_alpha
					)
					dst.set_pixel(dst_x, dst_y, out_color)

# Clear the icon cache (call on theme change or memory pressure)
static func clear_cache() -> void:
	_cache.clear()
