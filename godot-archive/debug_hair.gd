extends Node

func _ready():
	print("=== HAIR DEBUG SESSION ===")
	
	# Test the _select_hair function directly
	var instance = SpeciesInstance.new()
	var test_hair_array = ["h01", "h02", "h03", "h04"]
	
	print("Hair amounts from modular data:")
	for h in test_hair_array:
		var amt = SpeciesInstance._get_modular_count(h)
		print("  %s: %d" % [h, amt])
	
	print("\nTesting _select_hair function 20 times:")
	for i in range(20):
		var result = instance._select_hair(test_hair_array)
		print("  Test %d: %s" % [i+1, result])
	
	print("\n=== END DEBUG ===")
	get_tree().quit()
