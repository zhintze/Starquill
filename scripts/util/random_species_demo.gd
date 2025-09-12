# res://scripts/demo/random_species_demo.gd
extends Node2D

const SPECIES_JSON_PATH := "res://assets/data/species.json"
const CHARACTER_DISPLAY_SCENE := preload("res://scenes/CharacterDisplay.tscn")

var _rng := RandomNumberGenerator.new()

func _ready() -> void:
	_rng.randomize()

	# Ensure species are loaded (defensive)
	if StarquillData.get_species_count() == 0:
		if FileAccess.file_exists(SPECIES_JSON_PATH):
			StarquillData.load_species_from_json(SPECIES_JSON_PATH)
			print("After JSON load: count=", StarquillData.get_species_count(), " ids=", StarquillData.get_species_ids())
		else:
			print("JSON missing:", SPECIES_JSON_PATH, " → species will be loaded by ConfigManager")

	if StarquillData.get_species_count() == 0:
		push_error("RandomSpeciesDemo: No species loaded; nothing to render.")
		return

	# Prefer a known id; fall back to random so we always render something
	var target_id := "human"
	var inst: SpeciesInstance = null
	var s := StarquillData.get_species_by_id(target_id)
	if s == null:
		push_warning("Target id '%s' not found. Falling back to random." % target_id)
		inst = StarquillData.create_random_species_instance()
	else:
		inst = StarquillData.create_species_instance(target_id)

	if inst == null:
		push_error("RandomSpeciesDemo: Failed to create instance.")
		return

	# Test hair distribution
	_test_hair_distribution_fixed()

func _test_hair_distribution():
	print("Testing hair distribution...")
	
	# Get human species
	var human = StarquillData.get_species_by_id("human")
	if not human:
		print("Human species not found")
		return
	
	# Track hair selections over many iterations
	var hair_counts = {}
	var total_tests = 10000
	
	for i in total_tests:
		var instance = SpeciesInstance.new()
		instance.from_species(human)
		var selected_hair = instance.hair
		
		if selected_hair in hair_counts:
			hair_counts[selected_hair] += 1
		else:
			hair_counts[selected_hair] = 1
	
	print("Hair distribution test results over %d iterations:" % total_tests)
	print("Expected distributions based on amounts:")
	print("  h01: %.1f%% (69/176)" % (69.0/176.0 * 100))
	print("  h02: %.1f%% (83/176)" % (83.0/176.0 * 100))  
	print("  h03: %.1f%% (5/176)" % (5.0/176.0 * 100))
	print("  h04: %.1f%% (19/176)" % (19.0/176.0 * 100))
	print()
	print("Actual results:")
	for hair_type in hair_counts:
		var percentage = float(hair_counts[hair_type]) / float(total_tests) * 100
		print("  %s: %.1f%% (%d selections)" % [hair_type, percentage, hair_counts[hair_type]])

func _test_hair_distribution_debug():
	print("Testing hair distribution with debug output...")
	
	# Get human species
	var human = StarquillData.get_species_by_id("human")
	if not human:
		print("Human species not found")
		return
	
	# Track hair selections over 10 iterations with debug output
	var hair_counts = {}
	var total_tests = 10
	
	for i in total_tests:
		print("\n--- Test %d ---" % (i+1))
		var instance = SpeciesInstance.new()
		instance.from_species(human)
		var selected_hair = instance.hair
		print("Final selected hair: %s" % selected_hair)
		
		if selected_hair in hair_counts:
			hair_counts[selected_hair] += 1
		else:
			hair_counts[selected_hair] = 1
	
	print("\n=== SUMMARY ===")
	for hair_type in hair_counts:
		var percentage = float(hair_counts[hair_type]) / float(total_tests) * 100
		print("  %s: %.1f%% (%d selections)" % [hair_type, percentage, hair_counts[hair_type]])

func _test_hair_distribution_fixed():
	print("Testing hair distribution after fix...")
	
	# Get human species
	var human = StarquillData.get_species_by_id("human")
	if not human:
		print("Human species not found")
		return
	
	print("Human hair data: %s (type: %s)" % [human.hair, typeof(human.hair)])
	
	# Track hair selections over 1000 iterations
	var hair_counts = {}
	var total_tests = 1000
	
	for i in total_tests:
		var instance = SpeciesInstance.new()
		instance.from_species(human)
		var selected_hair = instance.hair
		
		if selected_hair in hair_counts:
			hair_counts[selected_hair] += 1
		else:
			hair_counts[selected_hair] = 1
	
	print("Hair distribution results over %d iterations:" % total_tests)
	print("Expected distributions based on amounts:")
	print("  h01: %.1f%% (69/176)" % (69.0/176.0 * 100))
	print("  h02: %.1f%% (83/176)" % (83.0/176.0 * 100))  
	print("  h03: %.1f%% (5/176)" % (5.0/176.0 * 100))
	print("  h04: %.1f%% (19/176)" % (19.0/176.0 * 100))
	print()
	print("Actual results:")
	for hair_type in hair_counts:
		var percentage = float(hair_counts[hair_type]) / float(total_tests) * 100
		print("  %s: %.1f%% (%d selections)" % [hair_type, percentage, hair_counts[hair_type]])
