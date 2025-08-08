extends Resource
class_name VerbSelector

var strategy: VerbSelectionStrategy

func select_verbs(stats: Stats, context: CombatContext) -> Array[Verb]:
	return strategy.select_verbs(stats, context)
