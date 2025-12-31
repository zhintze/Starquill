class_name DisplayConstants
extends RefCounted

## DisplayConstants
## Static constants for character display, weapon positioning, and rendering
## Adjust these values to tune off-hand weapon alignment without code changes

# Off-hand weapon positioning (one-handed weapons like swords, axes)
# Rotation: negative = counter-clockwise, positive = clockwise
# Offset X: negative = toward back of character, positive = toward front
# Offset Y: positive = down, negative = up
const OFFHAND_WEAPON_ROTATION: float = -35.0  # degrees counter-clockwise
const OFFHAND_WEAPON_OFFSET: Vector2 = Vector2(-20, 65)

# Off-hand shield positioning
# Shields don't rotate, only translate
const OFFHAND_SHIELD_OFFSET: Vector2 = Vector2(42, 0)

# Main hand weapon positioning (default, no transformation needed)
const MAINHAND_WEAPON_ROTATION: float = 0.0
const MAINHAND_WEAPON_OFFSET: Vector2 = Vector2.ZERO
