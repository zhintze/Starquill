extends Node
class_name EquipmentDisplayBuilder

class EquipmentDisplayResult:
	var pieces: Array[DisplayPiece] = []
	var hidden_species_layers: PackedInt32Array = PackedInt32Array()

static func build_for_character(ch: Character) -> EquipmentDisplayResult:
	print("[EquipBuilder] ch=", ch, " has=", ch.get_all_equipment_instances().size())
	var res := EquipmentDisplayResult.new()
	if ch == null:
		return res

	var insts: Array[EquipmentInstance] = ch.get_all_equipment_instances()
	for ei in insts:
		print("[EquipBuilder] instance type=", ei.item_type, " num=", ei.item_num)
		if ei == null:
			continue

		var cat: EquipmentCatalog.CatalogItem = StarquillData.get_equipment_by_type(ei.item_type)
		if cat == null:
			push_warning("EquipBuilder: unknown item_type '%s' on instance" % ei.item_type)
			continue

		# Accumulate species layers to hide (we never hide equipment layers)
		for h in cat.hidden_layers:
			res.hidden_species_layers.append(int(h))

		# Files follow: res://assets/images/equipment/{code}-{4 digits}-{3 digits}.png
		# Example: tr07-0032-054.png   (code=item_type, item_num padded to 4, layer_code padded to 3)
		var code: String = ei.item_type
		var num4: int = int(ei.item_num)

		for layer_code in cat.layer_codes:
			var layer3: int = int(layer_code)
			var path := "res://assets/images/equipment/%s-%04d-%03d.png" % [code, num4, layer3]

			# Use FileAccess to avoid import-db edge cases; warn if missing
			print("[EquipBuilder] PATH TRY: ", path)
			if not FileAccess.file_exists(path):
				push_warning("EquipBuilder: texture not found: %s" % path)
				continue

			

			# Determine tint: variance override, else base_color
			var tint: Color = ei.tint_for_layer(layer_code)

			var dp := DisplayPiece.from_path(path, layer3, tint)
			if dp != null:
				res.pieces.append(dp)

	return res
