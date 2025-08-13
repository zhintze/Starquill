## BusCombat.gd — turn/actions/damage/outcomes
extends Node

signal encounter_started(encounter_id: String, party_ids: Array, enemy_ids: Array)
signal turn_started(encounter_id: String, actor_id: String, round: int)
signal verb_selected(encounter_id: String, actor_id: String, verb_id: String, targets: Array)
signal action_resolved(encounter_id: String, actor_id: String, verb_id: String, result: Dictionary)
signal damage_applied(encounter_id: String, target_id: String, amount: float, damage_type: String)
signal actor_downed(encounter_id: String, actor_id: String)
signal encounter_ended(encounter_id: String, victory: bool, summary: Dictionary)

# Helpers
func emit_encounter_started(encounter_id: String, party_ids: Array, enemy_ids: Array) -> void:
	encounter_started.emit(encounter_id, party_ids, enemy_ids)

func emit_turn_started(encounter_id: String, actor_id: String, round: int) -> void:
	turn_started.emit(encounter_id, actor_id, round)

func select_verb(encounter_id: String, actor_id: String, verb_id: String, targets: Array) -> void:
	verb_selected.emit(encounter_id, actor_id, verb_id, targets)

func emit_action_result(encounter_id: String, actor_id: String, verb_id: String, hits: Array, crit: bool=false, applied_status: Array=[]) -> void:
	var result := {"hits": hits, "crit": crit, "status_applied": applied_status}
	action_resolved.emit(encounter_id, actor_id, verb_id, result)

func apply_damage(encounter_id: String, target_id: String, amount: float, damage_type: String) -> void:
	damage_applied.emit(encounter_id, target_id, amount, damage_type)

func emit_actor_downed(encounter_id: String, actor_id: String) -> void:
	actor_downed.emit(encounter_id, actor_id)

func emit_encounter_ended(encounter_id: String, victory: bool, summary: Dictionary) -> void:
	encounter_ended.emit(encounter_id, victory, summary)
