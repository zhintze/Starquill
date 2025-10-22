class_name Pathfinder
extends RefCounted

# Simple A* pathfinding for tile-based world maps

static func find_path(world_map: WorldMap, start: Vector2i, goal: Vector2i, max_distance: int = 100) -> Array[Vector2i]:
	if start == goal:
		return []

	# Check if goal is valid and passable
	var goal_tile = world_map.get_tile(goal)
	if not goal_tile or not goal_tile.is_passable:
		return []

	# A* algorithm
	var open_set: Array[Vector2i] = [start]
	var came_from: Dictionary = {}
	var g_score: Dictionary = {start: 0}
	var f_score: Dictionary = {start: _heuristic(start, goal)}

	while open_set.size() > 0:
		# Get node with lowest f_score
		var current = _get_lowest_f_score(open_set, f_score)

		if current == goal:
			return _reconstruct_path(came_from, current)

		open_set.erase(current)

		# Check neighbors (4-directional movement)
		var neighbors = [
			current + Vector2i(0, -1),  # Up
			current + Vector2i(0, 1),   # Down
			current + Vector2i(-1, 0),  # Left
			current + Vector2i(1, 0)    # Right
		]

		for neighbor in neighbors:
			# Skip if not valid or not passable
			if not WorldCoordinate.is_valid_position(neighbor):
				continue

			var tile = world_map.get_tile(neighbor)
			if not tile or not tile.is_passable:
				continue

			# Calculate tentative g_score
			var tentative_g_score = g_score.get(current, INF) + 1

			# Check distance limit
			if tentative_g_score > max_distance:
				continue

			if tentative_g_score < g_score.get(neighbor, INF):
				# This path to neighbor is better
				came_from[neighbor] = current
				g_score[neighbor] = tentative_g_score
				f_score[neighbor] = tentative_g_score + _heuristic(neighbor, goal)

				if not open_set.has(neighbor):
					open_set.append(neighbor)

	# No path found
	return []

static func _heuristic(a: Vector2i, b: Vector2i) -> float:
	# Manhattan distance for 4-directional movement
	return abs(a.x - b.x) + abs(a.y - b.y)

static func _get_lowest_f_score(open_set: Array[Vector2i], f_score: Dictionary) -> Vector2i:
	var lowest = open_set[0]
	var lowest_score = f_score.get(lowest, INF)

	for pos in open_set:
		var score = f_score.get(pos, INF)
		if score < lowest_score:
			lowest = pos
			lowest_score = score

	return lowest

static func _reconstruct_path(came_from: Dictionary, current: Vector2i) -> Array[Vector2i]:
	var path: Array[Vector2i] = [current]

	while came_from.has(current):
		current = came_from[current]
		path.insert(0, current)

	# Remove the starting position (we're already there)
	if path.size() > 0:
		path.remove_at(0)

	return path
