extends Resource
class_name Verb

var name: String
var main_stat: int
var power: int
var cost: int
var description: String

func _init(name_: String = "", main_stat_: int = StatType.Type.STR, power_: int = 0, cost_: int = 0, desc_: String = "") -> void:
	name = name_
	main_stat = main_stat_
	power = power_
	cost = cost_
	description = desc_

func get_main_stat_value(stats: Stats) -> int:
	return stats.get_stat(main_stat)
