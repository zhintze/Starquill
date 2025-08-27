extends Node
class_name ColorList

static func load_palette_from_csv(path: String) -> Array[Color]:
	var out: Array[Color] = []
	if not FileAccess.file_exists(path):
		push_warning("ColorList: file not found: %s" % path)
		return out

	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		return out

	while not f.eof_reached():
		var line := f.get_line().strip_edges()
		if line == "":
			continue

		# 1) Hex with or without '#'/0x
		var t := line
		var lower := t.to_lower()
		var is_hex := true
		for ch in lower:
			if not ((ch >= "0" and ch <= "9") or (ch >= "a" and ch <= "f") or ch == "#" or ch == "x"):
				is_hex = false
				break
		if is_hex:
			# normalize to "#..."
			if not lower.begins_with("#") and not lower.begins_with("0x"):
				t = "#" + t
			out.append(Color(t))
			continue

		# 2) CSV numbers (r,g,b[,a]) in either 0–255 or 0–1.0 scale
		if line.find(",") != -1:
			var parts := line.split(",", false)
			if parts.size() >= 3:
				var nums: Array[float] = []
				var all_int_or_float := true
				for p in parts:
					var v = p.strip_edges().to_float()
					if str(v) == "NaN":
						all_int_or_float = false
						break
					nums.append(v)
				if all_int_or_float:
					var scale_255 := false
					for v in nums:
						if v > 1.0:
							scale_255 = true
							break
					var r := nums[0]
					var g := nums[1]
					var b := nums[2]
					var a := 255.0 if scale_255 else 1.0
					if nums.size() >= 4:
						a = nums[3]
					if scale_255:
						out.append(Color(r/255.0, g/255.0, b/255.0, a/255.0))
					else:
						out.append(Color(r, g, b, a))
			continue

		# else: unrecognized row; skip
	return out
