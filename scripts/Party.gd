extends Node
class_name Party

var members: Array[Character]
var loose_equipment: Array[Items]
var items: Array[Items]

func get_all_equipment() -> Array[Equipment]:
    var all_eq: Array[Equipment] = []
    for member in members:
        all_eq += member.get_all_equipment()
    return all_eq